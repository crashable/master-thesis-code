using UnityEngine;

namespace DefaultNamespace
{
    [CreateAssetMenu(fileName = "MicrosoftSpeechSDKConfiguration", menuName = "MicrosoftSpeechSDK/ServiceConfig")]
    public class MicrosoftSpeechSDKServiceConfig : ScriptableObject
    {
        public string SubscriptionKey;
        public string Region;
        public string VoiceName;
    }
}