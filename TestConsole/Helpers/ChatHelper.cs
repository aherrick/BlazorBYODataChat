using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using System.Text;
using System.Text.Json;
using TestConsole.Models;

namespace TestConsole.Helpers;

public class ChatHelper
{
    #region Configuration

    private Kernel Kernel { get; }
#pragma warning disable SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    private ISemanticTextMemory Memory { get; }
#pragma warning restore SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    private AzureOpenAIChatCompletionWithDataService ChatCompletionWithData { get; }
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    private Kernel KernelChatWData { get; }

    private Configuration.AzureAISearch AzureAISearchConfig { get; }

    public ChatHelper()
    {
        #region Configuration

        var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
        var azureOpenAIConfig = config.GetSection(nameof(Configuration.AzureOpenAI)).Get<Configuration.AzureOpenAI>();
        AzureAISearchConfig = config.GetSection(nameof(Configuration.AzureAISearch)).Get<Configuration.AzureAISearch>();

        var builder = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(deploymentName: azureOpenAIConfig.DeploymentName,
                                              endpoint: azureOpenAIConfig.Endpoint,
                                              apiKey: azureOpenAIConfig.Key);

        Kernel = builder.Build();

#pragma warning disable SKEXP0011 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0021 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        Memory = new MemoryBuilder()
              .WithAzureOpenAITextEmbeddingGeneration(deploymentName: AzureAISearchConfig.DeploymentName, endpoint: azureOpenAIConfig.Endpoint,
                                                        modelId: AzureAISearchConfig.ModelName, apiKey: azureOpenAIConfig.Key)
              .WithMemoryStore(new AzureAISearchMemoryStore(endpoint: AzureAISearchConfig.Endpoint, apiKey: AzureAISearchConfig.Key))
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
            DataSourceApiKey = AzureAISearchConfig.Key,
            DataSourceEndpoint = AzureAISearchConfig.Endpoint,
            DataSourceIndex = AzureAISearchConfig.IndexName
        };

        ChatCompletionWithData = new AzureOpenAIChatCompletionWithDataService(chatCompWithDataConfig);

        KernelChatWData = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(config: chatCompWithDataConfig)
            .Build();

#pragma warning restore SKEXP0010

        #endregion Configuration
    }

    #endregion Configuration

    public async Task ExecuteKernelChatWithDataFunction(string query)
    {
        var function = KernelChatWData.CreateFunctionFromPrompt("Question: {{$input}}");

        await foreach (StreamingKernelContent response in KernelChatWData.InvokeStreamingAsync(function, new() { ["input"] = query }))
        {
            Console.Write(response.ToString());
        }
    }

    public async Task ExecuteKernelChatWithData(string query)
    {
        var chatHistory = new ChatHistory();

        chatHistory.AddUserMessage(query);

        // Chat Completion example
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var chatMessage = (AzureOpenAIWithDataChatMessageContent)await ChatCompletionWithData.GetChatMessageContentAsync(chatHistory);
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        var response = chatMessage.Content;
        Console.WriteLine(response);

        var toolResponse = JsonSerializer.Deserialize<SkToolContent.Rootobject>(chatMessage.ToolContent);
        Console.WriteLine(JsonSerializer.Serialize(toolResponse, new JsonSerializerOptions { WriteIndented = true }));
    }

    public async Task ExecuteChatCompletionFun()
    {
        AzureOpenAIChatCompletionService chatCompletionService = (AzureOpenAIChatCompletionService)Kernel.Services.GetService(typeof(IChatCompletionService));

        ChatHistory history = new(systemMessage: "You are batman. If asked who you are, say 'I am Batman!' then answer the users question and respond like a valley girl. Don't ask me questions back.");

        await HandleChatCompletion("tell me about you?", chatCompletionService, history);
        await HandleChatCompletion("do you have a sidekick?", chatCompletionService, history);
        await HandleChatCompletion("what is your favorite weapon?", chatCompletionService, history);
    }

    private static async Task HandleChatCompletion(string userPrompt, AzureOpenAIChatCompletionService service, ChatHistory history)
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

    public async Task ExecuteChatWAiSearch()
    {
        // Load prompts
        var prompts = Kernel.CreatePluginFromPromptDirectory("Prompts");

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

            await foreach (var result in Memory.SearchAsync(AzureAISearchConfig.IndexName, request, limit: 7, minRelevanceScore: 0.7))
            {
                additionalDataBuilder.AppendLine(result.Metadata.Text);
            }

            // Get chat response
            var chatResult = Kernel.InvokeStreamingAsync<StreamingChatMessageContent>(
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
    }
}