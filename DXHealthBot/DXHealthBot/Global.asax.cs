using DXHealthBot.HEALTH;
using Microsoft.IdentityModel.Protocols;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;

namespace DXHealthBot
{

    public interface ICredentialStore
    {
        string GetToken(string id, string tokenKey);
        void AddToken(string id, string tokenKey, string token);
    }

    public class CredentialStore : ICredentialStore
    {
        public const string MSHEALTHAPI_TOKEN_KEY = "HEALTH_TOKEN_KEY";
        public const string O365_TOKEN_KEY = "O365_TOKEN_KEY";

        Dictionary<string, Dictionary<string, string>> _idMap = new Dictionary<string, Dictionary<string, string>>();

        public void AddToken(string id, string tokenKey, string token)
        {
            Dictionary<string, string> dict;
            bool exists = _idMap.TryGetValue(id, out dict);
            if (!exists)
            {
                dict = new Dictionary<string, string>();
                _idMap[id] = dict;
            }

            dict[tokenKey] = token;
        }

        public string GetToken(string id, string tokenKey)
        {
            var hacktoken = Environment.GetEnvironmentVariable("DXHACKTOKEN");
            if (!string.IsNullOrEmpty(hacktoken))
                return hacktoken;

            Dictionary<string, string> dict = null;
            string token = null;
            if (_idMap.TryGetValue(id, out dict))
            {
                if (dict.TryGetValue(tokenKey, out token))
                {
                    return token;
                }
            }
            return null;
        }
    }

    public static class MyDependencies
    {
        static MyDependencies()
        {
            // Register Intent Processors...
            IntentHandlers.Add(new HealthApiIntentHandler());
        }

        public static ICredentialStore _store = new CredentialStore();
        public static List<IIntentProcessor> IntentHandlers = new List<IIntentProcessor>();
    }

    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            ReadOAuthSettings();
        }

        private void ReadOAuthSettings()
        {
            AuthBot.Models.AuthSettings.Mode = ConfigurationManager.AppSettings["ActiveDirectory.Mode"];

            AuthBot.Models.AuthSettings.EndpointUrl = ConfigurationManager.AppSettings["ActiveDirectory.EndpointUrl"];

            AuthBot.Models.AuthSettings.Tenant = ConfigurationManager.AppSettings["ActiveDirectory.Tenant"];

            AuthBot.Models.AuthSettings.RedirectUrl = ConfigurationManager.AppSettings["ActiveDirectory.RedirectUrl"];

            AuthBot.Models.AuthSettings.ClientId = ConfigurationManager.AppSettings["ActiveDirectory.ClientId"];

            AuthBot.Models.AuthSettings.ClientSecret = ConfigurationManager.AppSettings["ActiveDirectory.ClientSecret"];

            AuthBot.Models.AuthSettings.Scopes = ConfigurationManager.AppSettings["ActiveDirectory.Scopes"].Split(',');
        }
    }

    
}
