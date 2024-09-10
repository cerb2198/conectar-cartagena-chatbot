using EchoBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace EchoBot.Bots
{
    public class PevaarChatBot : ActivityHandler
    {
        private readonly OpenAIService _openAiService;
        private static readonly Dictionary<string, string> WelcomeMessages = new Dictionary<string, string>
{
    { "es", "¡Bienvenido a tu agente virtual de turismo a Cartagena! ¿En qué te puedo ayudar?" },
    { "en", "Welcome to your virtual tourism agent for Cartagena! How can I assist you?" },
    { "fr", "Bienvenue chez votre agent virtuel de tourisme pour Carthagène ! Comment puis-je vous aider ?" },
    { "it", "Benvenuto al tuo agente virtuale di turismo per Cartagena! Come posso aiutarti?" }
};

        private string selectedLanguage = "es";

        public PevaarChatBot(OpenAIService openAiService)
        {
            _openAiService = openAiService;
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await SendLanguageChoiceCardAsync(turnContext, cancellationToken);
                }
            }
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var userMessage = turnContext.Activity.Text;

            if (userMessage.ToLower() == "es" || userMessage.ToLower() == "en" || userMessage.ToLower() == "fr" || userMessage.ToLower() == "it")
            {
                selectedLanguage = userMessage.ToLower();
                await turnContext.SendActivityAsync(MessageFactory.Text(WelcomeMessages[selectedLanguage]), cancellationToken);
            }
            else
            {
                var assistantResponse = await _openAiService.GetTourismResponseAsync(userMessage);

                await turnContext.SendActivityAsync(assistantResponse, null, null, cancellationToken);
            }
        }

        private async Task SendLanguageChoiceCardAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            // Crear tarjetas de idioma
            var card = new HeroCard
            {
                Title = "Selecciona tu idioma / Select your language / Sélectionnez votre langue / Seleziona la tua lingua",
                Buttons = new List<CardAction>
                {
                    new CardAction(ActionTypes.ImBack, "Español", value: "es"),
                    new CardAction(ActionTypes.ImBack, "English", value: "en"),
                    new CardAction(ActionTypes.ImBack, "Français", value: "fr"),
                    new CardAction(ActionTypes.ImBack, "Italiano", value: "it")
                }
            };

            var reply = MessageFactory.Attachment(card.ToAttachment());
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }
    }
}
