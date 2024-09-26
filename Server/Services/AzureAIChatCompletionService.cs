using Azure;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using Microsoft.Extensions.Azure;
using Microsoft.SemanticKernel;
using OpenAI.Chat;
using Server.Models;

namespace Server.Services;

public class AzureAIChatCompletionService
{
    public ChatClient Instanace { get; }

    public AzureAIChatCompletionService(Configuration.AzureOpenAIChat azureOpenAIConfig)
    {
        Instanace = new AzureOpenAIClient(
            new Uri(azureOpenAIConfig.Endpoint),
            new AzureKeyCredential(azureOpenAIConfig.Key)
        ).GetChatClient(azureOpenAIConfig.DeploymentName);
    }
}