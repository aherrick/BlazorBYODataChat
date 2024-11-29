using Azure.AI.OpenAI.Chat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Azure;
using Microsoft.KernelMemory;
using OpenAI.Chat;
using Server.Models;
using Server.Models.Dto;
using Server.Services;
using Shared;
using System.Text.RegularExpressions;

namespace Server;

public static class Endpoints
{
    #region Chat

    public static void RegisterChatEndpoints(this IEndpointRouteBuilder routes)
    {
        var chatGrp = routes.MapGroup("/chat").DisableAntiforgery();

        #region Purge Index

        chatGrp.MapPost(
            "/purgeindex",
            async (
                AzureAIMemoryService azureAIMemoryService,
                Configuration.AzureAISearch azureAISearchConfig
            ) =>
            {
                await azureAIMemoryService.Instanace.DeleteIndexAsync(
                    azureAISearchConfig.IndexName
                );

                return true;
            }
        );

        #endregion Purge Index

        chatGrp.MapGet(
           "/getdocuments",
           async (
             AzureAIMemoryService azureAIMemoryService,
             Configuration.AzureAISearch azureAISearchConfig
           ) =>
           {
               var docs = await azureAIMemoryService.Instanace.SearchAsync("*", azureAISearchConfig.IndexName, limit: 10000);

               docs.Results.Reverse();

               return docs.Results.Select(x => x.SourceName).ToList();
           }
       );

        #region Ingest Data

        chatGrp.MapPost(
            "/ingestdata",
            async (
                [FromForm] FileDataDto fileDto,
                AzureAIMemoryService azureAIMemoryService,
                Configuration.AzureAISearch azureAISearchConfig
            ) =>
            {
                await using var memoryStream = new MemoryStream();
                await fileDto.File.CopyToAsync(memoryStream);

                //var docId = string.Concat(Regex.Matches(fileDto.File.FileName, @"[A-Za-z0-9]"));

                await azureAIMemoryService.Instanace.ImportDocumentAsync(
                    memoryStream,
                    documentId: Guid.NewGuid().ToString(),
                    fileName: fileDto.File.FileName,
                    index: azureAISearchConfig.IndexName
                );

                static async IAsyncEnumerable<FileChunkProgress> StreamFileChunkProgress()
                {
                    await Task.Yield(); // Make us async right away

                    yield return new FileChunkProgress() { PercentProcessed = 100 };
                }

                return StreamFileChunkProgress();
            }
        );

        #endregion Ingest Data

        #region Stream

        chatGrp.MapPost("/StreamMemorySearch", ChatStreamMemorySearch);

        static async IAsyncEnumerable<ChatMsgDto> ChatStreamMemorySearch(
            AzureAIChatCompletionService azureAIChatCompletionService,
            AzureAIChatDataSourceService azureAIChatDataSourceService,
            [FromBody] ChatDto chatDto
        )
        {
            var chatUpdates = azureAIChatCompletionService.Instanace.CompleteChatStreamingAsync(
                [new UserChatMessage(chatDto.Query)],
                azureAIChatDataSourceService.Instanace
            );

            await foreach (var chatUpdate in chatUpdates)
            {
                foreach (ChatMessageContentPart contentPart in chatUpdate.ContentUpdate ?? [])
                {
                    yield return new ChatMsgDto()
                    {
                        Message = contentPart.Text,
                        Author = ChatMsgAuthor.assistant
                    };
                }

#pragma warning disable AOAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                ChatMessageContext onYourDataContext = chatUpdate.GetMessageContext();
#pragma warning restore AOAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

                if (onYourDataContext?.Intent is not null)
                {
                    Console.WriteLine($"Intent: {onYourDataContext.Intent}");
                }
#pragma warning disable AOAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                foreach (ChatCitation citation in onYourDataContext?.Citations ?? [])
                {
                    Console.WriteLine($"Citation: {citation.Content}");
                }
#pragma warning restore AOAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            }
        }
    }

    #endregion Stream
}

#endregion Chat