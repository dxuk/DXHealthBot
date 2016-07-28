using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace DXHealthBot
{
    public class LUISHealthClient
    {
        public static async Task<HealthLUIS> ParseUserInput(string strInput)
        {
            string strRet = string.Empty;
            string strEscaped = Uri.EscapeDataString(strInput);

            using (var client = new HttpClient())
            {
                string uri = "https://api.projectoxford.ai/luis/v1/application?id=4b4d4560-4b80-4b73-8d82-462e598d7280&subscription-key=5e4f605bd80b4f2bbd10789647bb5a10&q=" + strEscaped;
                HttpResponseMessage msg = await client.GetAsync(uri);

                if (msg.IsSuccessStatusCode)
                {
                    var jsonResponse = await msg.Content.ReadAsStringAsync();
                    var _Data = JsonConvert.DeserializeObject<HealthLUIS>(jsonResponse);
                    return _Data;
                }
            }
            return null;
        }
    }

    public class HealthLUIS
    {
        public string query { get; set; }
        public lIntent[] intents { get; set; }
        public lEntity[] entities { get; set; }
    }

    public class lIntent
    {
        public string intent { get; set; }
        public float score { get; set; }
    }

    public class lEntity
    {
        public string entity { get; set; }
        public string type { get; set; }
        public int startIndex { get; set; }
        public int endIndex { get; set; }
        public float score { get; set; }
    }
}