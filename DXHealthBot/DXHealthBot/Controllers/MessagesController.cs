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
using System.Net.Http.Headers;

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

        private async Task<string> ParseText(Activity activity)
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
                case "y":
                case "Y":
                    //this is hardcoded in for the calendar
                    //Get O365Login AccessToken
                    await Conversation.SendAsync(activity, () => new ActionDialog());
                    var events = await GetCalendarItems(userID);

            
                    var root = JsonConvert.DeserializeObject<Rootobject>(events);
                    var count = root.value.Count();
                    strResult += $"You had {count} events in that time which may have affected your sleep...\n\n";
                    strResult += Environment.NewLine;

                    strResult += string.Join("\n\n", root.value.Select(v => v.subject));

                    break;
                case "n":
                case "N":
                    strResult = "No problem! Your funeral...";
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
                    //parse text
                    if (string.IsNullOrEmpty(strRet))
                    { 
                        strRet = await ParseText(activity);
                    }
                  

                    //check LUIS intents
                    if (string.IsNullOrEmpty(strRet))
                    {
                        // LUIS
                        HealthLUIS stLuis = await LUISHealthClient.ParseUserInput(activity.Text);
                        strRet = await CheckIntentsAsync(stLuis, activity);
                        TelemetryClient telemetry = new TelemetryClient();
                       
                        telemetry.TrackEvent(stLuis.query);

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

        private async Task<string> GetCalendarItems(string userId)
        {
            string token = MyDependencies._store.GetToken(userId, CredentialStore.O365_TOKEN_KEY);
            var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var ub = new UriBuilder("https://graph.microsoft.com");

            ub.Path = "v1.0" + "/" + "me/calendar/events";
            ub.Query = "$select=subject";

            string resStr = string.Empty;

            var resp = await http.GetAsync(ub.Uri);

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                // If we are unauthorized here assume that our token may have expired and use the  
                // refresh token to get a new one and then try the request again.. 
                // TODO: handle this - we can cache the refresh token in the same flow as the access token
                // just haven't done it.
                return "";

                // Re-issue the same request (will use new auth token now) 
                //return await MakeRequestAsync(path, query);
            }

            if (resp.IsSuccessStatusCode)
            {
                resStr = await resp.Content.ReadAsStringAsync();
            }
            return resStr;
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