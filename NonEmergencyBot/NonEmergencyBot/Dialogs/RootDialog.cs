using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using NonEmergencyBot.LUIS;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Net;

namespace NonEmergencyBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        protected BotState State { get; set; } = BotState.None;

        protected string TheftObject { get; set; }

        protected bool NeedsEmergencyHelp { get; set; }
        protected string Name { get; set; }
        protected DateTime DateOfBirth { get; set; }

        private static readonly List<VisualFeatureTypes> features =
            new List<VisualFeatureTypes>()
            {
                        VisualFeatureTypes.Categories, VisualFeatureTypes.Description,
                        VisualFeatureTypes.Faces, VisualFeatureTypes.ImageType,
                        VisualFeatureTypes.Tags
            };

        private int nextCrimeReference = 100;

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

            State = IntentStringToIssue(intentJson.TopScoringIntent.Intent);
            TheftObject = intentJson.Entities.FirstOrDefault(x => x.Type == Entities.StolenObject)?.Entity;

            PromptDialog.Confirm(
                context,
                ConfirmIssueTypeEntry,
                $"I would categorise that as {intentJson.TopScoringIntent.Intent.ToString().ToLower()} with {(intentJson.TopScoringIntent.Score * 100).ToString("#.00")}% confidence. Is that correct?",
                "Sorry, I didn't quite understand you, can you try again?",
                promptStyle: PromptStyle.Auto);

        }

        public async Task ConfirmIssueTypeEntry(IDialogContext context, IAwaitable<bool> arg)
        {
            var confirm = await arg;

            if (confirm)
            {
                if (State == BotState.Issue_Theft && !string.IsNullOrEmpty(TheftObject))
                {
                    PromptDialog.Attachment(
                        context,
                        TheftObjectUploaded,
                        $"Please upload a picture of your {TheftObject}");
                }
                else
                {
                    await ForwardToOperator(context);
                }
            }
            else
            {
                await ForwardToOperator(context);
            }
        }

        private async Task ForwardToOperator(IDialogContext context)
        {
            await context.PostAsync($"Thank you for the information. I am forwarding you to another member of staff who can comlpete your enquiry");
        }

        private async Task TheftObjectUploaded(IDialogContext context, IAwaitable<IEnumerable<Attachment>> arg)
        {
            var attachments = await arg;

            var att = attachments.FirstOrDefault();

            if (att != null)
            {
                var req = WebRequest.Create(att.ContentUrl);
                var response = req.GetResponse();
                using (var stream = response.GetResponseStream())
                {
                    var client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(PrivateKeys.VisionApiKey));
                    client.Endpoint = "https://northeurope.api.cognitive.microsoft.com";
                    var imageAnalysis = await client.AnalyzeImageInStreamAsync(stream, features);

                    if (ContainsItemOrPseudonym(imageAnalysis.Tags, TheftObject))
                    {
                        await IssueCrimeReferenceNumber(context);
                    }
                    else
                    {

                    }
                }
            }
        }

        private bool ContainsItemOrPseudonym(IList<ImageTag> tags, string theftObject)
        {
            return true;
        }

        private async Task IssueCrimeReferenceNumber(IDialogContext context)
        {
            await context.PostAsync($"Thank you for the information. Your crime reference number is {nextCrimeReference++}");
        }

        public async Task HandleBicycleImage(IDialogContext context, IMessageActivity result)
        {
            
        }

        private BotState IntentStringToIssue(Intents intent)
        {
            switch(intent)
            {
                case Intents.Theft: return BotState.Issue_Theft;
                case Intents.Assault: return BotState.Issue_Assault;
                default: return BotState.AskIssue;
            }
        }
    }
}