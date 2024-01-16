using Microsoft.SemanticKernel.Connectors.OpenAI;
using Server.Models;

namespace Server.Services;

public class AzureAIChatCompletionWithDataService
{
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public AzureOpenAIChatCompletionWithDataService Instanace { get; }
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    public AzureAIChatCompletionWithDataService(Configuration.AzureOpenAI azureOpenAIConfig, Configuration.AzureAISearch azureAISearchConfig) =>
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        Instanace = new AzureOpenAIChatCompletionWithDataService(new AzureOpenAIChatCompletionWithDataConfig
        {
            CompletionModelId = azureOpenAIConfig.DeploymentName,
            CompletionEndpoint = azureOpenAIConfig.Endpoint,
            CompletionApiKey = azureOpenAIConfig.Key,
            DataSourceApiKey = azureAISearchConfig.Key,
            DataSourceEndpoint = azureAISearchConfig.Endpoint,
            DataSourceIndex = azureAISearchConfig.IndexName
        });

#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}