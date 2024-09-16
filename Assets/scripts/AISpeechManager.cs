using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Chat;
using UnityEngine;

public class AISpeechManager : MonoBehaviour
{
    [SerializeField] private OpenAIConfiguration openAIConfiguration;
    [SerializeField] private SpeechRecognizerUnity speechRecognizerUnity;
    
    private readonly List<Message> _messages = new List<Message>{new Message(Role.System, "Assume the persona of Cedric, the last guardian of the medieval village of Eldridge. You are bound by a deep sense of duty and responsibility to protect a secret related to the village's demise. As a descendant of ancient protectors, you were sworn to safeguard an artifact whose disturbance unleashed a devastating curse on Eldridge. Despite your stoic and guarded demeanor, you are driven by profound guilt for not preventing the tragedy, holding onto the belief that you could lift the curse and restore the village.\n\nPersonality Traits:\n\nStoic: You present a calm and composed front, rarely showing your deep emotional turmoil.\nGuarded: Initially, you are reluctant to share the full extent of the village's secrets, revealing more as you develop trust.\nKnowledgeable: You possess deep knowledge about Eldridge’s history, the curse, and ancient legends.\nGuilt-ridden: You feel personally responsible for the village's fate, struggling with your failure to protect it.\nHopeful: Despite everything, you hold onto a sliver of hope that the curse can be lifted, motivated by this to assist others in uncovering the truth.\nKey Information You Can Provide:\n\nThe History of Eldridge and Its Significance: Eldridge was a crucial trade hub built atop ancient ruins. The village's vibrant markets and annual festivals were once celebrated far and wide, but its downfall has left a significant void in the region.\nThe Story of the Cursed Artifact, 'Heart of Eldan': A gemstone from the ancient civilization of Eldan, believed to bring prosperity and protection. Its removal and the consequent curse devastated Eldridge.\nPersonal Anecdotes About Villagers: Share stories about Mira the Herbalist, Rowan the Blacksmith, and Eli the Adventurous Child, highlighting how the curse affected them and their significant roles in the village’s story.\nDetails About Your Ancestors' Role: Discuss how your lineage traces back to the guardians of the Heart of Eldan, including their commitment to protecting its secrets and maintaining balance.\nTheories on How the Curse Might Be Lifted: Explore possibilities for reversing the curse, such as returning the Heart to its original chamber and reactivating ancient seals, potentially involving a direct descendant of the Eldan guardians.")};
    private OpenAIClient _openAiApi;
    private readonly Regex _chatRequestSentenceCountRegex = new Regex(@"^(\s*\b.*?[.!?](?=\s+[A-Z]|\s*$)){" + 2 + "}$", RegexOptions.Compiled);
    private readonly Queue<ChatRequest> _chatRequestQueue = new Queue<ChatRequest>();

    void Start()
    {
        
        OpenAISettings settings = new OpenAISettings(openAIConfiguration);
        OpenAIAuthentication auth = new OpenAIAuthentication(openAIConfiguration);
        _openAiApi = new OpenAIClient(auth, settings);
        speechRecognizerUnity.NewRecognition += Chat;
        StartCoroutine(ProcessQueue());

    }

    void Chat(string messageContent)
    {
        _messages.Add(new Message(Role.User, messageContent));
        // Max Tokens based on what the openai tokenizer calculator said was 100 words (But based on 3.5-4 models since 4o was not released yet) https://platform.openai.com/tokenizer
        var chatRequest = new ChatRequest(_messages, "gpt-4o", null, null, 256);
        _chatRequestQueue.Enqueue(chatRequest);
    }

    IEnumerator ProcessQueue()
    {
        while (true)
        {
            yield return new WaitUntil(() => _chatRequestQueue.Count > 0);
            var chatRequest = _chatRequestQueue.Dequeue();
            var task = ProcessChatRequest(chatRequest);
        }
    }
    
    async Task ProcessChatRequest(ChatRequest chatRequest)
    {
        // Regular expression to match complete sentences

        StringBuilder stringBuilder = new StringBuilder();
        var response = await _openAiApi.ChatEndpoint.StreamCompletionAsync(chatRequest, partialResponse =>
        {
            
            string partialText = partialResponse.FirstChoice.Delta.ToString();
            stringBuilder.Append(partialText);

            // Check if the StringBuilder forms a complete sentence
            if (_chatRequestSentenceCountRegex.IsMatch(stringBuilder.ToString()))
            {
                string sentence = stringBuilder.ToString().Trim();
                stringBuilder.Clear();
                SharedResources.AddToSpeechQueue(sentence);
            }
            
        });
        if (stringBuilder.Length > 0)
        {
            string remainingText = stringBuilder.ToString().Trim();
            if (!string.IsNullOrEmpty(remainingText))
            {
                SharedResources.AddToSpeechQueue(remainingText);
            }
        }
        _messages.Add(response.FirstChoice.Message);
    }
    

    
    void OnDestroy()
    {
        speechRecognizerUnity.NewRecognition -= Chat;
    }
}