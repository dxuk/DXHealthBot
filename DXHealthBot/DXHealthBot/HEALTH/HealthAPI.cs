using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;

namespace DXHealthBot
{
    public class HealthAPI
    {
        private const string ApiVersion = "v1";

        public static async Task<string> MakeRequestAsync(string token, string path, string query = "")
        {
            var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var ub = new UriBuilder("https://api.microsofthealth.net");

            ub.Path = ApiVersion + "/" + path;
            ub.Query = query;

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

        public static async Task<string> GetActivity(string token, string activity, DateTime Start, DateTime end)
        {
            string res = string.Empty;
            try
            {
                res = await MakeRequestAsync(token, "me/Activities/",
                    string.Format("startTime={0}&endTime={1}&activityTypes={2}&ActivityIncludes=Details",
                    Start.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"),
                    end.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"),
                    activity));
            }
            catch (Exception ex)
            {
                return $"API Request Error - {ex.Message}";
            }

            await Task.Run(() =>
            {
                // Format the JSON string 
                var obj = JsonConvert.DeserializeObject(res);
                res = JsonConvert.SerializeObject(obj, Formatting.Indented);
            });

            return res;
        }
    }
}