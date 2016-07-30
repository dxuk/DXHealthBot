using System;
using System.Threading;
using System.Threading.Tasks;
using AuthBot;
using AuthBot.Dialogs;
using AuthBot.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;


namespace DXHealthBot.DIALOGS
{


    [Serializable]
    public class ActionDialog : IDialog<string>
    {
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            var message = await item;
            
            //endpoint v2
            if (string.IsNullOrEmpty(await context.GetAccessToken(AuthSettings.Scopes)))
            {
                await context.Forward(new AzureAuthDialog(AuthSettings.Scopes), this.ResumeAfterAuth, message, CancellationToken.None);
            }
            else
            {
                //endpoint v2
                var accessToken = await context.GetAccessToken(AuthSettings.Scopes);
                MyDependencies._store.AddToken(message.From.Id, CredentialStore.O365_TOKEN_KEY, accessToken);
                if (string.IsNullOrEmpty(accessToken))
                {
                    return;
                }

                //await context.PostAsync($"Your Office365 access token is: {accessToken}");

                //To logout use this: 
                //await context.Logout();
                //context.Wait(this.MessageReceivedAsync);


                context.Wait(MessageReceivedAsync);

            }

        }

        private async Task ResumeAfterAuth(IDialogContext context, IAwaitable<string> result)
        {
            var message = await result;

            await context.PostAsync(message);
            context.Wait(MessageReceivedAsync);
        }
    }
}

