using System;

namespace ConectaCartagena.Models.Options
{
    public class OpenAIOptions
    {
        public string API_KEY { get; set; } = String.Empty;
        public string ApiBaseUrl { get; set; } = String.Empty;
        public string Model { get; set; } = String.Empty;
        public string AssitantId { get; set; } = String.Empty;
    }
}
