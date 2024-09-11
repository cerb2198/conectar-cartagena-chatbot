using ConectaCartagena.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using ConectaCartagena.Models.State;

namespace ConectaCartagena.Bots
{
    public class ConectaCartagenaChatbot : ActivityHandler
    {
        private readonly OpenAIService _openAiService;
        private readonly LanguageService _languageService;
        private readonly IStatePropertyAccessor<UserProfile> _userProfileAccessor;
        private readonly UserState _userState; // Almacena el UserState para guardar los cambios

        public ConectaCartagenaChatbot(OpenAIService openAiService, LanguageService languageService, UserState userState)
        {
            _openAiService = openAiService;
            _languageService = languageService;
            _userState = userState;
            _userProfileAccessor = userState.CreateProperty<UserProfile>("UserProfile");
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
            // Recupera el perfil del usuario (si no existe, lo crea por defecto)
            var userProfile = await _userProfileAccessor.GetAsync(turnContext, () => new UserProfile());

            var userMessage = turnContext.Activity.Text.ToLower();

            if (userMessage == "es" || userMessage == "en" || userMessage == "fr" || userMessage == "it")
            {
                // Actualiza el idioma en el perfil del usuario
                userProfile.Language = userMessage;

                // Obtén el mensaje de bienvenida en el idioma seleccionado
                var welcomeMessage = _languageService.GetWelcomeMessage(userProfile.Language);

                await turnContext.SendActivityAsync(MessageFactory.Text(welcomeMessage), cancellationToken);
            }
            else
            {
                // Utiliza el idioma almacenado en el perfil del usuario
                var assistantResponse = await _openAiService.GetTourismResponseAsync(userMessage, userProfile.Language);

                await turnContext.SendActivityAsync(assistantResponse, null, null, cancellationToken);
            }

            // Guarda el estado del usuario después de cada interacción
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        private async Task SendLanguageChoiceCardAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
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
