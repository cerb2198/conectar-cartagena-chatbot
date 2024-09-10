﻿using System;

namespace EchoBot.Models.Options
{
    public class OpenAIOptions
    {
        public string API_KEY { get; set; } = String.Empty;
        public string ApiBaseUrl { get; set; } = String.Empty;
        public string Model { get; set; } = String.Empty;
    }
}