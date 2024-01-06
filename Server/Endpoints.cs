using Azure;
using Azure.Search.Documents.Indexes;
using Microsoft.AspNetCore.Mvc;
using Server.Models;
using Server.Models.Dto;
using Server.Services;
using Shared;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Server;

public static class Endpoints
{
    #region Chat

    public static void RegisterChatEndpoints(this IEndpointRouteBuilder routes)
    {
        var chatGrp = routes.MapGroup("/chat").DisableAntiforgery();

        chatGrp.MapPost("/purgeindex", async (Configuration.AzureAISearch azureAISearch) =>
        {
            var indexClient = new SearchIndexClient(new Uri(azureAISearch.Endpoint), new AzureKeyCredential(azureAISearch.Key));
            await indexClient.DeleteIndexAsync(azureAISearch.IndexName);
        });

        chatGrp.MapPost("/ingestdata", async ([FromForm] FileDataDto fileDto,
            [FromServices] AzureAIChatCompletionService azureAIChatCompletionService,
            [FromServices] AzureAIMemoryService azureAIMemoryService,
            [FromServices] AzureAISearchService azureAISearchService) =>
        {
            await using var memoryStream = new MemoryStream();
            await fileDto.File.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            var text = new StringBuilder();

            if (fileDto.File.ContentType.Contains("txt"))
            {
                text.Append(Encoding.UTF8.GetString(fileBytes));
            }
            else if (fileDto.File.ContentType.Contains("pdf"))
            {
                using PdfDocument document = PdfDocument.Open(fileBytes);

                foreach (UglyToad.PdfPig.Content.Page page in document.GetPages())
                {
                    text.Append(ContentOrderTextExtractor.GetText(page));
                }
            }

            var azureAISearchDto = new AzureAISearchDto
            {
                Body = text.ToString(),
                Id = Guid.NewGuid().ToString(),
                Title = Path.GetFileNameWithoutExtension(fileDto.File.FileName)
            };

            async IAsyncEnumerable<FileChunkProgress> StreamFileChunkProgress()
            {
                await foreach (var file in azureAISearchService.Save(azureAISearchDto))
                {
                    yield return file;
                }
            }
            return StreamFileChunkProgress();
        });
    }

    #endregion Chat
}