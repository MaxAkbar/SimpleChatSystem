using SimpleChatSystem;
using System.Net;
using System.Text;
using System.Text.Json;

var ollamaEndpoint = "http://127.0.0.1:11434";
var ollamaClient = new HttpClient
{
    BaseAddress = new Uri(ollamaEndpoint)
};
var modeName = await SelectOllamaModel(ollamaClient);

await StartChat(ollamaClient, modeName);
Console.WriteLine("Exiting the application.");

static async Task<bool> ChatWithModel(HttpClient ollamaClient, ChatRequest chatRequest)
{
    Console.Write("User > ");

    var endOfConversation = false;
    var userInput = Console.ReadLine();

    if (userInput == "/bye" || string.IsNullOrWhiteSpace(userInput))
    {
        return endOfConversation = false;
    }

    ChatResponse? chatResponse = await ChatCompletion(ollamaClient, chatRequest, userInput);

    if (chatResponse != null)
    {
        var assistantMessage = new Message { Role = chatResponse.Message.Role, Content = chatResponse.Message.Content };
        chatRequest.Messages.Add(assistantMessage);
        Console.WriteLine($"{assistantMessage.Role} > {assistantMessage.Content}");

        endOfConversation = true;
    }
    else
    {
        Console.WriteLine("Failed to deserialize the response.");

        endOfConversation = false;
    }

    return endOfConversation;
}

static async Task<string> SelectOllamaModel(HttpClient ollamaClient)
{
    var responseMessage = await ollamaClient.GetAsync("/api/tags");
    var content = await responseMessage.Content.ReadAsStringAsync();

    if (responseMessage != null && responseMessage.StatusCode == HttpStatusCode.OK && content != null)
    {
        // Deserialize the JSON string to the ModelsResponse object
        ModelsResponse modelsResponse = JsonSerializer.Deserialize<ModelsResponse>(content)!;

        if (modelsResponse != null)
        {
            // Output the deserialized object
            for (int i = 0; i < modelsResponse.Models.Count; i++)
            {
                Model? model = modelsResponse.Models[i];
                Console.WriteLine($"({i}) {model.Name}");
            }

            Console.WriteLine();
            Console.WriteLine("Please use the numeric value for the model to interact with.");

            var userInput = Console.ReadLine();

            if (!int.TryParse(userInput, out int modelIndex) || modelIndex < 0 || modelIndex >= modelsResponse.Models.Count)
            {
                Console.WriteLine("Invalid model index.");

                return string.Empty;
            }

            return modelsResponse.Models[modelIndex].Name;
        }
    }

    return string.Empty;
}

static async Task StartChat(HttpClient ollamaClient, string modeName)
{
    if (modeName != string.Empty)
    {
        // chat with the model
        Console.Clear();
        // prompt the user if they want to chat with the model or a knowledge base
        Console.WriteLine("Hello I am a friendly AI assistant. How can I help you?");
        Console.WriteLine("To end the chat type /bye");
        Console.WriteLine();

        var chatRequest = new ChatRequest
        {
            Model = modeName,
            Messages = [],
            Stream = false
        };
        var userMessage = new Message { Role = "system", Content = "You are a helpfull assistant." };

        chatRequest.Messages.Add(userMessage);

        while (await ChatWithModel(ollamaClient, chatRequest)) ;
    }
}

static async Task<ChatResponse?> ChatCompletion(HttpClient ollamaClient, ChatRequest chatRequest, string userInput)
{
    var userMessage = new Message { Role = "user", Content = userInput };

    chatRequest.Messages.Add(userMessage);

    var chatRequestJson = JsonSerializer.Serialize(chatRequest);
    var content = new StringContent(chatRequestJson, Encoding.UTF8, "application/json");
    var responseMessage = await ollamaClient.PostAsync("/api/chat", content);
    var llmResponse = await responseMessage.Content.ReadAsStringAsync();
    var chatResponse = JsonSerializer.Deserialize<ChatResponse>(llmResponse);
    return chatResponse;
}