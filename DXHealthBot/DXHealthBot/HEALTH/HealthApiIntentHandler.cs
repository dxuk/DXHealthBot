using NodaTime;
using NodaTime.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Bot.Connector;
using DXHealthBot.Model;

namespace DXHealthBot.HEALTH
{
    public class HealthApiIntentHandler : IIntentProcessor
    {
        public async Task<Tuple<bool, string>> ProcessIntentAsync(string userId, ICredentialStore creds, HealthLUIS data)
        {
            bool ret = false;
            string resultStr = null;

            var token = creds.GetToken(userId, CredentialStore.MSHEALTHAPI_TOKEN_KEY);
            if (string.IsNullOrEmpty(token))
            {
                var loginUri = new Uri($"http://localhost:3979/api/auth/home?UserId={userId}");
                resultStr = $"Please pay a visit to {loginUri.ToString()} to associate your user identity with your Microsoft Health identity.";
                return Tuple.Create(true, resultStr);
            }

            var entity = data.entities.FirstOrDefault(e => e.type == "ActivityType");
            if (entity == null)
            {
                return Tuple.Create(false, "");
            }
    
            var entityStr = entity.entity;

            switch (data.intents[0].intent)
            {
                case "SummariseActivity":
                    if (entity != null)
                    {

                        var entityTime = (lEntity)data.entities.FirstOrDefault(e =>
                        e.type == "builtin.datetime.time" ||
                        e.type == "builtin.datetime.duration" ||
                        e.type == "builtin.datetime.date");

                        ParseResult<Period> res = null;

                        // TODO: parse the time formats correctly...
                        if (entityTime.type == "builtin.datetime.duration")
                        {
                            res = PeriodPattern.NormalizingIsoPattern.Parse(entityTime.resolution.duration);
                        }
                        else if (entityTime.type == "builtin.datetime.time")
                        {
                            res = PeriodPattern.NormalizingIsoPattern.Parse(entityTime.resolution.time);
                        }
                        else if (entityTime.type == "builtin.datetime.date")
                        {
                            var pattern = LocalDatePattern.CreateWithInvariantCulture("yyyy-MM-dd");
                            LocalDate parseResult = pattern.Parse(entityTime.resolution.date).Value;
                        }
                    }


                    //var st = SystemClock.Instance.GetCurrentInstant().InUtc().LocalDateTime - res.Value;

                    // TODO: Make it work dynamically
                    //DateTime start = st.ToDateTimeUnspecified();
                    DateTime start = DateTime.Now.AddDays(-1);
                    DateTime end = DateTime.Now;


                    var result = await HealthAPI.GetActivity(token, entityStr, start, end);
                    ret = true;
                    break;
                    //var entityStr = data.entities.FirstOrDefault(e => e.type == "ActivityType").entity;

                    //// This could be either date, time or duration..
                    //var entityTime = data.entities.FirstOrDefault(e =>
                    //    e.type == "builtin.datetime.time" ||
                    //    e.type == "builtin.datetime.duration" ||
                    //    e.type == "builtin.datetime.date");

                    //ParseResult<Period> res = null;

                    //// TODO: parse the time formats correctly...
                    //if (entityTime.type == "builtin.datetime.duration")
                    //{
                    //    res = PeriodPattern.NormalizingIsoPattern.Parse(entityTime.resolution.duration);
                    //}
                    //else if (entityTime.type == "builtin.datetime.time")
                    //{
                    //    res = PeriodPattern.NormalizingIsoPattern.Parse(entityTime.resolution.time);
                    //}
                    //else if (entityTime.type == "builtin.datetime.date")
                    //{
                    //    var pattern = LocalDatePattern.CreateWithInvariantCulture("yyyy-MM-dd");
                    //    LocalDate parseResult = pattern.Parse(entityTime.resolution.date).Value;
                    //}

                    //var entity = data.entities[0].entity;

                    //// Now call the relevant Microsoft Health API and respond to the user...
                    //if (entityTime.type == "builtin.datetime.duration")
                    //{
                    //    try
                    //    {
                    //        var st = SystemClock.Instance.GetCurrentInstant().InUtc().LocalDateTime - res.Value;

                    //        DateTime start = st.ToDateTimeUnspecified();
                    //        DateTime end = DateTime.Now;
                    //        var res2 = await GetActivity(token, entityStr, start, end);
                    //        var sleep = JsonConvert.DeserializeObject<Sleep>(res2);

                    //        // create a textual summary of sleep in that period...
                    //        int num = sleep.itemCount;
                    //        if (num <= 0)
                    //        {
                    //            prompt = "You didn't track any sleep";
                    //            break;
                    //        }
                    //        var total = sleep.sleepActivities.Sum((a) =>
                    //        {
                    //            if (a.sleepDuration != null)
                    //            {
                    //                var dur = PeriodPattern.NormalizingIsoPattern.Parse(a.sleepDuration);
                    //                return dur.Value.ToDuration().Ticks;
                    //            }
                    //            else
                    //                return 0;
                    //        });

                    //        var av = total / num;
                    //        var sleepSpan = TimeSpan.FromTicks((long)av);
                    //        var totalSpan = TimeSpan.FromTicks(total);

                    //        var avSleepStr = $"{sleepSpan.ToString(@"%h")} hrs {sleepSpan.ToString(@"%m")} mins";
                    //        var totalSleepStr = $"{totalSpan.ToString(@"%d")} days {totalSpan.ToString(@"%h")} hrs {totalSpan.ToString(@"%m")} mins";

                    //        prompt = $"You have tracked {num} sleeps - average sleep per night {avSleepStr} for a total of {totalSleepStr}";
                    //    }
                    //    catch (Exception ex)
                    //    {

                    //    }
            }
            return Tuple.Create(ret, resultStr);
        }
    }
}