using Azure;
using Azure.Search.Documents.Indexes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel.ChatCompletion;
using Server.Helpers;
using Server.Models;
using Server.Models.Dto;
using Server.Models.Sk;
using Server.Services;
using Shared;
using System.Collections.ObjectModel;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
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
            var resp = await indexClient.DeleteIndexAsync(azureAISearch.IndexName);

            return resp.IsError;
        });

        #endregion Purge Index

        #region Ingest Data

        chatGrp.MapPost("/ingestdata", async ([FromForm] FileDataDto fileDto,
            AzureAIChatCompletionService azureAIChatCompletionService,
            AzureAIMemoryService azureAIMemoryService,
            AzureAISearchService azureAISearchService) =>
        {
            await using var memoryStream = new MemoryStream();
            await fileDto.File.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            var text = new StringBuilder();

            switch (fileDto.File.ContentType)
            {
                case MediaTypeNames.Text.Plain:
                    text.Append(Encoding.UTF8.GetString(fileBytes));
                    break;

                case MediaTypeNames.Application.Pdf:
                    {
                        using PdfDocument document = PdfDocument.Open(fileBytes);

                        foreach (UglyToad.PdfPig.Content.Page page in document.GetPages())
                        {
                            text.Append(ContentOrderTextExtractor.GetText(page));
                        }

                        break;
                    }

                default:
                    throw new UnsupportedMediaTypeException();
            }

            var body = text.ToString().Trim();

            var azureAISearchDto = new AzureAISearchDto(Title: Path.GetFileNameWithoutExtension(fileDto.File.FileName),
                                             Body: body,
                                             Id: Guid.NewGuid().ToString());

            async IAsyncEnumerable<FileChunkProgress> StreamFileChunkProgress()
            {
                if (!string.IsNullOrEmpty(azureAISearchDto.Body))
                {
                    await foreach (var fileChunkProgress in azureAISearchService.Save(azureAISearchDto))
                    {
                        yield return fileChunkProgress;
                    }
                }
                else
                {
                    // for now if no body, just write 100 // TODO: figure out how to handle empty text
                    yield return new FileChunkProgress() { PercentProcessed = 100 };
                }
            }

            return StreamFileChunkProgress();
        });

        #endregion Ingest Data

        #region Clear

        chatGrp.MapPost("/clear", (ChatHistory chat) =>
        {
            // remove all except first which is the system prompt
            if (chat.Count > 1)
            {
                chat.RemoveRange(1, chat.Count - 1);
            }

            return true;
        });

        #endregion Clear

        #region Cache

        chatGrp.MapGet("/cache", (ChatHistory chat) =>
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

        #endregion Cache

        #region Stream

        chatGrp.MapPost("/stream", ChatStream);

        static async IAsyncEnumerable<ChatMsgDto> ChatStream(
                                                AzureAIChatCompletionService azureAIChatCompletionService,
                                                AzureAIMemoryService azureAIMemoryService,
                                                ChatHistory chat,
                                                Configuration.AzureAISearch azureAISearchConfig,
                                                AzureAIChatCompletionWithDataService azureAIChatCompletionWithDataService,
                                                [FromBody] ChatDto chatDto)
        {
            var chatResponseBuilder = new StringBuilder();
            var titleSourceList = new List<ChatMsgSource>();

            chat.AddUserMessage(chatDto.Query);
            var citationRetrieved = false;

            await foreach (var response in azureAIChatCompletionWithDataService.Instanace.GetStreamingChatMessageContentsAsync(chat))
            {
                if (!citationRetrieved)
                {
                    citationRetrieved = true;

                    var skInnerContentJson = JsonSerializer.Serialize(response.InnerContent);
                    var skInnerConent = JsonSerializer.Deserialize<SkResponseInnerContent.Rootobject>(skInnerContentJson);
                    var citationsContent = JsonSerializer.Deserialize<SkCitation.Rootobject>(skInnerConent.messages.First().delta.content);

                    for (int i = 0; i < citationsContent.citations.Length; i++)
                    {
                        var citation = citationsContent.citations[i];

                        titleSourceList.Add(new ChatMsgSource()
                        {
                            Title = $"doc[{i + 1}]",
                            Url = Guid.NewGuid().ToString() // for now
                        });
                    }
                }

                chatResponseBuilder.Append(response.Content);

                yield return new ChatMsgDto()
                {
                    Message = response.Content,
                    Sources = titleSourceList,
                    Author = ChatMsgAuthor.assistant
                };
            }

            chat.AddMessage(AuthorRole.Assistant, chatResponseBuilder.ToString(),
         metadata: new ReadOnlyDictionary<string, object>(titleSourceList.ToDictionary(k => k.Url, v => (object)v.Title)));

            ////////////////////////
            ///

            /*
            var additionalDataBuilder = new StringBuilder();

            await foreach (var result in azureAIMemoryService.Instanace.SearchAsync(azureAISearchConfig.IndexName, chatDto.Query, limit: 5, minRelevanceScore: 0.5))
            {
                var yo = result.Metadata.Id;

                // append additional info
                additionalDataBuilder.AppendLine(result.Metadata.Text);

                titleSourceList.Add(new ChatMsgSource()

                {
                    Title = result.Metadata.Description,
                    Url = result.Metadata.Id
                });
            }

            */

            // start Plugin
            /*

            var actionContext = new KernelArguments()
            {
                ["$context"] = string.Join("\n", additionalDataBuilder.ToString()),
                ["$query"] = chatDto.Query
            };

            //if we have a Q / A only start adding history
            if (chat.Count > 1)
            {
                actionContext["$chat_history"] = string.Join("\n", chat.Skip(1).Select(x => x.Role + ": " + x.Content));
            }

            var pluginChat = azureAIChatCompletionService.Instanace.Plugins.GetFunction("chat", "answer");

            await foreach (var response in pluginChat.InvokeStreamingAsync(azureAIChatCompletionService.Instanace, actionContext))
            {
                var responseStr = response.ToString();
                chatResponseBuilder.Append(responseStr);

                yield return new ChatMsgDto()
                {
                    Message = responseStr,
                    Sources = titleSourceList,
                    Author = ChatMsgAuthor.assistant
                };
            }
            */

            // end Plugin

            // start IChatCompletionService

            /*

             const string ADD_INFO_MSG = "Here's some additional information: ";
             additionalDataBuilder.Insert(0, ADD_INFO_MSG);

             chat.AddUserMessage(additionalDataBuilder.ToString());
             chat.AddUserMessage(chatDto.Query);

             var chatCompService = azureAIChatCompletionService.Instanace.GetRequiredService<IChatCompletionService>();

             await foreach (var response in chatCompService.GetStreamingChatMessageContentsAsync(chat))
             {
                 chatResponseBuilder.Append(response.Content);

                 yield return new ChatMsgDto()
                 {
                     Message = response.Content,
                     Sources = titleSourceList,
                     Author = ChatMsgAuthor.assistant
                 };
             }

             // remove additional info block from chat history
             chat.Remove(chat.First(x => x.Content.StartsWith(ADD_INFO_MSG)));

             // end IChatCompletionService

             chat.AddMessage(AuthorRole.Assistant, chatResponseBuilder.ToString(),
                 metadata: new ReadOnlyDictionary<string, object>(titleSourceList.ToDictionary(k => k.Url, v => (object)v.Title)));

             */
        }

        #endregion Stream
    }
}

#endregion Chat