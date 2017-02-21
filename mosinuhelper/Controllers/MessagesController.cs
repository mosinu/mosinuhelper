using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.Text;

namespace mosinuhelper
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
            // Global values
            bool boolAskedForUserName = false;
            string strUserName = "";
            if (activity.Type == ActivityTypes.Message)
            {
                // Get any saved values
                StateClient sc = activity.GetStateClient();
                BotData userData = sc.BotState.GetPrivateConversationData(
                    activity.ChannelId, activity.Conversation.Id, activity.From.Id);
                boolAskedForUserName = userData.GetProperty<bool>("AskedForUserName");
                strUserName = userData.GetProperty<string>("UserName") ?? "";
                // Create text for a reply message   
                StringBuilder strReplyMessage = new StringBuilder();
                if (boolAskedForUserName == false) // Never asked for name
                {
                    strReplyMessage.Append($"Hello, I am the **Mosinu Helper** Bot");
                    strReplyMessage.Append($"\n");
                    strReplyMessage.Append($"You can ask me anything");
                    strReplyMessage.Append($"\n");
                    strReplyMessage.Append($"and I will try to assist you");
                    strReplyMessage.Append($"\n\n");
                    strReplyMessage.Append($"What is your name?");
                    // Set BotUserData
                    userData.SetProperty<bool>("AskedForUserName", true);
                }
                else // Have asked for name
                {
                    if (strUserName == "") // Name was never provided
                    {
                        // If we have asked for a username but it has not been set
                        // the current response is the user name
                        strReplyMessage.Append($"Hello {activity.Text}!");
                        // Set BotUserData
                        userData.SetProperty<string>("UserName", activity.Text);
                    }
                    else // Name was provided
                    {
                        strReplyMessage.Append($"{strUserName}, tell me more about {activity.Text}");
                    }
                }
                // Save BotUserData
                sc.BotState.SetPrivateConversationData(
                    activity.ChannelId, activity.Conversation.Id, activity.From.Id, userData);
                // Create a reply message
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                Activity replyMessage = activity.CreateReply(strReplyMessage.ToString());
                await connector.Conversations.ReplyToActivityAsync(replyMessage);
            }
            else
            {
                Activity replyMessage = HandleSystemMessage(activity);
                if (replyMessage != null)
                {
                    ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                    await connector.Conversations.ReplyToActivityAsync(replyMessage);
                }
            }
            // Return response
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Get BotUserData
                StateClient sc = message.GetStateClient();
                BotData userData = sc.BotState.GetPrivateConversationData(
                    message.ChannelId, message.Conversation.Id, message.From.Id);
                // Set BotUserData
                userData.SetProperty<string>("UserName", "");
                userData.SetProperty<bool>("AskedForUserName", false);
                // Save BotUserData
                sc.BotState.SetPrivateConversationData(
                    message.ChannelId, message.Conversation.Id, message.From.Id, userData);
                // Create a reply message
                ConnectorClient connector = new ConnectorClient(new Uri(message.ServiceUrl));
                Activity replyMessage = message.CreateReply("Personal data has been deleted.");
                return replyMessage;
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
                ConnectorClient connector = new ConnectorClient(new Uri(message.ServiceUrl));
                Activity replyMessage = message.CreateReply("I see you are typing, I will wait.");
                return replyMessage;
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }

    public class Scores
    {
        public double anger { get; set; }
        public double contempt { get; set; }
        public double disgust { get; set; }
        public double fear { get; set; }
        public double happiness { get; set; }
        public double neutral { get; set; }
        public double sadness { get; set; }
        public double surprise { get; set; }
    }

    public class EmotionResult
    {
        public Scores scores { get; set; }
    }

}