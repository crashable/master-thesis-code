using System;
using System.Collections.Concurrent;
using UnityEngine;

public static class SharedResources
{
    private static readonly ConcurrentQueue<string> SpeechQueue = new ConcurrentQueue<string>();
    public static event Action OnNewDataAvailable;

    // Method to add a new message to the queue
    public static void AddToSpeechQueue(string message)
    {
        try
        {
            SpeechQueue.Enqueue(message);
            OnNewDataAvailable?.Invoke();
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine($"Error: {e.Message}");
        }
    }

    // Method to retrieve and remove the next message from the queue
    public static bool TryGetNextSpeech(out string speechData)
    {
        return SpeechQueue.TryDequeue(out speechData);
    }
}
