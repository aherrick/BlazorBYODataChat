using Azure.AI.OpenAI;
using OpenAI.Chat;
using Server.Models;
using System.ClientModel.Primitives;

namespace Server.Services;

public class AzureAIChatCompletionService(Configuration.AzureOpenAIChat azureOpenAIConfig)
{
    public ChatClient Instanace { get; } =
        new AzureOpenAIClient(
            new Uri(azureOpenAIConfig.Endpoint),
            new System.ClientModel.ApiKeyCredential(azureOpenAIConfig.Key),
            new AzureOpenAIClientOptions()
            {
                RetryPolicy = new ClientRetryPolicy(3)
            }
        ).GetChatClient(azureOpenAIConfig.DeploymentName);
}