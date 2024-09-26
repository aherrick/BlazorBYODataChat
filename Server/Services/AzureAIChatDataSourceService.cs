using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using Microsoft.KernelMemory;
using OpenAI.Chat;
using Server.Models;

#pragma warning disable AOAI001

namespace Server.Services;

public class AzureAIChatDataSourceService
{
    public ChatCompletionOptions Instanace { get; }

    public AzureAIChatDataSourceService(Configuration.AzureAISearch azureAISearchConfig)
    {
        ChatCompletionOptions options = new();
        options.AddDataSource(
            new AzureSearchChatDataSource()
            {
                Endpoint = new Uri(azureAISearchConfig.Endpoint),
                IndexName = azureAISearchConfig.IndexName,
                Authentication = DataSourceAuthentication.FromApiKey(azureAISearchConfig.Key),
                InScope = azureAISearchConfig.InScope
            }
        );

        Instanace = options;
    }
}