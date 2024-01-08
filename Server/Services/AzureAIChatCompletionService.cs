using Microsoft.SemanticKernel;
using Server.Models;

namespace Server.Services;

public class AzureAIChatCompletionService
{
    public Kernel Instanace { get; }

    public AzureAIChatCompletionService(Configuration.AzureOpenAI azureOpenAIConfig)
    {
        Instanace = Kernel.CreateBuilder()
                                 .AddAzureOpenAIChatCompletion(deploymentName: azureOpenAIConfig.DeploymentName, endpoint: azureOpenAIConfig.Endpoint, apiKey: azureOpenAIConfig.Key).Build();

        var pluginsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Plugins", "chat");
        Instanace.ImportPluginFromPromptDirectory(pluginsDirectory);
    }
}