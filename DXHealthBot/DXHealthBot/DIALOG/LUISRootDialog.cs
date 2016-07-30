using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace DXHealthBot.DIALOGS
{
    [LuisModel("5e4f605bd80b4f2bbd10789647bb5a10", "4b4d4560-4b80-4b73-8d82-462e598d7280")]
    [Serializable]
    public class LUISRootDialog : LuisDialog<object>
    {
        private const string ENTITY_TOPIC = "ActivityType";

        private string responseMessage;


        [LuisIntent("SumariseActivity")]
        public async Task GiveInfo(IDialogContext context, LuisResult result)
        {
            string what;
            EntityRecommendation topic;
            if (result.TryFindEntity(ENTITY_TOPIC, out topic))
            {
                what = topic.Entity;
                //do something

            }
            else // if entity could not be found
            {
                responseMessage = "Please tell me what you want to know. ";
            }
            await context.PostAsync(responseMessage);
            context.Wait(MessageReceived);
        }

        [LuisIntent("")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            string message = $"Sorry I did not understand this. \n\n Intents supported:" + string.Join(", ", result.Intents.Select(i => i.Intent));
            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }

        public LUISRootDialog(ILuisService service = null)
            : base(service)
        {
        }


    }
}