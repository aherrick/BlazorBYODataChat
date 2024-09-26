using System.Net.Mime;
using System.Text;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Chat;
using Server.Helpers;
using Server.Models;
using Server.Models.Dto;
using Server.Services;
using Shared;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using Xceed.Words.NET;

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

                //var text = new StringBuilder();

                //switch (fileDto.File.ContentType)
                //{
                //    case MediaTypeNames.Text.Plain:
                //        {
                //            text.Append(Encoding.UTF8.GetString(memoryStream.ToArray()));

                //            break;
                //        }
                //    case MediaTypeNames.Application.Pdf:
                //        {
                //            using PdfDocument document = PdfDocument.Open(memoryStream.ToArray());

                //            foreach (UglyToad.PdfPig.Content.Page page in document.GetPages())
                //            {
                //                text.Append(ContentOrderTextExtractor.GetText(page));
                //            }

                //            break;
                //        }
                //    case "application/vnd.openxmlformats-officedocument.wordprocessingml.document":
                //        {
                //            using DocX doc = DocX.Load(memoryStream);
                //            text.Append(doc.Text);

                //            break;
                //        }

                //    default:
                //        throw new UnsupportedMediaTypeException();
                //}

                //await azureAIMemoryService.Instanace.ImportTextAsync(
                //    text: text.ToString().Trim(),
                //    documentId: Guid.NewGuid().ToString(),
                //    index: azureAISearchConfig.IndexName
                //);

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
                var context = chatUpdate.GetAzureMessageContext();
#pragma warning restore AOAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

                if (context?.Citations != null)
                {
                    citations.AddRange(
                        from s in context.Citations
                        select new ChatMsgSource { Title = s.Title, Url = s.Url }
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