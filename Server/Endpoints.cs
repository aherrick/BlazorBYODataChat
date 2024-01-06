﻿using Azure;
using Azure.Search.Documents.Indexes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel.ChatCompletion;
using Server.Models;
using Server.Models.Dto;
using Server.Services;
using Shared;
using System.Collections.ObjectModel;
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

        #region Purge Index

        chatGrp.MapPost("/purgeindex", async (Configuration.AzureAISearch azureAISearch) =>
        {
            var indexClient = new SearchIndexClient(new Uri(azureAISearch.Endpoint), new AzureKeyCredential(azureAISearch.Key));
            await indexClient.DeleteIndexAsync(azureAISearch.IndexName);
        });

        #endregion Purge Index

        #region Ingest Data

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

            var azureAISearchDto = new AzureAISearchDto(Title: Path.GetFileNameWithoutExtension(fileDto.File.FileName),
                                                        Body: text.ToString(),
                                                        Id: Guid.NewGuid().ToString());

            async IAsyncEnumerable<FileChunkProgress> StreamFileChunkProgress()
            {
                await foreach (var file in azureAISearchService.Save(azureAISearchDto))
                {
                    yield return file;
                }
            }
            return StreamFileChunkProgress();
        });

        #endregion Ingest Data

        #region Clear

        chatGrp.MapPost("/clear", ([FromServices] ChatHistory chat) =>
        {
            // remove all except first which is the system prompt
            if (chat.Count > 1)
            {
                chat.RemoveRange(1, chat.Count - 1);
            }

            return true;
        });

        #endregion Clear

        #region Get Cache

        chatGrp.MapGet("/getcache", ([FromServices] ChatHistory chat) =>
        {
            var chatMsgs = new List<ChatMsgDto>();

            // the first one is the AI's instructions so let's just skip it
            foreach (var chatHistory in chat.Skip(1))
            {
                var author = (ChatMsgAuthor)Enum.Parse(typeof(ChatMsgAuthor), chatHistory.Role.ToString());

                var msg = new ChatMsgDto()
                {
                    Message = chatHistory.Content,
                    Author = author
                };

                if (chatHistory.Metadata != null)
                {
                    msg.Sources.AddRange(from m in chatHistory.Metadata
                                         select new ChatMsgSource
                                         {
                                             Title = m.Value.ToString(),
                                             Url = m.Key.ToString()
                                         });
                }

                chatMsgs.Add(msg);
            }

            return chatMsgs;
        });

        #endregion Get Cache

        #region Stream

        chatGrp.MapPost("/stream", ChatStream);

        static async IAsyncEnumerable<ChatMsgDto> ChatStream(
                                                [FromServices] AzureAIChatCompletionService azureAIChatCompletionService,
                                                [FromServices] AzureAIMemoryService azureAIMemoryService,
                                                [FromServices] ChatHistory chat,
                                                [FromServices] Configuration.AzureAISearch azureAISearchConfig,
                                                [FromBody] ChatDto chatDto)
        {
            const string ADD_INFO_MSG = "Here's some additional information: ";

            var builder = new StringBuilder();
            var titleSourceList = new List<ChatMsgSource>();

            await foreach (var result in azureAIMemoryService.Instanace.SearchAsync(azureAISearchConfig.IndexName, chatDto.Query, limit: 5, minRelevanceScore: 0.5))
            {
                // append additional info
                builder.AppendLine(result.Metadata.Text);

                titleSourceList.Add(new ChatMsgSource()

                {
                    Title = result.Metadata.Description,
                    Url = result.Metadata.Id
                });
            }

            builder.Insert(0, ADD_INFO_MSG);

            chat.AddUserMessage(builder.ToString());
            chat.AddUserMessage(chatDto.Query);

            var resposneSb = new StringBuilder();

            await foreach (var response in azureAIChatCompletionService.Instanace.GetRequiredService<IChatCompletionService>()
                                                                                                       .GetStreamingChatMessageContentsAsync(chat))
            {
                resposneSb.Append(response.Content);

                yield return new ChatMsgDto()
                {
                    Message = response.Content,
                    Sources = titleSourceList,
                    Author = ChatMsgAuthor.assistant
                };
            }

            chat.AddMessage(AuthorRole.Assistant, resposneSb.ToString(), metadata: new ReadOnlyDictionary<string, object>(titleSourceList.ToDictionary(k => k.Url, v => (object)v.Title)));

            // remove additional info block from chat history
            chat.Remove(chat.First(x => x.Content.StartsWith(ADD_INFO_MSG)));
        }

        #endregion Stream
    }
}

#endregion Chat