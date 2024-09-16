using System;
using System.Collections.Concurrent;
using UnityEngine;
using Microsoft.CognitiveServices.Speech;
using System.Threading.Tasks;
using DefaultNamespace;
using JobSystem;
using Microsoft.CognitiveServices.Speech.Audio;
using Unity.Collections;
using Unity.Jobs;
using Debug = UnityEngine.Debug;


public class SpeechSynthesiserUnityStream : MonoBehaviour
{
    [SerializeField] private MicrosoftSpeechSDKServiceConfig speechSDKServiceConfig;
    private AudioSource _audioSource;
    private SpeechConfig _speechConfig;
    private SpeechSynthesizer _speechSynthesizer;

    [SerializeField] private SpeechSynthProvider speechSynthProvider;
    public uLipSync.uLipSync uLipsync;
    private readonly ConcurrentQueue<string> _sentenceQueue = new();

    private volatile bool
        _isSynthesizing; // Marked as volatile to ensure the visibility of its latest value across threads

    private void Awake()
    {
        _speechConfig =
            SpeechConfig.FromSubscription(speechSDKServiceConfig.SubscriptionKey, speechSDKServiceConfig.Region);
        _speechConfig.SpeechSynthesisVoiceName = speechSDKServiceConfig.VoiceName;
        _speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Raw44100Hz16BitMonoPcm);
        _speechSynthesizer = new SpeechSynthesizer(_speechConfig, null);
        using var connection = Connection.FromSpeechSynthesizer(_speechSynthesizer);
        connection.Open(true);
        _audioSource = GetComponent<AudioSource>();
        _audioSource.loop = true;
        _audioSource.Play();
    }

    void Start()
    {
        SharedResources.OnNewDataAvailable += TryDequeueNewSentence;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            StopSynthesisAndAudio();
        }
    }
    
    public async void StopSynthesisAndAudio()
    {
        // Stop the synthesis process
        if (_isSynthesizing)
        {
            await _speechSynthesizer.StopSpeakingAsync();
            _isSynthesizing = false;
        }

        // Stop the currently playing audio
        if (_audioSource.isPlaying)
        {
            _audioSource.Stop();
        }

        // Clear the sentence queue and the audio data queue
        while (_sentenceQueue.TryDequeue(out _)) { /* intentionally left blank */ }
        speechSynthProvider.Clear();
    }
    

    private void OnDestroy()
    {
        _speechSynthesizer.Dispose();

        SharedResources.OnNewDataAvailable -= TryDequeueNewSentence;
        
        
    }


    void OnAudioFilterRead(float[] data, int channels)
    {
        speechSynthProvider.FillBuffer(data, channels);
        uLipsync.OnDataReceived(data, channels);
    }


    void TryDequeueNewSentence()
    {
        if (SharedResources.TryGetNextSpeech(out string textChunk))
        {
            _sentenceQueue.Enqueue(textChunk);
            if (!_isSynthesizing)
            {
                ProcessSentences();
            }
        }
    }

    private async void ProcessSentences()
    {
        if (_isSynthesizing) return;
        _isSynthesizing = true;

        while (_sentenceQueue.TryDequeue(out string text))
        {
            await Task.Run(() => SynthesizeSpeechToAudioDataAsync(_speechSynthesizer, text, speechSynthProvider));
            //Debug.Log("New Task run");
        }

        _isSynthesizing = false;
    }

    private static async Task SynthesizeSpeechToAudioDataAsync(SpeechSynthesizer synthesizer, string text,
        SpeechSynthProvider speechSynthProvider)
    {
        using var result = await synthesizer.StartSpeakingTextAsync(text).ConfigureAwait(false);

        if (result.Reason == ResultReason.SynthesizingAudioStarted)
        {
            using var audioDataStream = AudioDataStream.FromResult(result);
            byte[] buffer = new byte[32000];
            uint bytesRead;


            while ((bytesRead = audioDataStream.ReadData(buffer)) > 0)
            {
                byte[] audioData = new byte[bytesRead];
                Array.Copy(buffer, audioData, bytesRead);

                // Convert byte data to float data using AudioConversionJob
                NativeArray<byte> inputArray = new NativeArray<byte>(audioData, Allocator.TempJob);
                NativeArray<float> outputArray = new NativeArray<float>(audioData.Length / 2, Allocator.TempJob);

                AudioConversionJob job = new AudioConversionJob
                {
                    Input = inputArray,
                    Output = outputArray
                };

                JobHandle handle = job.Schedule(audioData.Length / 2, 64);
                handle.Complete();

                float[] samples = new float[outputArray.Length];
                outputArray.CopyTo(samples);
                speechSynthProvider.EnqueueSamples(samples);
                //Debug.Log("Enqueued Speech Samples");
                inputArray.Dispose();
                outputArray.Dispose();
            }
        }
        else if (result.Reason == ResultReason.Canceled)
        {
            var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
            Debug.LogError($"CANCELED: Reason={cancellation.Reason}");
            if (cancellation.Reason == CancellationReason.Error)
            {
                Debug.LogError($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                Debug.LogError($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
            }
        }
    }
}