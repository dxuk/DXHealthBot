using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using DXHealthBot.Model;

namespace DXHealthBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// 
        /// 

        ICredentialStore _creds;
        public MessagesController()
        {
            _creds = MyDependencies._store;
        }


        private async Task<MSHealthUserText> ParseUserInput(string input)
        {
            string escaped = Uri.EscapeDataString(input);

            using (var http = new HttpClient())
            {
                string key = Environment.GetEnvironmentVariable("MSHEALTHBOT_LUIS_API_KEY");
                string id = Environment.GetEnvironmentVariable("MSHEALTHBOT_LUIS_APP_ID");

                string uri = $"https://api.projectoxford.ai/luis/v1/application?id={id}&subscription-key={key}&q={escaped}";
                var resp = await http.GetAsync(uri);
                resp.EnsureSuccessStatusCode();

                var strRes = await resp.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<MSHealthUserText>(strRes);
                return data;
            }
        }

        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                // calculate something for us to return
                int length = (activity.Text ?? string.Empty).Length;

                // return our reply to the user
                Activity reply = activity.CreateReply($"You sent {activity.Text} which was {length} characters");
                await connector.Conversations.ReplyToActivityAsync(reply);
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
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
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}