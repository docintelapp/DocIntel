using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;

using Newtonsoft.Json;

namespace DocIntel.Integrations.ThreatMatch
{
    public class APIClient
    {
        private readonly string _username;
        private readonly string _apiKey;
        private readonly WebProxy _proxy;
        string SERVER = "eu.threatmatch.com";
        public AccessTokenResponse AccessToken { get; }

        public APIClient(string username, string apiKey, WebProxy proxy = default)
        {
            _username = username;
            _apiKey = apiKey;
            _proxy = proxy;
            AccessToken = GetAccessToken();
            Profile = new ProfileAPIClient(this);
            Reports = new ReportAPIClient(this);
        }

        public ProfileAPIClient Profile { get; set; }
        public ReportAPIClient Reports { get; set; }

        public ClientSetting GetSetting()
        {
            var request = BuildPostRequest("GET","developers-platform", new
            {
                
            });

            return GetResponseObject<ClientSetting>(request);
        }
        public AccessTokenResponse GetAccessToken()
        {
            var request = BuildPostRequest("POST","developers-platform/token", new
            {
                client_id = _username,
                client_secret = _apiKey
            });

            var accessTokenResponse = GetResponseObject<AccessTokenResponse>(request);
            
            return accessTokenResponse;
        }

        public T GetResponseObject<T>(HttpWebRequest request)
        {
            return JsonConvert.DeserializeObject<T>(GetResponseString((request)));
        }

        public string GetResponseString(HttpWebRequest request)
        {
            Stream stream;
            stream = new MemoryStream();

            GetResponseStream(request, stream);

            stream.Position = 0;
            StreamReader reader2 = new StreamReader(stream, System.Text.Encoding.UTF8);
            string resultData = reader2.ReadToEnd();
            Console.WriteLine(resultData);
            return resultData;
        }

        public void GetResponseStream(HttpWebRequest request, Stream stream)
        {
            HttpStatusCode httpStatusCode;
            try
            {
                using WebResponse response = request.GetResponse();
                httpStatusCode = ((System.Net.HttpWebResponse) response).StatusCode;
                Console.WriteLine(httpStatusCode.ToString() +  "--");
                Console.WriteLine(response.ContentType);

                using Stream responseStream = response.GetResponseStream();

                byte[] buffer = new byte[4096];
                int count = 0;
                do
                {
                    count = responseStream.Read(buffer, 0, buffer.Length);
                    stream.Write(buffer, 0, count);
                } while (count != 0);
            }
            catch (WebException e)
            {
                HttpWebResponse response = ((System.Net.HttpWebResponse) e.Response);
                if (response != null)
                {
                    httpStatusCode = response.StatusCode;
                    var reader = new StreamReader(response.GetResponseStream());
                    System.Console.WriteLine(reader.ReadToEnd());
                }
                else
                {
                    httpStatusCode = HttpStatusCode.InternalServerError;
                    System.Console.WriteLine(e.Message);
                }
            }
        }

        public HttpWebRequest BuildPostRequest(string method, string endpointString, object query = null)
        {
            string end_point = endpointString;
            string url = end_point;
            if (!endpointString.StartsWith("http")) 
                url = "https://" + SERVER + "/api/" + end_point;
            
            if (query != null && method == "GET")
            {
                var o = JsonConvert.SerializeObject(query);
                var d = JsonConvert.DeserializeObject<IDictionary<string, string>>(o);
                if (d.Any())
                {
                    var qs = d.Where(_ => !string.IsNullOrEmpty(_.Value)).Select(x => HttpUtility.UrlEncode(x.Key) + "=" + HttpUtility.UrlEncode(x.Value));
                    url += "?" + string.Join("&", qs);
                }
            }
            Console.WriteLine("URL ==> " + url);
            
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Proxy = _proxy;

            request.AllowAutoRedirect = true;
            
            if (AccessToken != null)
                SetHeaders(request, AccessToken);
            
            if (query != null & method == "POST") 
            {
                request.Method = "POST";
                request.ContentType = "application/json";
                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(JsonConvert.SerializeObject(query));
                }
            } 
            
            return request;
        }

        private void SetHeaders(HttpWebRequest request, AccessTokenResponse at)
        {
            request.Headers["Authorization"] = "Bearer " + at.AccessToken;
        }
    }

    public class ClientSetting
    {
        public Dictionary<string,string> Client { get; set; }
    }

    public class AccessTokenResponse
    {
        [JsonProperty("access_token")] public string AccessToken { get; set; }
        [JsonProperty("token_type")] public string TokenType { get; set; }
        [JsonProperty("expires_at")] public int ExpiresAt { get; set; }
    }
}