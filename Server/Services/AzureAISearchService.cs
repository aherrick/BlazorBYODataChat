using Microsoft.SemanticKernel.Text;
using Server.Models;
using Server.Models.Dto;
using Shared;

namespace Server.Services;

public class AzureAISearchService(AzureAIMemoryService azureAIMemoryService, Configuration.AzureAISearch azureAISearchConfig)
{
    private const int MaxTokensPerParagraph = 960;
    private const int MaxTokensPerLine = 360;

    public async IAsyncEnumerable<FileChunkProgress> Save(AzureAISearchDto azureAISearchDto)
    {
        // rebuild new index with fresh data

        if (!string.IsNullOrEmpty(azureAISearchDto.Body))
        {
            // https://github.com/Azure-Samples/miyagi/blob/f8e4bb3e50d60bac4baa5d49bf8b9c978547a5ec/services/recommendation-service/dotnet/Controllers/MemoryController.cs
#pragma warning disable SKEXP0055 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var lines = TextChunker.SplitPlainTextLines(azureAISearchDto.Body, MaxTokensPerLine);
#pragma warning restore SKEXP0055 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0055 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var chunks = TextChunker.SplitPlainTextParagraphs(lines, MaxTokensPerParagraph);
#pragma warning restore SKEXP0055 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            for (var i = 0; i < chunks.Count; i++)
            {
                // TODO: "Reference" didn't seem to same off all fields, need to revist
                // await azureAIMemoryService.Instanace.SaveReferenceAsync

                await azureAIMemoryService.Instanace.SaveInformationAsync(
                    collection: azureAISearchConfig.IndexName,
                    text: chunks[i],
                    id: azureAISearchDto.Id,
                    description: azureAISearchDto.Title);

                yield return new FileChunkProgress()
                {
                    PercentProcessed = (int)Math.Round((i + 1) / (decimal)chunks.Count * 100)
                };

                await Task.Delay(200); // rate limit
            }
        }
    }
}