// Create kernel
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using System.Text;

#region Configuration

var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var azureOpenAIConfig = config.GetSection(nameof(AzureOpenAI)).Get<AzureOpenAI>();
var azureAISearchConfig = config.GetSection(nameof(AzureAISearch)).Get<AzureAISearch>();

var builder = Kernel.CreateBuilder()
        .AddAzureOpenAIChatCompletion(deploymentName: azureOpenAIConfig.DeploymentName,
                                      endpoint: azureOpenAIConfig.Endpoint,
                                      apiKey: azureOpenAIConfig.Key);

//#pragma warning disable SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
//builder.Plugins.AddFromType<ConversationSummaryPlugin>();
//#pragma warning restore SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var kernel = builder.Build();

#pragma warning disable SKEXP0011 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0021 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var memory = new MemoryBuilder()
      //.WithLoggerFactory(ConsoleLogger.LoggerFactory)
      .WithAzureOpenAITextEmbeddingGeneration(deploymentName: azureAISearchConfig.DeploymentName, endpoint: azureOpenAIConfig.Endpoint,
                                                modelId: azureAISearchConfig.ModelName, apiKey: azureOpenAIConfig.Key)
      .WithMemoryStore(new AzureAISearchMemoryStore(endpoint: azureAISearchConfig.Endpoint, apiKey: azureAISearchConfig.Key))
      .Build();
#pragma warning restore SKEXP0021 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

#pragma warning restore SKEXP0011 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

#endregion Configuration

// Load prompts
var prompts = kernel.CreatePluginFromPromptDirectory("Prompts");

var promptCurrent = "chat2";

// Create chat history
ChatHistory history = [];

// Start the chat loop
while (true)
{
    // Get user input
    Console.Write("User > ");
    var request = Console.ReadLine();

    var additionalDataBuilder = new StringBuilder();

    await foreach (var result in memory.SearchAsync(azureAISearchConfig.IndexName, request, limit: 5, minRelevanceScore: 0.5))
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

    // Append to history
    history.AddUserMessage(request);
    history.AddAssistantMessage(message);
}

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