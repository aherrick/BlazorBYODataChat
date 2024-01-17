// Create kernel
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using System.Text.Json;

#region Configuration

var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var azureOpenAIConfig = config.GetSection(nameof(AzureOpenAI)).Get<AzureOpenAI>();
var azureAISearchConfig = config.GetSection(nameof(AzureAISearch)).Get<AzureAISearch>();

var builder = Kernel.CreateBuilder()
        .AddAzureOpenAIChatCompletion(deploymentName: azureOpenAIConfig.DeploymentName,
                                      endpoint: azureOpenAIConfig.Endpoint,
                                      apiKey: azureOpenAIConfig.Key);

var kernel = builder.Build();

#pragma warning disable SKEXP0011 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0021 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var memory = new MemoryBuilder()
      .WithAzureOpenAITextEmbeddingGeneration(deploymentName: azureAISearchConfig.DeploymentName, endpoint: azureOpenAIConfig.Endpoint,
                                                modelId: azureAISearchConfig.ModelName, apiKey: azureOpenAIConfig.Key)
      .WithMemoryStore(new AzureAISearchMemoryStore(endpoint: azureAISearchConfig.Endpoint, apiKey: azureAISearchConfig.Key))
      .Build();
#pragma warning restore SKEXP0021 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

#pragma warning restore SKEXP0011 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var chatCompWithDataConfig = new AzureOpenAIChatCompletionWithDataConfig
{
    CompletionModelId = azureOpenAIConfig.DeploymentName,
    CompletionEndpoint = azureOpenAIConfig.Endpoint,
    CompletionApiKey = azureOpenAIConfig.Key,
    DataSourceApiKey = azureAISearchConfig.Key,
    DataSourceEndpoint = azureAISearchConfig.Endpoint,
    DataSourceIndex = azureAISearchConfig.IndexName
};

var chatCompletionWithData = new AzureOpenAIChatCompletionWithDataService(chatCompWithDataConfig);

Kernel kernelChatWData = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(config: chatCompWithDataConfig)
    .Build();

#pragma warning restore SKEXP0010

#endregion Configuration

var ask = "what is scc";

///
/// (1) chat w/ data
///

var function = kernelChatWData.CreateFunctionFromPrompt("Question: {{$input}}");

await foreach (StreamingKernelContent response in kernelChatWData.InvokeStreamingAsync(function, new() { ["input"] = ask }))
{
    Console.Write(response.ToString());
}

///
/// (2) chat w/ data
///

/*
var chatHistory = new ChatHistory();

chatHistory.AddUserMessage(ask);

// Chat Completion example
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var chatMessage = (AzureOpenAIWithDataChatMessageContent)await chatCompletionWithData.GetChatMessageContentAsync(chatHistory);
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var response = chatMessage.Content;
Console.WriteLine(response);

var toolResponse = JsonSerializer.Deserialize<RootobjectToolContent>(chatMessage.ToolContent);
Console.WriteLine(JsonSerializer.Serialize(toolResponse, new JsonSerializerOptions { WriteIndented = true }));
*/

///
/// CHAT COMPLETION FUN
///

/*

AzureOpenAIChatCompletionService chatCompletionService = (AzureOpenAIChatCompletionService)kernel.Services.GetService(typeof(IChatCompletionService));

ChatHistory history = new(systemMessage: "You are batman. If asked who you are, say 'I am Batman!' then answer the users question and respond like a valley girl. Don't ask me questions back.");

await HandleChatCompletion("tell me about you?", chatCompletionService, history);
await HandleChatCompletion("do you have a sidekick?", chatCompletionService, history);
await HandleChatCompletion("what is your favorite weapon?", chatCompletionService, history);

*/

///
/// CHAT W/ AI SEARCH
///

/*

// Load prompts
var prompts = kernel.CreatePluginFromPromptDirectory("Prompts");

var promptCurrent = "chat6";

// Create chat history
ChatHistory history = [];

// Start the chat loop
while (true)
{
    // Get user input
    Console.Write("user > ");
    var request = Console.ReadLine();

    var additionalDataBuilder = new StringBuilder();

    await foreach (var result in memory.SearchAsync(azureAISearchConfig.IndexName, request, limit: 7, minRelevanceScore: 0.7))
    {
        additionalDataBuilder.AppendLine(result.Metadata.Text);
    }

    // Get chat response
    var chatResult = kernel.InvokeStreamingAsync<StreamingChatMessageContent>(
        prompts[promptCurrent],
        new() {
            { "request", request },
            { "history", string.Join("\n", history.Select(x => x.Role + ": " + x.Content)) },
            { "context", additionalDataBuilder.ToString() }
        }
    );

    // Stream the response
    string message = "";
    await foreach (var chunk in chatResult)
    {
        if (chunk.Role.HasValue)
        {
            Console.Write(chunk.Role + " > ");
        }

        message += chunk;
        Console.Write(chunk);
    }
    Console.WriteLine();
    Console.WriteLine();

    // Append to history
    history.AddUserMessage(request);
    history.AddAssistantMessage(message);
}

*/

Console.Read();

static async Task HandleChatCompletion(string userPrompt, AzureOpenAIChatCompletionService service, ChatHistory history)
{
    Console.WriteLine($"user > {userPrompt}");
    history.AddUserMessage(userPrompt);

    var assistantResponse = string.Empty;
    await foreach (var resultChunk in service.GetStreamingChatMessageContentsAsync(history))
    {
        if (resultChunk.Role.HasValue)
        {
            Console.Write($"{resultChunk.Role} > ");
        }

        assistantResponse += resultChunk;
        Console.Write(resultChunk);
    }

    history.AddAssistantMessage(assistantResponse);

    Console.WriteLine();
    Console.WriteLine();
}

// config
internal class AzureOpenAI
{
    public string DeploymentName { get; set; }
    public string Endpoint { get; set; }
    public string Key { get; set; }
}

internal class AzureAISearch
{
    public string DeploymentName { get; set; }
    public string ModelName { get; set; }
    public string Endpoint { get; set; }
    public string Key { get; set; }
    public string IndexName { get; set; }
}

// ToolContent
public class RootobjectToolContent
{
    public Citation[] citations { get; set; }
    public string intent { get; set; }
}

public class Citation
{
    public string content { get; set; }
    public object id { get; set; }
    public object title { get; set; }
    public object filepath { get; set; }
    public object url { get; set; }
    public Metadata metadata { get; set; }
    public string chunk_id { get; set; }
}

public class Metadata
{
    public string chunking { get; set; }
}