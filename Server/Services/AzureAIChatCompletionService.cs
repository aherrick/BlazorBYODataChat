using Azure.AI.OpenAI;
using OpenAI.Chat;
using Server.Models;

namespace Server.Services;

public class AzureAIChatCompletionService(Configuration.AzureOpenAIChat azureOpenAIConfig)
{
    public ChatClient Instanace { get; } =
        new AzureOpenAIClient(
            new Uri(azureOpenAIConfig.Endpoint),
            new System.ClientModel.ApiKeyCredential(azureOpenAIConfig.Key)
        ).GetChatClient(azureOpenAIConfig.DeploymentName);
}