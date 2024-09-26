using Microsoft.KernelMemory;
using Server.Models;

namespace Server.Services;

public class AzureAIMemoryService
{
    public IKernelMemory Instanace { get; }

    public AzureAIMemoryService(
        Configuration.AzureOpenAITextEmbedding azureOpenAITextEmbedding,
        Configuration.AzureOpenAIChat azureOpenAIChat,
        Configuration.AzureAISearch azureAISearchConfig
    )
    {
        Instanace = new KernelMemoryBuilder()
            .WithAzureOpenAITextGeneration(
                new AzureOpenAIConfig()
                {
                    APIType = AzureOpenAIConfig.APITypes.ChatCompletion,
                    Deployment = azureOpenAIChat.DeploymentName,
                    Endpoint = azureOpenAIChat.Endpoint,
                    APIKey = azureOpenAIChat.Key,
                    Auth = AzureOpenAIConfig.AuthTypes.APIKey
                }
            )
            .WithAzureOpenAITextEmbeddingGeneration(
                new AzureOpenAIConfig()
                {
                    APIType = AzureOpenAIConfig.APITypes.EmbeddingGeneration,
                    Deployment = azureOpenAITextEmbedding.DeploymentName,
                    Endpoint = azureOpenAITextEmbedding.Endpoint,
                    APIKey = azureOpenAITextEmbedding.Key,
                    Auth = AzureOpenAIConfig.AuthTypes.APIKey
                }
            )
            .WithAzureAISearchMemoryDb(
                new AzureAISearchConfig()
                {
                    Endpoint = azureAISearchConfig.Endpoint,
                    APIKey = azureAISearchConfig.Key,
                    Auth = AzureAISearchConfig.AuthTypes.APIKey,
                    UseHybridSearch = true
                }
            )
            .WithSearchClientConfig(
                new SearchClientConfig
                {
                    MaxMatchesCount = 3,
                    Temperature = 0,
                    TopP = 0
                }
            )
            .Build<MemoryServerless>();
    }
}