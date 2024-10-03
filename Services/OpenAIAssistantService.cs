using OpenAI;
using System;
using OpenAI.Assistants;
using OpenAI.Files;
using Microsoft.Extensions.Options;
using ConectaCartagena.Models.Options;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Collections.Generic;

#pragma warning disable OPENAI001
namespace ConectaCartagena.Services
{
    public class OpenAIAssistantService
    {
        private OpenAIClient _openAIClient;
        private FileClient _fileClient;
        private AssistantClient _assistantClient;
        private OpenAIOptions _openAIOptions;
        private LanguageService _languageService;

        public OpenAIAssistantService(IOptions<OpenAIOptions> openAIOptions, LanguageService languageService)
        {
            _openAIOptions = openAIOptions.Value;
            _openAIClient = new(_openAIOptions.API_KEY);
            _fileClient = _openAIClient.GetFileClient();
            _assistantClient = _openAIClient.GetAssistantClient();
            _languageService = languageService;
        }

        public async Task<string> CreateNewThread()
        {
            var result = await _assistantClient.CreateThreadAsync();
            var thread = result.Value;
            return thread.Id;
        }

        public async Task DeleteThread(string threadId)
        {
            await _assistantClient.DeleteThreadAsync(threadId);
        }

        public async Task<string> GetAnswer(string userMessage, string lang, string threadId)
        {
            string complementationMsg = $"\nResponde esta pregunta en idioma: {lang}. Donde es = español, en = ingles, fr = frances, it = italiano";

            var thread = await GetThread(threadId);

            IEnumerable<MessageContent> messageContent = new List<MessageContent>() { MessageContent.FromText(userMessage+complementationMsg) };

            await _assistantClient.CreateMessageAsync(thread, MessageRole.User, messageContent);

            var assistant = await GetAssistant();

            ThreadRun threadRun = await _assistantClient.CreateRunAsync(thread, assistant);

            threadRun = await PollUntilTerminalStatusAsync(threadRun);

            var options = new MessageCollectionOptions() { Order = ListOrder.NewestFirst };

            var messages = _assistantClient.GetMessages(threadRun.ThreadId, new MessageCollectionOptions() { Order = "desc" }).GetAllValues().ToList();

            return messages.FirstOrDefault().Content.FirstOrDefault().Text;
        }

        private async Task<AssistantThread> GetThread(string threadId)
        {
            AssistantThread thread = await _assistantClient.GetThreadAsync(threadId);

            return thread;
        }

        private async Task<Assistant> GetAssistant()
        {
            Assistant assistant = await _assistantClient.GetAssistantAsync(_openAIOptions.AssistantId);
            
            return assistant;
        }

        private async Task<ThreadRun> PollUntilTerminalStatusAsync(ThreadRun threadRun)
        {
            TimeSpan pollingTimeout = TimeSpan.FromSeconds(30);
            DateTime startTime = DateTime.UtcNow;

            do
            {
                if (DateTime.UtcNow - startTime > pollingTimeout)
                {
                    throw new TimeoutException("Time out was reached.");
                }

                await Task.Delay(TimeSpan.FromMilliseconds(100));

                threadRun = await _assistantClient.GetRunAsync(threadRun.ThreadId, threadRun.Id);

            } while (!threadRun.Status.IsTerminal);

            return threadRun;
        }

        private async Task<OpenAIFileInfo> UploadKBSources()
        {
            return await _fileClient.UploadFileAsync(CreateDummyStreamFile(), "file.json", FileUploadPurpose.Assistants);
        }

        private async Task<Assistant> CreateAssistant(String lang)
        {
            var customFile = await UploadKBSources();

            AssistantCreationOptions assistantOptions = new()
            {
                Name = "Conecta Cartagena",
                Instructions = _languageService.GetExpertTouristPromt(lang),
                Tools =
                {
                    new FileSearchToolDefinition()
                },
                ToolResources = new()
                {
                    FileSearch = new()
                    {
                        NewVectorStores =
                            {
                                new VectorStoreCreationHelper([customFile.Id]),
                            }
                    }
                },
            };

            return await _assistantClient.CreateAssistantAsync("gpt-4o", assistantOptions);
        }

        private Stream CreateDummyStreamFile()
        {
            using Stream document = BinaryData.FromString("""
                {
                    "description": "This document contains the sale history data for Contoso products.",
                    "sales": [
                        {
                            "month": "January",
                            "by_product": {
                                "113043": 15,
                                "113045": 12,
                                "113049": 2
                            }
                        },
                        {
                            "month": "February",
                            "by_product": {
                                "113045": 22
                            }
                        },
                        {
                            "month": "March",
                            "by_product": {
                                "113045": 16,
                                "113055": 5
                            }
                        }
                    ]
                }
                """).ToStream();

            return document;
        }
    }
}
