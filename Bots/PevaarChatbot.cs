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

        public PevaarChatBot(OpenAIService openAiService)
        {
            _openAiService = openAiService;
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "¡Bienvenido a Pevaar Software Factory! ¿En qué podemos ayudarte hoy?";
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText), cancellationToken);
                }
            }
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var userMessage = turnContext.Activity.Text;

            var prompt = $"\"{userMessage}\"\n" +
                         "Teniendo en cuenta que el texto anterior fue escrito por una persona para un chatbot ¿cual es la intencion del mensaje?\n" +
                         "1. Enviar un correo a un asesor de ventas de Pevaar\n" +
                         "2. Apartar una cita/llamada con un agente de ventas\n" +
                         "3. Otra acción.\n" +
                         "Responde en el siguiente formato: \"Respuesta {NumeroOpcionRespuestaCorrecta}\"";

            var intentResult = await _openAiService.GetIntentAsync(prompt);

            switch (intentResult)
            {
                case "Respuesta 1":
                    await SendSalesEmailFlow(turnContext, cancellationToken);
                    break;
                case "Respuesta 2":
                    await ScheduleCallFlow(turnContext, cancellationToken);
                    break;
                default:
                    await ProcessGenerativeFlow(userMessage, turnContext, cancellationToken);
                    break;
            }
        }

        private async Task SendSalesEmailFlow(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync("Por favor proporciona tu nombre, el correo electrónico y la empresa que representas para enviar el correo al asesor de ventas.", null, null, cancellationToken);
            // Aquí puedes agregar un flujo para capturar y procesar la información.
        }

        private async Task ScheduleCallFlow(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync("Para apartar una cita/llamada, por favor proporciona la fecha y hora preferida, así como tu nombre y correo electrónico.", null, null, cancellationToken);
            // Aquí puedes agregar un flujo para programar la llamada.
        }

        private async Task ProcessGenerativeFlow(string userMessage, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var generativeResponse = await _openAiService.GetGenerativeResponseAsync(userMessage);
            await turnContext.SendActivityAsync(generativeResponse, null, null, cancellationToken);
        }
    }
}
