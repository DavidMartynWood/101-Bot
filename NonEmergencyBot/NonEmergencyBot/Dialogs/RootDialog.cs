using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

using NonEmergencyBot.LUIS;

namespace NonEmergencyBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        protected BotState State { get; set; } = BotState.None;

        protected LuisContext LUISIssueResult { get; set; }

        protected List<string> StolenObjectImages { get; set; }
        protected List<string> AssaultInjuryImages { get; set; }

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
                case BotState.AskLocation:
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
                await context.PostAsync($"Can you please briefly describe the issue you have?");
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
            LUISIssueResult = new LuisContext(result.Text);

            State = IntentStringToIssue(LUISIssueResult.CurrentResponse.TopScoringIntent.Intent);

            // Build small issue description from Entities 
            //  e.g. My bike was stolen from in front of my house.
            //      - {{ Bike }} {{ stolen }} {{ my house }}

            PromptDialog.Confirm(
                context,
                ConfirmIssueTypeEntry,
                $"I would categorise that as {LUISIssueResult.CurrentResponse.TopScoringIntent.Intent.ToString().ToLower()} with " +
                $"{(LUISIssueResult.CurrentResponse.TopScoringIntent.Score * 100).ToString("#.00")}% confidence. Is that correct?",
                "Sorry, I didn't quite understand you, can you try again?",
                promptStyle: PromptStyle.Auto);

        }
        public async Task ConfirmIssueTypeEntry(IDialogContext context, IAwaitable<bool> arg)
        {
            var confirm = await arg;

            if (confirm && State != BotState.AskIssue)
            {
                switch (State)
                {
                    case BotState.Issue_Theft:
                        await HandleTheftIssue(context);
                        break;
                    case BotState.Issue_Assault:
                        await HandleAssultIssue(context);
                        break;
                    case BotState.Issue_Harassment:
                        break;
                    case BotState.Issue_CarCrash:
                        break;
                    case BotState.Issue_CriminalDamage:
                        break;
                    case BotState.Issue_Information:
                        break;
                    case BotState.Issue_None:
                        break;
                }
            }
            else
            {
                State = BotState.AskIssue;
                await context.PostAsync($"Sorry, could you try describe it differently?");
            }
        }

        private async Task HandleTheftIssue(IDialogContext context)
        {
            if (!string.IsNullOrEmpty(LUISIssueResult.CurrentResponse.Entities.FirstOrDefault(x => x.Type == Entities.StolenObject)?.Entity))
            {
                PromptDialog.Attachment(
                    context,
                    TheftObjectUploaded,
                    $"Please upload a picture of your " +
                    $"{LUISIssueResult.CurrentResponse.Entities.FirstOrDefault(x => x.Type == Entities.StolenObject)?.Entity}");
            }
            else
            {
                await ForwardToOperator(context);
            }
        }
        private void PromptForStolenObjectUpload(IDialogContext context)
        {
            PromptDialog.Attachment(
                context,
                TheftObjectUploaded,
                $"Please upload a picture of your {LUISIssueResult.CurrentResponse.Entities.Where(x => x.Type == Entities.StolenObject).FirstOrDefault()?.Entity}");
        }
        private async Task TheftObjectUploaded(IDialogContext context, IAwaitable<IEnumerable<Attachment>> arg)
        {
            var stolen = await arg;
            StolenObjectImages = stolen.Select(x => x.ContentUrl).ToList();

            var att = StolenObjectImages.FirstOrDefault();

            if (att != null)
            {
                var req = WebRequest.Create(att);
                var response = req.GetResponse();
                using (var stream = response.GetResponseStream())
                {
                    var client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(PrivateKeys.VisionApiKey));
                    client.Endpoint = "https://northeurope.api.cognitive.microsoft.com";
                    var imageAnalysis = await client.AnalyzeImageInStreamAsync(stream, features);

                    if (ContainsItemOrPseudonym(imageAnalysis.Tags, LUISIssueResult.CurrentResponse.Entities.Where(x => x.Type == Entities.StolenObject).FirstOrDefault()?.Entity))
                    {
                        await IssueCrimeReferenceNumber(context);
                    }
                    else
                    {
                        PromptDialog.Confirm(
                            context,
                            ConfirmPictureOfStolenObjectIsCorrect,
                            $"That looks like {imageAnalysis.Description.Captions[0].Text}. Are you sure this is a picture of the " +
                            $"{LUISIssueResult.CurrentResponse.Entities.Where(x => x.Type == Entities.StolenObject).FirstOrDefault()?.Entity}",
                            "Sorry, I didn't quite understand you, can you try again?",
                            promptStyle: PromptStyle.Auto);
                    }
                }
            }
        }
        private async Task ConfirmPictureOfStolenObjectIsCorrect(IDialogContext context, IAwaitable<bool> result)
        {
            var confirmed = await result;

            if (confirmed)
            {
                State = BotState.AskLocation;
                await ForwardToOperator(context);
                context.Wait(MessageReceivedAsync);
            }
            else
            {
                StolenObjectImages.ToList().Clear();
                PromptForStolenObjectUpload(context);
            }
        }
        private bool ContainsItemOrPseudonym(IList<ImageTag> tags, string theftObject)
        {
            if (tags.Any(x => string.Equals(x.Name, theftObject, StringComparison.InvariantCultureIgnoreCase)))
            {
                return true;
            }

            if (string.Equals(theftObject, "bike", StringComparison.InvariantCultureIgnoreCase))
            {
                return ContainsItemOrPseudonym(tags, "bicycle");
            }

            return false;
        }

        private async Task HandleAssultIssue(IDialogContext context)
        {
            if (LUISIssueResult.CurrentResponse.Entities.Any(x => x.Type == Entities.Weapon))
            {
                var weapon = LUISIssueResult.CurrentResponse.Entities.Where(x => x.Type == Entities.Weapon).FirstOrDefault();

                // Weapon involved, need extra assistance?
                PromptDialog.Confirm(
                    context,
                    ConfirmWeaponAdditionalServices,
                    $"I noticed you mentioned there was a {weapon.Entity}, do you need additional emergency services?",
                    "Sorry, I didn't quite understand you, can you try again?",
                    promptStyle: PromptStyle.Auto);
            }
            else
            {
                PromptDialog.Confirm(
                    context,
                    ConfirmAnyInjuriesFromAssault,
                    $"Do you have any injuries from the assault?",
                    "Sorry, I didn't quite understand you, can you try again?",
                    promptStyle: PromptStyle.Auto);
            }


            State = BotState.AskLocation;
        }
        private async Task ConfirmAnyInjuriesFromAssault(IDialogContext context, IAwaitable<bool> arg)
        {
            var confirm = await arg;

            if (confirm)
            {
                PromptDialog.Attachment(
                    context,
                    AssaultInjuriesUploaded,
                    $"Please upload any pictures of your injuries that you have.");
            }
        }
        private async Task AssaultInjuriesUploaded(IDialogContext context, IAwaitable<IEnumerable<Attachment>> arg)
        {
            var injuries = await arg;
            AssaultInjuryImages = injuries.Select(x => x.ContentUrl).ToList();

            if (AssaultInjuryImages.Any())
            {
                await context.PostAsync($"Thank you for uploading those for me.");
            }

            context.Wait(MessageReceivedAsync);
        }
        private async Task ConfirmWeaponAdditionalServices(IDialogContext context, IAwaitable<bool> arg)
        {
            var confirm = await arg;

            if (confirm)
            {
                // Complainant needs additional services, notify them here.
                await context.PostAsync($"We will notify additional emergency services that there is a " +
                    $"{LUISIssueResult.CurrentResponse.Entities.Where(x => x.Type == Entities.Weapon)?.FirstOrDefault().Entity} involved.");
            }
            else
            {
                await context.PostAsync($"Thank you for letting us know there was a " +
                    $"{LUISIssueResult.CurrentResponse.Entities.Where(x => x.Type == Entities.Weapon)?.FirstOrDefault().Entity} involved.");
            }

            PromptDialog.Confirm(
                context,
                ConfirmAnyInjuriesFromAssault,
                $"Do you have any injuries from the assault?",
                "Sorry, I didn't quite understand you, can you try again?",
                promptStyle: PromptStyle.Auto);
        }

        private async Task ForwardToOperator(IDialogContext context)
        {
            await context.PostAsync($"Thank you for the information. I am forwarding you to another member of staff who can comlpete your enquiry");
        }
        private async Task IssueCrimeReferenceNumber(IDialogContext context)
        {
            await context.PostAsync($"Thank you for the information. Your crime reference number is {nextCrimeReference++}");
        }
        private BotState IntentStringToIssue(Intents intent)
        {
            switch(intent)
            {
                case Intents.Theft: return BotState.Issue_Theft;
                case Intents.Assault: return BotState.Issue_Assault;
                case Intents.CarCrash: return BotState.Issue_CarCrash;
                case Intents.CriminalDamage: return BotState.Issue_CriminalDamage;
                case Intents.Harassment: return BotState.Issue_Harassment;
                case Intents.Information: return BotState.Issue_Information;
                case Intents.None: return BotState.Issue_None;
                default: return BotState.AskIssue;
            }
        }
    }
}