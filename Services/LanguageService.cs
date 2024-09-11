using ConectaCartagena.Models.Options;
using Newtonsoft.Json;
using System.IO;

namespace ConectaCartagena.Services
{
    public class LanguageService
    {
        private readonly LanguageOptions _languageOptions;

        public LanguageService()
        {
            _languageOptions = LoadLanguages();
        }

        public LanguageOptions GetLanguageOptions()
        {
            return _languageOptions;
        }

        public string GetExpertTouristPromt(string language)
        {
            return language.ToLower() switch
            {
                "es" => _languageOptions.promts.tourist_expert.es,
                "en" => _languageOptions.promts.tourist_expert.en,
                "fr" => _languageOptions.promts.tourist_expert.fr,
                "it" => _languageOptions.promts.tourist_expert.it,
                _ => _languageOptions.promts.tourist_expert.es
            };
        }

        public string GetWelcomeMessage(string language)
        {
            return language.ToLower() switch
            {
                "es" => _languageOptions.messages.welcome.es,
                "en" => _languageOptions.messages.welcome.en,
                "fr" => _languageOptions.messages.welcome.fr,
                "it" => _languageOptions.messages.welcome.it,
                _ => _languageOptions.messages.welcome.es
            };
        }

        private LanguageOptions LoadLanguages()
        {
            var jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "lang.json");
            var json = File.ReadAllText(jsonFilePath);
            return JsonConvert.DeserializeObject<LanguageOptions>(json);
        }
    }
}
