﻿using EchoBot.Models.Options;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EchoBot.Services
{
    public class OpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly OpenAIOptions _openAIOptions;

        public OpenAIService(HttpClient httpClient, IOptions<OpenAIOptions> options)
        {
            _httpClient = httpClient;
            _openAIOptions = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<string> GetIntentAsync(string prompt)
        {
            var requestBody = CreateChatRequestBody(
                model: _openAIOptions.Model ?? "gpt-4",
                systemMessage: "Eres un asistente que ayuda a identificar intenciones.",
                userMessage: prompt,
                maxTokens: 50
            );

            return await SendChatRequestAsync(requestBody);
        }

        public async Task<string> GetGenerativeResponseAsync(string userMessage)
        {
            var requestBody = CreateChatRequestBody(
                model: _openAIOptions.Model ?? "gpt-4",
                systemMessage: "Eres un asistente que responde a las preguntas del usuario.",
                userMessage: userMessage,
                maxTokens: 150
            );

            return await SendChatRequestAsync(requestBody);
        }

        private async Task<string> SendChatRequestAsync(object requestBody)
        {
            var requestContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"{_openAIOptions.ApiBaseUrl}/v1/chat/completions"),
                Headers = { { "Authorization", $"Bearer {_openAIOptions.API_KEY}" } },
                Content = requestContent
            };

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error en la solicitud a completions: {response.StatusCode}. Detalles: {responseBody}");
            }

            var result = JsonSerializer.Deserialize<ChatCompletionResponse>(responseBody);
            return result?.Choices?[0]?.Message?.Content?.Trim() ?? string.Empty;
        }

        private object CreateChatRequestBody(string model, string systemMessage, string userMessage, int maxTokens)
        {
            return new
            {
                model,
                messages = new[]
                {
                    new { role = "system", content = systemMessage },
                    new { role = "user", content = userMessage }
                },
                max_tokens = maxTokens
            };
        }
    }

    public class ChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice> Choices { get; set; }

        public class Choice
        {
            [JsonPropertyName("message")]
            public Message Message { get; set; }
        }

        public class Message
        {
            [JsonPropertyName("content")]
            public string Content { get; set; }
        }
    }
}