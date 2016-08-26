using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Configuration;
using System.Web.UI;
using CTCT.Components;

namespace CTCT.Authentication.WebPage
{
    /// <summary>
    /// Class used to authenticate from web application
    /// </summary>
    public sealed class AuthenticationWebPage : Page
    {
        private const string RequestAuthorizationUrl = "https://oauth2.constantcontact.com/oauth2/oauth/siteowner/authorize?response_type=code&client_id={0}&redirect_uri={1}&state={2}";
        private const string RequestAccessTokenUrl = "https://oauth2.constantcontact.com/oauth2/oauth/token?grant_type=authorization_code&client_id={0}&client_secret={1}&code={2}&redirect_uri={3}";

        private string _url = string.Empty;
        private static string _redirectUrl = string.Empty;
        private static string _apiKey = string.Empty;
        private static string _clientSecret = string.Empty;
        
        /// <summary>
        /// Set CTCT configuration information if the api key and client secret are stored somewhere besides the default names in the configuration settings.
        /// This method is optional, and if it is not called, the configuration is queried for these values.
        /// </summary>
        /// <param name="apiKey"></param>
        /// <param name="clientSecret"></param>
        public static void Initialize(string apiKey, string clientSecret)
        {
            _apiKey = apiKey;
            _clientSecret = clientSecret;
        }

        /// <summary>
        /// Access token field
        /// </summary>
        public string AccessToken;

        /// <summary>
        /// State field
        /// </summary>
        public string State;

        /// <summary>
        /// Httpcontext field
        /// </summary>
        public HttpContext HttpContext;

        /// <summary>
        /// Initialize new instance of AuthenticationWebForm class
        /// </summary>
        /// <param name="httpContext">current application context</param>
        /// <param name="state">state query parameter</param>
        /// <param name="redirectUrl">where to redirect after authentication</param>
        public AuthenticationWebPage(HttpContext httpContext, string state, string redirectUrl)
        {
            HttpContext = httpContext;
            State = state;

            if (redirectUrl != null)
                _redirectUrl = redirectUrl;

            if (string.IsNullOrEmpty(_apiKey))
                _apiKey = ConfigurationManager.AppSettings["APIKey"];
            if (string.IsNullOrEmpty(_redirectUrl))
                _redirectUrl = ConfigurationManager.AppSettings["RedirectURL"];
            if (string.IsNullOrEmpty(_clientSecret))
                _clientSecret = ConfigurationManager.AppSettings["ClientSecretKey"];  
        }

        /// <summary>
        /// Gets URL with all parameters filled in for requesting authorization code
        /// </summary>
        public string GetAuthorizationCodeUrl() {
            _url = String.Format(RequestAuthorizationUrl, HttpUtility.UrlEncode(_apiKey),
                                 HttpUtility.UrlEncode(_redirectUrl), HttpUtility.UrlEncode(State));
            return _url;
        }

        /// <summary>
        /// Gets authorization code
        /// </summary>
        public void GetAuthorizationCode()
        {
            HttpContext.Response.Redirect(GetAuthorizationCodeUrl());
        }

        /// <summary>
        /// Gets access token by code
        /// </summary>
        /// <param name="code">authorization code</param>
        /// <returns>access token</returns>
        public string GetAccessTokenByCode(string code)
        {
            _url = String.Format(RequestAccessTokenUrl, HttpUtility.UrlEncode(_apiKey),
                                 HttpUtility.UrlEncode(_clientSecret), HttpUtility.UrlEncode(code),
                                 HttpUtility.UrlEncode(_redirectUrl));

            try
            {
                var request = WebRequest.Create(_url) as HttpWebRequest;
                if (request != null)
                {
                    request.Method = "POST";

                    var response = request.GetResponse() as HttpWebResponse;

                    if (response != null)
                    {
                        using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                        {
                            var responseText = reader.ReadToEnd();

                            var info = Component.FromJSON<AuthenticationRequest>(responseText);

                            AccessToken = info.AccessToken;
                        }
                        response.Close();
                    }
                }
            }
            catch (WebException)
            {
                return null;
            }
            catch (Exception)
            {
                return null;
            }

            return AccessToken;
        }
    }
}
