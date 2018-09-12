using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using NonEmergencyBot.LUIS;

namespace NonEmergencyBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        protected BotState State { get; set; } = BotState.None;

        protected bool NeedsEmergencyHelp { get; set; }
        protected string Name { get; set; }
        protected DateTime DateOfBirth { get; set; }


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
                case BotState.None:
                    await HandleEmergencyCheck(context, activity);
                    break;
                case BotState.Name:
                    await HandleNameEntry(context, activity);
                    break;
                case BotState.DateOfBirth:
                    await HandleDateOfBirthEntry(context, activity);
                    break;
                case BotState.AskIssue:
                    await HandleIssueTypeEntry(context, activity);
                    break;
                case BotState.Issue_Theft:
                    break;
                case BotState.Issue_Assault:
                    break;
                case BotState.Issue_Witness:
                    break;
                case BotState.ContactDetails:
                    break;
                default:
                    break;
            }
        }

        public async Task HandleEmergencyCheck(IDialogContext context, IMessageActivity result)
        {
            PromptDialog.Confirm(
                context,
                ConfirmEmergencyCheck,
                "Do you require immediate emergency assistance?",
                "Sorry, I didn't quite understand you, can you try again?",
                promptStyle: PromptStyle.Auto);
        }
        public async Task ConfirmEmergencyCheck(IDialogContext context, IAwaitable<bool> arg)
        {
            var confirm = await arg;
            NeedsEmergencyHelp = confirm;

            await context.PostAsync(confirm
                ? $"Thank you, please hold while we connect you to the emergency line."
                : $"Thank you for confirming that for me.");

            if (!NeedsEmergencyHelp)
            {
                State = BotState.Name;
                await context.PostAsync($"Can you please enter your full name for me?");
            }
            else
            {
                // DIAL 999!
            }

            context.Wait(MessageReceivedAsync);
        }

        public async Task HandleNameEntry(IDialogContext context, IMessageActivity result)
        {
            Name = result.Text;
            PromptDialog.Confirm(
                context,
                ConfirmNameEntry,
                $"Your name is {Name}?",
                "Sorry, I didn't quite understand you, can you try again?",
                promptStyle: PromptStyle.Auto);
        }
        public async Task ConfirmNameEntry(IDialogContext context, IAwaitable<bool> arg)
        {
            var confirm = await arg;

            if (confirm)
            {
                await context.PostAsync($"Hi, {Name}!");
                State = BotState.DateOfBirth;
                await context.PostAsync($"Can you tell me your date of birth please? (dd/MM/yyyy)");
            }
            else
            {
                Name = null;
                await context.PostAsync($"Can you enter your name again please?");
            }

            context.Wait(MessageReceivedAsync);
        }

        public async Task HandleDateOfBirthEntry(IDialogContext context, IMessageActivity result)
        {
            try
            {
                var dobString = DateTime.ParseExact(result.Text, "dd/MM/yyyy", CultureInfo.GetCultureInfo("en-GB").DateTimeFormat);
                DateOfBirth = dobString;
                PromptDialog.Confirm(
                    context,
                    ConfirmDateOfBirthEntry,
                    $"Your date of birth is {dobString.ToString("dd/MM/yyyy")}?",
                    "Sorry, I didn't quite understand you, can you try again?",
                    promptStyle: PromptStyle.Auto);

            }
            catch
            {
                await context.PostAsync($"Sorry, I didn't understand that date. " +
                    $"Could you try again in the format dd/MM/yyyy?");
            }

        }
        public async Task ConfirmDateOfBirthEntry(IDialogContext context, IAwaitable<bool> arg)
        {
            var confirm = await arg;

            if (confirm)
            {
                State = BotState.AskIssue;
                await context.PostAsync($"Can you please describe, in as few words as you can, " +
                    $"the issue you are having today?");
            }
            else
            {
                DateOfBirth = DateTime.MinValue;
                await context.PostAsync($"Can you enter your date of birth again please?");
            }

            context.Wait(MessageReceivedAsync);
        }

        public async Task HandleIssueTypeEntry(IDialogContext context, IMessageActivity result)
        {
            // Do some intent calculation here.
            var luis = new LuisContext(result.Text);
            var intentJson = luis.CurrentResponse;

            PromptDialog.Confirm(
                context,
                ConfirmIssueTypeEntry,
                $"I would categorise that as {intentJson.TopScoringIntent.Intent.ToLower()} with {(intentJson.TopScoringIntent.Score * 100).ToString("#.00")}% confidence. Is that correct?",
                "Sorry, I didn't quite understand you, can you try again?",
                promptStyle: PromptStyle.Auto);

        }
        public async Task ConfirmIssueTypeEntry(IDialogContext context, IAwaitable<bool> arg)
        {
            var confirm = await arg;

            if (confirm)
            {
            }
            else
            {
            }

            context.Wait(MessageReceivedAsync);
        }

    }
}