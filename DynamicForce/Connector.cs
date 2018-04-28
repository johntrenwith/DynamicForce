using Common.Logging;
using Salesforce.Common;
using Salesforce.Force;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace DynamicForce
{
    public class Connector : IConnector
    {
        private ILog _log;
        private HttpClient _jsonHttpClient;
        private HttpClient _xmlHttpClient;        
        private string _consumerKey;
        private string _consumerSecret;
        private string _username;
        private string _password;
        private string _instanceUrl;
        private string _authToken;
        private string _apiVersion;
        private bool _isSandboxUser;

        public Connector(string consumerKey, string consumerSecret, string username,
            string password, bool isSandboxUser,
            ILog log, 
            HttpClient jsonHttpClient,
            HttpClient xmlHttpClient)
        {
            _consumerKey = consumerKey;
            _consumerSecret = consumerSecret;
            _username = username;
            _password = password;
            _isSandboxUser = isSandboxUser;
            _log = log;
            _jsonHttpClient = jsonHttpClient;
            _xmlHttpClient = xmlHttpClient;
        }

        public async Task<ForceClient> Authenticate()
        {

            var auth = new AuthenticationClient(_xmlHttpClient);

            _log.Info("Authenticating with Salesforce");

            var url = _isSandboxUser ? "https://test.salesforce.com/services/oauth2/token"
                : "https://login.salesforce.com/services/oauth2/token";

            await auth.UsernamePasswordAsync(_consumerKey, _consumerSecret, _username, _password, url);
            _log.Info("Connected to Salesforce");

            _apiVersion = auth.ApiVersion;
            _authToken = auth.AccessToken;
            _instanceUrl = auth.InstanceUrl;

            return new ForceClient(auth.InstanceUrl, auth.AccessToken, auth.ApiVersion, _xmlHttpClient, _jsonHttpClient);
        }

        public async Task<string> DescribeObject(string url)
        {
            string descriptionContent = string.Empty;
            HttpClient httpClient = new HttpClient();
            url = _instanceUrl + url;

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);

            request.Headers.Add("Authorization", "Bearer " + _authToken);

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                HttpResponseMessage response = await httpClient.SendAsync(request);

                descriptionContent = await response.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                descriptionContent = e.Message;
            }

            httpClient.Dispose();

            return descriptionContent;
        }

        public string ApiVersion { get { return _apiVersion; } }
    }
}

