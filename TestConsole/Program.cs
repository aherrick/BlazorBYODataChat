////using TestConsole.Helpers;

////var chatHelper = new ChatHelper();

////await chatHelper.ExecuteInMemoryKernel();

////Console.Read();

//using Microsoft.SemanticKernel.ChatCompletion;
//using Microsoft.SemanticKernel.Connectors.OpenAI;
//using Microsoft.SemanticKernel;

//var kernel = Kernel.CreateBuilder()
//           .AddAzureOpenAIChatCompletion(
//               TestConfiguration.AzureOpenAI.ChatDeploymentName,
//               TestConfiguration.AzureOpenAI.Endpoint,
//               TestConfiguration.AzureOpenAI.ApiKey)
//           .Build();

//var chatHistory = new ChatHistory();

//// First question without previous context based on uploaded content.
//var ask = "How did Emily and David meet?";
//chatHistory.AddUserMessage(ask);

//// Chat Completion example
//var chatExtensionsOptions = GetAzureChatExtensionsOptions();
//var promptExecutionSettings = new OpenAIPromptExecutionSettings { op AzureChatExtensionsOptions = chatExtensionsOptions };

//var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

//var chatMessage = await chatCompletion.GetChatMessageContentAsync(chatHistory, promptExecutionSettings);

//var response = chatMessage.Content!;

//// Output
//// Ask: How did Emily and David meet?
//// Response: Emily and David, both passionate scientists, met during a research expedition to Antarctica.
//Console.WriteLine($"Ask: {ask}");
//Console.WriteLine($"Response: {response}");
//Console.WriteLine();

//// Chat history maintenance
//chatHistory.AddAssistantMessage(response);

//// Second question based on uploaded content.
//ask = "What are Emily and David studying?";
//chatHistory.AddUserMessage(ask);

//// Chat Completion Streaming example
//Console.WriteLine($"Ask: {ask}");
//Console.WriteLine("Response: ");

//await foreach (var word in chatCompletion.GetStreamingChatMessageContentsAsync(chatHistory, promptExecutionSettings))
//{
//    Console.Write(word);
//}

// static AzureChatExtensionsOptions GetAzureChatExtensionsOptions()
//{
//    var azureSearchExtensionConfiguration = new AzureSearchChatExtensionConfiguration
//    {
//        SearchEndpoint = new Uri(TestConfiguration.AzureAISearch.Endpoint),
//        Authentication = new OnYourDataApiKeyAuthenticationOptions(TestConfiguration.AzureAISearch.ApiKey),
//        IndexName = TestConfiguration.AzureAISearch.IndexName
//    };

//    return new AzureChatExtensionsOptions
//    {
//        Extensions = { azureSearchExtensionConfiguration }
//    };
//}

// Extension methods to use data sources with options are subject to SDK surface changes. Suppress the
// warning to acknowledge and this and use the subject-to-change AddDataSource method.
#pragma warning disable AOAI001

using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using OpenAI.Chat;

ChatCompletionOptions options = new();
options.AddDataSource(
    new AzureSearchChatDataSource()
    {
        Endpoint = new Uri("https://your-search-resource.search.windows.net"),
        IndexName = "contoso-products-index",
        Authentication = DataSourceAuthentication.FromApiKey(
            Environment.GetEnvironmentVariable("OYD_SEARCH_KEY")
        ),
    }
);

ChatCompletion completion = chatClient.CompleteChat(
    [new UserChatMessage("What are the best-selling Contoso products this month?"),],
    options
);

AzureChatMessageContext onYourDataContext = completion.GetAzureMessageContext();

if (onYourDataContext?.Intent is not null)
{
    Console.WriteLine($"Intent: {onYourDataContext.Intent}");
}
foreach (AzureChatCitation citation in onYourDataContext?.Citations ?? [])
{
    Console.WriteLine($"Citation: {citation.Content}");
}