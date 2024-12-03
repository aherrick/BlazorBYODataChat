using Azure;
using Azure.AI.OpenAI.Chat;
using Azure.Search.Documents.Indexes;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Chat;
using Server.Models;
using Server.Models.Dto;
using Server.Services;
using Shared;
using Azure.Search.Documents.Models;
using System.Text.Json;

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
             Configuration.AzureAISearch azureAISearchConfig
           ) =>
           {
               List<DocumentDto> docummentDtos = [];

               try
               {
                   var searchIndexClient = new SearchIndexClient(new Uri(azureAISearchConfig.Endpoint), new AzureKeyCredential(azureAISearchConfig.Key));
                   var searchClient = searchIndexClient.GetSearchClient(azureAISearchConfig.IndexName);

                   var options = new Azure.Search.Documents.SearchOptions
                   {
                       Size = 100,
                       IncludeTotalCount = true,
                       Skip = 0
                   };

                   SearchResults<SearchDocument> searchResults = null;

                   do
                   {
                       searchResults = await searchClient.SearchAsync<SearchDocument>(searchText: string.Empty, options: options);

                       await foreach (var doc in searchResults.GetResultsAsync())
                       {
                           // this is a wild way to have to retrieve docId & filename // any suggestions on a cleaner appraoch are welcome

                           var tags = doc.Document.ElementAt(1).Value as IEnumerable<object>;

                           var docPayloadStr = doc.Document.ElementAt(2).Value.ToString();
                           var docIdStr = tags.ElementAt(0).ToString();

                           // Extract the GUID part using Substring
                           var docId = docIdStr[(docIdStr.IndexOf(':') + 1)..];
                           var docPayload = JsonSerializer.Deserialize<DocumentPayloadDto>(docPayloadStr);

                           docummentDtos.Add(new DocumentDto()
                           {
                               DocumentId = docId,
                               Name = docPayload.file
                           });
                       }

                       options.Skip += 100;
                   } while (options.Skip < searchResults.TotalCount);
               }
               catch (RequestFailedException ex) when (ex.Status == 404)
               {
               }

               return docummentDtos.DistinctBy(x => x.Name).ToList();
           }
       );

        chatGrp.MapPost(
           "/deletedocument",
           async (
             AzureAIMemoryService azureAIMemoryService,
             Configuration.AzureAISearch azureAISearchConfig,
             DocumentDto documentDto
           ) =>
           {
               await azureAIMemoryService.Instanace.DeleteDocumentAsync(documentDto.DocumentId, azureAISearchConfig.IndexName);

               return true;
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
                    documentId: fileDto.Id,
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