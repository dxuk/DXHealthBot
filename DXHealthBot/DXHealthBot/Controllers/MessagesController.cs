﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using DXHealthBot.Model;
using Microsoft.Bot.Builder.Dialogs; 


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

       /* private async Task NotifyUser(this IDialogContext context, string messageText)
        { 
            if (!string.IsNullOrEmpty(messageText)) 
            { 
                 string serviceUrl = context.PrivateConversationData.Get<string>("ServiceUrl"); 
                 var connector = new ConnectorClient(new Uri(serviceUrl)); 
                 var reply = context.MakeMessage(); 
                 reply.Text = messageText; 
                 await connector.Conversations.ReplyToActivityAsync((Activity)reply); 
             } 
         }*/


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

                //get API tokens
                string healthToken = _creds.GetToken(userID, CredentialStore.MSHEALTHAPI_TOKEN_KEY);


                //now check the message text and process
                try
                {
                    //check for diagnostic requests
                    if (string.IsNullOrEmpty(strRet))
                    { 
                        strRet = CheckDiagnostics(activity);
                    }

                    //check LUIS intents
                    if (string.IsNullOrEmpty(strRet))
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