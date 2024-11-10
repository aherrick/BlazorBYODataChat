using Azure.AI.OpenAI.Chat;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Chat;
using Server.Models;
using Server.Models.Dto;
using Server.Services;
using Shared;

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

                await azureAIMemoryService.Instanace.ImportDocumentAsync(
                    memoryStream,
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
                var text = string.Join("", chatUpdate.ContentUpdate.Select(x => x.Text));
                var citations = new List<ChatMsgSource>();

#pragma warning disable AOAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                var context = chatUpdate.GetMessageContext();
#pragma warning restore AOAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

                if (context?.Citations != null)
                {
                    citations.AddRange(
                        from s in context.Citations
                        select new ChatMsgSource { Title = s?.Title, Url = s.Uri?.ToString() }
                    );
                }

                yield return new ChatMsgDto()
                {
                    Message = text,
                    Sources = citations,
                    Author = ChatMsgAuthor.assistant
                };
            }
        }
    }

    #endregion Stream
}

#endregion Chat