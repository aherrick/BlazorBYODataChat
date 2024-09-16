using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using Server.Models;

namespace Server.Services;

#pragma warning disable SKEXP0020,SKEXP0001, SKEXP0010

public class AzureAIMemoryService
{
    public ISemanticTextMemory Instanace { get; }

    public AzureAIMemoryService(
        Configuration.AzureOpenAITextEmbedding azureOpenAITextEmbedding,
        Configuration.AzureAISearch azureAISearchConfig
    )
    {
        var acsMS = new AzureAISearchMemoryStore(
            endpoint: azureAISearchConfig.Endpoint,
            apiKey: azureAISearchConfig.Key
        );

        Instanace = new MemoryBuilder()
            .WithTextEmbeddingGeneration(
                (loggerFactory, httpClient) =>
                {
                    return new AzureOpenAITextEmbeddingGenerationService(
                        deploymentName: azureOpenAITextEmbedding.DeploymentName,
                        endpoint: azureOpenAITextEmbedding.Endpoint,
                        apiKey: azureOpenAITextEmbedding.Key,
                        httpClient: httpClient,
                        loggerFactory: loggerFactory
                    );
                }
            )
            .WithMemoryStore(acsMS)
            .Build();
    }
}