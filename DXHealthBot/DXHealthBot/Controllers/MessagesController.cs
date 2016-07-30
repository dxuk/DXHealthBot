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
using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder.Dialogs;
using DXHealthBot.DIALOGS;

namespace DXHealthBot
{
    public interface IIntentProcessor
    {
        Task<Tuple<bool, string>> ProcessIntentAsync(string userId, ICredentialStore creds, HealthLUIS data);
    }

    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// 
        /// 
        //test CI

        ICredentialStore _creds;
        public MessagesController()
        {
            _creds = MyDependencies._store;
        }

        private async Task<string> CheckIntentsAsync(HealthLUIS stLuis, Activity activity)
        {
            string strResult = string.Empty;

            foreach (var intentHandler in MyDependencies.IntentHandlers)
            {
                var handled = await intentHandler.ProcessIntentAsync(activity.From.Id, _creds, stLuis);
                if (handled.Item1 == true)
                {
                    strResult = handled.Item2;
                    break;
                }
            }

            if (string.IsNullOrEmpty(strResult))
            {
                strResult = "Sorry, I don't understand, please try again";
            }

            return strResult;
        }

        private string CheckDiagnostics(Activity activity)
        {
            string strResult = string.Empty;
            string userID = activity.From.Id;

            switch (activity.Text)
            {
                case "user":
                    strResult = $"The userID is: {userID}";
                    break;
                case "htoken":
                    string healthToken = _creds.GetToken(userID, CredentialStore.MSHEALTHAPI_TOKEN_KEY);
                    strResult = $"Your Health API token is: {healthToken} ";
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

                //Get user id
                var userID = activity.From.Id;
                if (string.IsNullOrEmpty(userID))
                {
                    strRet = ("Struggling to get a user id...");
                }

                

                //now check the message text and process
                try
                {
                    //check for diagnostic requests
                    if (string.IsNullOrEmpty(strRet))
                    { 
                        strRet = CheckDiagnostics(activity);
                    }

                    //Get O365Login AccessToken
                    await Conversation.SendAsync(activity, () => new ActionDialog());

                    //check LUIS intents
                    if (string.IsNullOrEmpty(strRet))
                    {
                        // LUIS
                        HealthLUIS stLuis = await LUISHealthClient.ParseUserInput(activity.Text);
                        strRet = await CheckIntentsAsync(stLuis, activity);
                        TelemetryClient telemetry = new TelemetryClient();
                       
                        telemetry.TrackEvent(strRet);

                    }
                }
                catch (Exception ex)
                { 
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