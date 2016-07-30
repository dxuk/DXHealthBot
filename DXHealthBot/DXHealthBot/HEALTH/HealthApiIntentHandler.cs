using NodaTime;
using NodaTime.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Bot.Connector;
using DXHealthBot.Model;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

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
                resultStr = $"Hi, I am the DXHealthBot. \n\n Hope you are well! \n\n I need to connect to your Health data in order to assist you. \n\n Please visit {loginUri.ToString()} to enable me to have access to your Microsoft Health data.";
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
                    ParseResult<Period> res = null;
                    DateTime dt = new DateTime();
                    DateTime start = new DateTime();
                    if (entity != null)
                    {
                        var entityTime = (lEntity)data.entities.FirstOrDefault(e =>
                        e.type == "builtin.datetime.time" ||
                        e.type == "builtin.datetime.duration" ||
                        e.type == "builtin.datetime.date");

                        // TODO: parse the time formats correctly...
                        if (entityTime.type == "builtin.datetime.duration")
                        {
                            if (entityTime.resolution.duration == "PT24H")
                            {
                                start = DateTime.Now.AddDays(-1);
                            }
                            else
                            {
                                res = PeriodPattern.NormalizingIsoPattern.Parse(entityTime.resolution.duration);
                                var st1 = SystemClock.Instance.GetCurrentInstant().InUtc().LocalDateTime - res.Value;
                                start = st1.ToDateTimeUnspecified();
                            }
                        }
                        else if (entityTime.type == "builtin.datetime.time")
                        {
                            var str1 = Regex.Replace(entityTime.resolution.time, "[A-Za-z ]", "");
                            dt = DateTime.Parse(str1, null, System.Globalization.DateTimeStyles.RoundtripKind);
                            start = dt;
                            //res = PeriodPattern.NormalizingIsoPattern.Parse(entityTime.resolution.time);
                        }
                        else if (entityTime.type == "builtin.datetime.date")
                        {
                            //dt = DateTime.Parse(entityTime.resolution.date, null, System.Globalization.DateTimeStyles.RoundtripKind);

                            //var date = LocalDatePattern.IsoPattern.Parse(entityTime.resolution.date);
                            //var val = date.Value;
                            //var st1 = SystemClock.Instance.GetCurrentInstant().InUtc().LocalDateTime - date;
                            //start = st1.ToDateTimeUnspecified();

                            var pattern = LocalDatePattern.CreateWithInvariantCulture("yyyy-MM-dd");
                            LocalDate parseResult = pattern.Parse(entityTime.resolution.date).Value;
                            start = parseResult.ToDateTimeUnspecified();
                        }
                    }

                    LocalDateTime st; 
                    if (res != null)
                        st = SystemClock.Instance.GetCurrentInstant().InUtc().LocalDateTime - res.Value;

                    // TODO: Make it work dynamically
                    //DateTime start = dt;
                    //DateTime start = st.ToDateTimeUnspecified();
                    //DateTime start = DateTime.Now.AddDays(-1);
                    DateTime end = DateTime.Now;

                    var result = await HealthAPI.GetActivity(token, entityStr, start, end);
                    var sleep = JsonConvert.DeserializeObject<Sleep>(result);

                    // create a textual summary of sleep in that period...
                    int num = sleep.itemCount;
                    if (num <= 0)
                    {
                        resultStr = "You didn't track any sleep";
                        break;
                    }
                    var total = sleep.sleepActivities.Sum((a) =>
                    {
                        if (a.sleepDuration != null)
                        {
                            var dur = PeriodPattern.NormalizingIsoPattern.Parse(a.sleepDuration);
                            return dur.Value.ToDuration().Ticks;
                        }
                        else
                            return 0;
                    });

                    var av = total / num;
                    var sleepSpan = TimeSpan.FromTicks((long)av);
                    var totalSpan = TimeSpan.FromTicks(total);

                    var avSleepStr = $"{sleepSpan.ToString(@"%h")} hrs {sleepSpan.ToString(@"%m")} mins";
                    var totalSleepStr = $"{totalSpan.ToString(@"%d")} days {totalSpan.ToString(@"%h")} hrs {totalSpan.ToString(@"%m")} mins";

                    resultStr = $"You have tracked {num} sleeps - average sleep per night {avSleepStr} for a total of {totalSleepStr} \n\n This was an insufficient amount of sleep :( \n\n Do you want more details (y/n)?";
  

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