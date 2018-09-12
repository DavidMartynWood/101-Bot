using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace NonEmergencyBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.GetActivityType() == ActivityTypes.Message)
            {
                await Conversation.SendAsync(activity, () => new Dialogs.RootDialog());
            }
            else
            {
                await HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private async  Task<Activity> HandleSystemMessage(Activity message)
        {
            string messageType = message.GetActivityType();
            if (messageType == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (messageType == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
                if (message.MembersAdded.Any(x => x.Id == message.Recipient.Id))
                {
                    ConnectorClient connector = new ConnectorClient(new Uri(message.ServiceUrl));
                    var cheshirePoliceLogo =
                        "https://www.police.uk/static/img/crest/cheshire.png";

                    var reply = message.CreateReply();
                    var card = new HeroCard
                    {
                        Title = "Cheshire Police 101",
                        Text = "Welcome to Cheshire Police 101 Web Chat",
                        Images = new List<CardImage> { new CardImage(cheshirePoliceLogo) },
                        
                    };
                    reply.Attachments.Add(card.ToAttachment());
                    connector.Conversations.ReplyToActivity(reply);

                    reply = message.CreateReply("I am just going to take a few simple details from you " +
                        "so our operator will know how to help you.");
                    connector.Conversations.ReplyToActivity(reply);

                    await Conversation.SendAsync(message, () => new Dialogs.RootDialog());
                }
            }
            else if (messageType == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (messageType == ActivityTypes.Typing)
            {
                // Handle knowing that the user is typing
            }

            return null;
        }
    }
}