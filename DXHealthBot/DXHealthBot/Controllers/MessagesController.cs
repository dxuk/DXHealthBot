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

        private string CheckIntents(HealthLUIS stLuis)
        {
            string strResult = string.Empty;

            switch (stLuis.intents[0].intent)
            {
                case "SummariseActivity":
                    strResult = "Summarising activity";
                    break;
                case "None":
                    break;
                default:
                    strResult = "Sorry, I don't understand, please try again";
                    break;
            }
            return strResult;
        }

        private string CheckDiagnostics(Activity activity)
        {
            string strResult = string.Empty;

            switch (activity.Text)
            {
                case "htoken":
                    strResult = "Your Health API token is: ";
                    break;
                case "None":
                    break;
            }
            return strResult;
        }


        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                string strRet = string.Empty;

                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

               
                try
                {
                    //check for diagnostic requests
                    strRet = CheckDiagnostics(activity);

                    //check LUIS intents
                    if (strRet == string.Empty)
                    {
                        // LUIS
                        HealthLUIS stLuis = await LUISHealthClient.ParseUserInput(activity.Text);
                        strRet = CheckIntents(stLuis);
                    }
                }
                catch (Exception ex)
                {
                    //print exception into chat stream
                    strRet = ex.Message;
                }


                // return our reply to the user
                Activity reply = activity.CreateReply(strRet);
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
                // Handle knowing that the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}