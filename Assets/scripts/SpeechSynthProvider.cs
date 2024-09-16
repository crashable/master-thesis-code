using System.Collections.Concurrent;
using AudioBufferBurstLibrary;

public class SpeechSynthProvider : SynthProvider
{
    private ConcurrentQueue<float> _audioDataQueueRead = new ();
    private ConcurrentQueue<float> _audioDataQueueWrite = new ();

    public void EnqueueSamples(float[] samples)
    {
        foreach (var sample in samples)
        {
            _audioDataQueueWrite.Enqueue(sample);
        }
    }

    public new void Clear()
    {
        base.Clear();
        _audioDataQueueRead.Clear();
        _audioDataQueueWrite.Clear();
    }

    protected override void ProcessBuffer(ref SynthBuffer buffer)
    {
        ProcessAudio(ref buffer, _audioDataQueueRead);

        // Swap the queues if the read queue is empty
        if (_audioDataQueueRead.IsEmpty)
        {
            (_audioDataQueueRead, _audioDataQueueWrite) = (_audioDataQueueWrite, _audioDataQueueRead);
        }
    }

    private static void ProcessAudio(ref SynthBuffer buffer, ConcurrentQueue<float> audioDataQueue)
    {
        for (int i = 0; i < buffer.Length; i += buffer.Channels)
        {
            if (audioDataQueue.TryDequeue(out float sample))
            {
                buffer[i] = sample;
                if (buffer.Channels == 2 && i + 1 < buffer.Length)
                {
                    buffer[i + 1] = sample; // Duplicate the mono data
                }
            }
            else
            {
                buffer[i] = 0; // Fill remaining with zeros
                if (buffer.Channels == 2 && i + 1 < buffer.Length)
                {
                    buffer[i + 1] = 0;
                }
            }
        }
    }
}