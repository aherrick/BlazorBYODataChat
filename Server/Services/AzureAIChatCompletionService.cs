using Microsoft.SemanticKernel;
using Server.Models;

namespace Server.Services;

public class AzureAIChatCompletionService(Configuration.AzureOpenAI azureOpenAIConfig)
{
    public Kernel Instanace => Kernel.CreateBuilder()
                            .AddAzureOpenAIChatCompletion(deploymentName: azureOpenAIConfig.DeploymentName, endpoint: azureOpenAIConfig.Endpoint, apiKey: azureOpenAIConfig.Key).Build();
}