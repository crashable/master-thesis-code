using System;
using DefaultNamespace;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using UnityEngine;

public class SpeechRecognizerUnity : MonoBehaviour
{
    [SerializeField] private MicrosoftSpeechSDKServiceConfig speechSDKServiceConfig;
    
    private SpeechRecognizer _recognizerUnity;
    private SpeechConfig _speechConfig;

    // Default timeouts and threshold values
    private float _currentTimeoutMs = 1500;
    private const float MaxTimeout = 2500;
    private const float MinTimeout = 500;
    private const float TimeoutIncrement = 100;
    private const float TimeoutDecrement = 100;
    private const float HighSpeechRateThreshold = 5.0f; // Words per second, example threshold
    private const float LowSpeechRateThreshold = 2.0f; // Words per second, example threshold

    private DateTime _lastAdjustmentTime;

    public delegate void OnSpeechRecognizedHandler(string messageContent);
    public event OnSpeechRecognizedHandler NewRecognition;

    void Start()
    {
        _speechConfig = SpeechConfig.FromSubscription(speechSDKServiceConfig.SubscriptionKey, speechSDKServiceConfig.Region);
        SetSegmentationSilenceTimeout(_currentTimeoutMs);
        _recognizerUnity = new SpeechRecognizer(_speechConfig, AudioConfig.FromDefaultMicrophoneInput());
        _recognizerUnity.Recognizing += OnRecognizing;
        _recognizerUnity.Recognized += OnRecognized;

        _recognizerUnity.StartContinuousRecognitionAsync().ConfigureAwait(false);
        _lastAdjustmentTime = DateTime.Now;
    }
    
    private void OnDestroy()
    {
        _recognizerUnity.StopContinuousRecognitionAsync().Wait();
        _recognizerUnity.Recognizing -= OnRecognizing;
        _recognizerUnity.Recognized -= OnRecognized;
        _recognizerUnity.Dispose();
    }

    private void OnRecognizing(object sender, SpeechRecognitionEventArgs e)
    {
        if (e.Result.Reason == ResultReason.RecognizingSpeech)
        {
            AdjustTimeoutBasedOnSpeechRate(e.Result.Text);
        }
    }

    private void OnRecognized(object sender, SpeechRecognitionEventArgs e)
    {
        if (e.Result.Reason == ResultReason.RecognizedSpeech && e.Result.Text.Length > 0)
        {
            NewRecognition?.Invoke(e.Result.Text);
        }
    }

    private void AdjustTimeoutBasedOnSpeechRate(string interimText)
    {
        float speechRate = CalculateSpeechRate(interimText);

        if (speechRate > HighSpeechRateThreshold)
        {
            _currentTimeoutMs = IncreaseTimeout();
        }
        else if (speechRate < LowSpeechRateThreshold)
        {
            _currentTimeoutMs = DecreaseTimeout();
        }

        SetSegmentationSilenceTimeout(_currentTimeoutMs);
        _lastAdjustmentTime = DateTime.Now;
    }

    private float IncreaseTimeout()
    {
        return Math.Min(MaxTimeout, _currentTimeoutMs + TimeoutIncrement);
    }

    private float DecreaseTimeout()
    {
        return Math.Max(MinTimeout, _currentTimeoutMs - TimeoutDecrement);
    }

    private float CalculateSpeechRate(string text)
    {
        TimeSpan duration = DateTime.Now - _lastAdjustmentTime;
        return (float)(text.Length / duration.TotalSeconds);
    }

    private void SetSegmentationSilenceTimeout(float timeoutMs)
    {
        _speechConfig.SetProperty(PropertyId.Speech_SegmentationSilenceTimeoutMs, timeoutMs.ToString());
    }
}
