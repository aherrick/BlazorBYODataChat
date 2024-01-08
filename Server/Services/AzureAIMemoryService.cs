using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using Server.Models;

namespace Server.Services;

public class AzureAIMemoryService
{
#pragma warning disable SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public ISemanticTextMemory Instanace { get; }
#pragma warning restore SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    public AzureAIMemoryService(Configuration.AzureOpenAI azureOpenAIConfig, Configuration.AzureAISearch azureAISearchConfig)
    {
#pragma warning disable SKEXP0021 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var acsMS = new AzureAISearchMemoryStore(endpoint: azureAISearchConfig.Endpoint, apiKey: azureAISearchConfig.Key);
#pragma warning restore SKEXP0021 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

#pragma warning disable SKEXP0011 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        Instanace = new MemoryBuilder()
              //.WithLoggerFactory(ConsoleLogger.LoggerFactory)
              .WithAzureOpenAITextEmbeddingGeneration(deploymentName: azureAISearchConfig.DeploymentName, endpoint: azureOpenAIConfig.Endpoint,
                                                        modelId: azureAISearchConfig.ModelName, apiKey: azureOpenAIConfig.Key
               )
              .WithMemoryStore(acsMS)
              .Build();
#pragma warning restore SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

#pragma warning restore SKEXP0011 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }
}