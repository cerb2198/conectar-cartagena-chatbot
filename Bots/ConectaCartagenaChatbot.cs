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
        private readonly OpenAIAssistantService _openAIAssistantService;
        private readonly LanguageService _languageService;
        private readonly IStatePropertyAccessor<UserProfile> _userProfileAccessor;
        private readonly IStatePropertyAccessor<string> _conversationThreadProfileAccessor;
        private readonly UserState _userState; // Almacena el UserState para guardar los cambios

        public ConectaCartagenaChatbot(OpenAIService openAiService, OpenAIAssistantService openAIAssistantService, LanguageService languageService, UserState userState)
        {
            _openAiService = openAiService;
            _openAIAssistantService = openAIAssistantService;
            _languageService = languageService;
            _userState = userState;
            _userProfileAccessor = userState.CreateProperty<UserProfile>("UserProfile");
            _conversationThreadProfileAccessor = _userState.CreateProperty<string>("ConversationThread");
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    string threadId = await _openAIAssistantService.CreateNewThread();
                    await _conversationThreadProfileAccessor.SetAsync(turnContext, threadId, cancellationToken);

                    await _userState.SaveChangesAsync(turnContext, false, cancellationToken);

                    await SendLanguageChoiceCardAsync(turnContext, cancellationToken);
                }
            }
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var userProfile = await _userProfileAccessor.GetAsync(turnContext, () => new UserProfile());

            var userMessage = turnContext.Activity.Text.ToLower();

            if (userMessage == "es" || userMessage == "en" || userMessage == "fr" || userMessage == "it")
            {
                EventFactory.CreateHandoffInitiation(turnContext, new { DummyMessage = "hi"});

                userProfile.Language = userMessage;

                var welcomeMessage = _languageService.GetWelcomeMessage(userProfile.Language);

                await turnContext.SendActivityAsync(MessageFactory.Text(welcomeMessage), cancellationToken);
            }
            else
            {
                string currentThread = await _conversationThreadProfileAccessor.GetAsync(turnContext);

                var assistantResponse = await _openAIAssistantService.GetAnswer(userMessage, userProfile.Language, currentThread);

                await turnContext.SendActivityAsync(assistantResponse, null, null, cancellationToken);
            }

            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMembersRemovedAsync(IList<ChannelAccount> membersRemoved, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersRemoved)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    string threadId= await _conversationThreadProfileAccessor.GetAsync(turnContext, () => null, cancellationToken);
                    if (threadId != null)
                    {
                        await _openAIAssistantService.DeleteThread(threadId);
                    }

                    await _conversationThreadProfileAccessor.DeleteAsync(turnContext, cancellationToken);
                }
            }
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
