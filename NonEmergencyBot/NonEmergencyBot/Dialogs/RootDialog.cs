using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace NonEmergencyBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        protected BotState State { get; set; } = BotState.None;

        protected bool NeedsEmergencyHelp { get; set; }
        protected string Name { get; set; }


        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            switch (State)
            {
                case BotState.ImmediateHelp:
                    break;
                case BotState.Name:
                    break;
                case BotState.None:
                    State = BotState.ImmediateHelp;
                    IsItAnEmergency(context, activity);
                    break;
                default:
                    await context.PostAsync(activity.Text);
                    break;
            }

            context.Wait(MessageReceivedAsync);
        }

        public virtual async Task IsItAnEmergency(IDialogContext context, IMessageActivity result)
        {
            PromptDialog.Confirm(
                context,
                HandleEmergencyCheck,
                "Do you require immediate emergency assistance?",
                "Sorry, I didn't quite understand you, can you try again?",
                promptStyle: PromptStyle.Auto);
        }

        public async Task HandleEmergencyCheck(IDialogContext context, IAwaitable<bool> arg)
        {
            var confirm = await arg;
            NeedsEmergencyHelp = confirm;

            await context.PostAsync(confirm
                ? $"Thank you, please hold while we connect you to the emergency line."
                : $"Thank you for confirming that for me.");

        }
    }
}