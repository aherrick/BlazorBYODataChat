using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using System.Text;
using System.Text.Json;
using TestConsole.Models;

namespace TestConsole.Helpers;

public class ChatHelper
{
    #region Configuration

    private Kernel Kernel { get; }
#pragma warning disable SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    private ISemanticTextMemory Memory { get; }
#pragma warning restore SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    private AzureOpenAIChatCompletionWithDataService ChatCompletionWithData { get; }
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    private Kernel KernelChatWData { get; }

    private Configuration.AzureAISearch AzureAISearchConfig { get; }

    private MemoryServerless MemoryServerless { get; }

    public ChatHelper()
    {
        #region Configuration

        var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
        var azureOpenAIConfig = config.GetSection(nameof(Configuration.AzureOpenAI)).Get<Configuration.AzureOpenAI>();
        AzureAISearchConfig = config.GetSection(nameof(Configuration.AzureAISearch)).Get<Configuration.AzureAISearch>();

        var builder = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(deploymentName: azureOpenAIConfig.DeploymentName,
                                              endpoint: azureOpenAIConfig.Endpoint,
                                              apiKey: azureOpenAIConfig.Key);

        Kernel = builder.Build();

#pragma warning disable SKEXP0011 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0021 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        Memory = new MemoryBuilder()
              .WithAzureOpenAITextEmbeddingGeneration(deploymentName: AzureAISearchConfig.DeploymentName, endpoint: azureOpenAIConfig.Endpoint,
                                                        modelId: AzureAISearchConfig.ModelName, apiKey: azureOpenAIConfig.Key)
              .WithMemoryStore(new AzureAISearchMemoryStore(endpoint: AzureAISearchConfig.Endpoint, apiKey: AzureAISearchConfig.Key))
              .Build();
#pragma warning restore SKEXP0021 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

#pragma warning restore SKEXP0011 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        var chatCompWithDataConfig = new AzureOpenAIChatCompletionWithDataConfig
        {
            CompletionModelId = azureOpenAIConfig.DeploymentName,
            CompletionEndpoint = azureOpenAIConfig.Endpoint,
            CompletionApiKey = azureOpenAIConfig.Key,
            DataSourceApiKey = AzureAISearchConfig.Key,
            DataSourceEndpoint = AzureAISearchConfig.Endpoint,
            DataSourceIndex = AzureAISearchConfig.IndexName
        };

        ChatCompletionWithData = new AzureOpenAIChatCompletionWithDataService(chatCompWithDataConfig);

        KernelChatWData = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(config: chatCompWithDataConfig)
            .Build();

#pragma warning restore SKEXP0010

        MemoryServerless = new KernelMemoryBuilder()
            .WithAzureOpenAITextGeneration(new AzureOpenAIConfig()
            {
                APIType = AzureOpenAIConfig.APITypes.ChatCompletion,
                Auth = AzureOpenAIConfig.AuthTypes.APIKey,
                APIKey = azureOpenAIConfig.Key,
                Deployment = azureOpenAIConfig.DeploymentName,
                Endpoint = azureOpenAIConfig.Endpoint,
                MaxTokenTotal = 16384
            }/*, new DefaultGPTTokenizer()*/)
            .WithAzureOpenAITextEmbeddingGeneration(new AzureOpenAIConfig()
            {
                Auth = AzureOpenAIConfig.AuthTypes.APIKey,
                APIKey = azureOpenAIConfig.Key,
                Deployment = AzureAISearchConfig.DeploymentName,
                Endpoint = azureOpenAIConfig.Endpoint,
                MaxTokenTotal = 8191
            }/*, new DefaultGPTTokenizer()*/)
            .Build<MemoryServerless>();

        #endregion Configuration
    }

    #endregion Configuration

    public async Task ExecuteKernelChatWithDataFunction(string query)
    {
        var function = KernelChatWData.CreateFunctionFromPrompt("Question: {{$input}}");

        await foreach (StreamingKernelContent response in KernelChatWData.InvokeStreamingAsync(function, new() { ["input"] = query }))
        {
            Console.Write(response.ToString());
        }
    }

    public async Task ExecuteKernelChatWithData(string query)
    {
        var chatHistory = new ChatHistory();

        chatHistory.AddUserMessage(query);

        // Chat Completion example
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var chatMessage = (AzureOpenAIWithDataChatMessageContent)await ChatCompletionWithData.GetChatMessageContentAsync(chatHistory);
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        var response = chatMessage.Content;
        Console.WriteLine(response);

        var toolResponse = JsonSerializer.Deserialize<SkCitation.Rootobject>(chatMessage.ToolContent);
        Console.WriteLine(JsonSerializer.Serialize(toolResponse, new JsonSerializerOptions { WriteIndented = true }));
    }

    public async Task ExecuteChatCompletionFun()
    {
        AzureOpenAIChatCompletionService chatCompletionService = (AzureOpenAIChatCompletionService)Kernel.Services.GetService(typeof(IChatCompletionService));

        ChatHistory history = new(systemMessage: "You are batman. If asked who you are, say 'I am Batman!' then answer the users question and respond like a valley girl. Don't ask me questions back.");

        await HandleChatCompletion("tell me about you?", chatCompletionService, history);
        await HandleChatCompletion("do you have a sidekick?", chatCompletionService, history);
        await HandleChatCompletion("what is your favorite weapon?", chatCompletionService, history);
    }

    private static async Task HandleChatCompletion(string userPrompt, AzureOpenAIChatCompletionService service, ChatHistory history)
    {
        Console.WriteLine($"user > {userPrompt}");
        history.AddUserMessage(userPrompt);

        var assistantResponse = string.Empty;
        await foreach (var resultChunk in service.GetStreamingChatMessageContentsAsync(history))
        {
            if (resultChunk.Role.HasValue)
            {
                Console.Write($"{resultChunk.Role} > ");
            }

            assistantResponse += resultChunk;
            Console.Write(resultChunk);
        }

        history.AddAssistantMessage(assistantResponse);

        Console.WriteLine();
        Console.WriteLine();
    }

    public async Task ExecuteChatWAiSearch()
    {
        // Load prompts
        var prompts = Kernel.CreatePluginFromPromptDirectory("Prompts");

        var promptCurrent = "chat6";

        // Create chat history
        ChatHistory history = [];

        // Start the chat loop
        while (true)
        {
            // Get user input
            Console.Write("user > ");
            var request = Console.ReadLine();

            var additionalDataBuilder = new StringBuilder();

            await foreach (var result in Memory.SearchAsync(AzureAISearchConfig.IndexName, request, limit: 7, minRelevanceScore: 0.7))
            {
                additionalDataBuilder.AppendLine(result.Metadata.Text);
            }

            // Get chat response
            var chatResult = Kernel.InvokeStreamingAsync<StreamingChatMessageContent>(
                prompts[promptCurrent],
                new() {
            { "request", request },
            { "history", string.Join("\n", history.Select(x => x.Role + ": " + x.Content)) },
            { "context", additionalDataBuilder.ToString() }
                }
            );

            // Stream the response
            string message = "";
            await foreach (var chunk in chatResult)
            {
                if (chunk.Role.HasValue)
                {
                    Console.Write(chunk.Role + " > ");
                }

                message += chunk;
                Console.Write(chunk);
            }
            Console.WriteLine();
            Console.WriteLine();

            // Append to history
            history.AddUserMessage(request);
            history.AddAssistantMessage(message);
        }
    }

    public async Task ExecuteInMemoryKernel()
    {
        await MemoryServerless.ImportWebPageAsync("https://en.wikipedia.org/wiki/The_Beatles");

        await PrintMemoryKernel("what are the top albums?");
        await PrintMemoryKernel("what members are still alive? when did the others die if so? ");
        await PrintMemoryKernel("whats an interesting fact about them? make it about paul");
        await PrintMemoryKernel("when did the start, and when did the finish and why?");
        await PrintMemoryKernel("who is michael jordan?"); // I DONT KNOW

        await PrintMemoryKernel("who introduced them to marijuana?");
    }

    private async Task PrintMemoryKernel(string query)
    {
        var answer = await MemoryServerless.AskAsync(query);
        Console.WriteLine($"Question: {query}");
        Console.WriteLine($"Answer: {answer.Result}");
        Console.WriteLine();
    }

    public async Task ExecuteNL2SQL(string query)
    {
    }

    //    async function getSQLFromNLP(userPrompt: string): Promise<QueryData> {
    //    // Get the high-level database schema summary to be used in the prompt.
    //    // The db.schema file could be generated by a background process or the
    //    // schema could be dynamically retrieved.
    //    const dbSchema = await fs.promises.readFile('db.schema', 'utf8');

    //    const systemPrompt = `
    //      Assistant is a natural language to SQL bot that returns only a JSON object with the SQL query and
    //      the parameter values in it.The SQL will query a PostgreSQL database.
    //    PostgreSQL tables, with their columns:

    //      ${dbSchema
    //}

    //Rules:
    //      - Convert any strings to a PostgreSQL parameterized query value to avoid SQL injection attacks.
    //      - Always return a JSON object with the SQL query and the parameter values in it.
    //      - Return a valid JSON object. Do NOT include any text outside of the JSON object.
    //      - Example JSON object to return: { "sql": "", "paramValues": [] }

    //User: "Display all company reviews. Group by company."
    //      Assistant: { "sql": "SELECT * FROM reviews", "paramValues": [] }

    //      User: "Display all reviews for companies located in cities that start with 'L'."
    //      Assistant: { "sql": "SELECT r.* FROM reviews r INNER JOIN customers c ON r.customer_id = c.id WHERE c.city LIKE 'L%'", "paramValues": [] }

    //User: "Display revenue for companies located in London. Include the company name and city."
    //      Assistant:
    //{
    //    "sql": "SELECT c.company, c.city, SUM(o.total) AS revenue FROM customers c INNER JOIN orders o ON c.id = o.customer_id WHERE c.city = $1 GROUP BY c.company, c.city",
    //        "paramValues": ["London"]
    //      }

    //User: "Get the total revenue for Adventure Works Cycles. Include the contact information as well."
    //      Assistant:
    //{
    //    "sql": "SELECT c.company, c.city, c.email, SUM(o.total) AS revenue FROM customers c INNER JOIN orders o ON c.id = o.customer_id WHERE c.company = $1 GROUP BY c.company, c.city, c.email",
    //        "paramValues": ["Adventure Works Cycles"]
    //      }

    //-Convert any strings to a PostgreSQL parameterized query value to avoid SQL injection attacks.
    //      - Do NOT include any text outside of the JSON object. Do not provide any additional explanations or context. Just the JSON object is needed.
    //    `;

    //let queryData: QueryData = { sql: '', paramValues: [], error: '' };
    //let results = '';

    //try
    //{
    //    results = await callOpenAI(systemPrompt, userPrompt);
    //    if (results)
    //    {
    //        console.log('results', results);
    //        const parsedResults = JSON.parse(results);
    //        queryData = { ...queryData, ...parsedResults };
    //        if (isProhibitedQuery(queryData.sql))
    //        {
    //            queryData.sql = '';
    //            queryData.error = 'Prohibited query.';
    //        }
    //    }
    //}
    //catch (error)
    //{
    //    console.log(error);
    //    if (isProhibitedQuery(results))
    //    {
    //        queryData.sql = '';
    //        queryData.error = 'Prohibited query.';
    //    }
    //    else
    //    {
    //        queryData.error = results;
    //    }
    //}

    //return queryData;
    //}

    //function isProhibitedQuery(query: string): boolean {
    //    if (!query) return false;

    //const prohibitedKeywords = [
    //    'insert', 'update', 'delete', 'drop', 'truncate', 'alter', 'create', 'replace',
    //        'information_schema', 'pg_catalog', 'pg_tables', 'pg_namespace', 'pg_class',
    //        'table_schema', 'table_name', 'column_name', 'column_default', 'is_nullable',
    //        'data_type', 'udt_name', 'character_maximum_length', 'numeric_precision',
    //        'numeric_scale', 'datetime_precision', 'interval_type', 'collation_name',
    //        'grant', 'revoke', 'rollback', 'commit', 'savepoint', 'vacuum', 'analyze'
    //];
    //const queryLower = query.toLowerCase();
    //return prohibitedKeywords.some(keyword => queryLower.includes(keyword));
    //}
}