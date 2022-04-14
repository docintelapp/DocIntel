using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using Newtonsoft.Json;

namespace DocIntel.Integrations.ThreatConnect
{
    public class SecurityLabelAPIClient
    {
        
    }

    public class TagAPIClient
    {
        
    }

    public class TaskAPIClient
    {
        
    }

    public class VictimAPIClient
    {
        
    }
    
    public class APIClient
    {
        private readonly string _accessId;
        private readonly string _secretKey;
        
        private string _baseURL = "https://api.threatconnect.com";
        private readonly WebProxy _proxy;

        public APIClient(string accessId, string secretKey, WebProxy proxy = default)
        {
            _accessId = accessId;
            _secretKey = secretKey;
            _proxy = proxy;
            Owners = new OwnerAPIClient(this);
            Groups = new GroupAPIClient(this);
            Indicators = new IndicatorAPIClient(this);
        }

        public IndicatorAPIClient Indicators { get; set; }

        public OwnerAPIClient Owners { get; }
        public GroupAPIClient Groups { get; set; }

        public async Task<T> Query<T>(string apiPath, string apiMethod = "GET", Dictionary<string,string> parameters = null)
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler()
            {
                Proxy = _proxy
            };
            HttpClient httpClient = new HttpClient(httpClientHandler);

            if (parameters != null)
            {
                var queryString = string.Join("&", 
                    parameters.Select(kv 
                        => HttpUtility.UrlEncode(kv.Key) + "=" + HttpUtility.UrlEncode(kv.Value)));
                if (!string.IsNullOrEmpty(queryString))
                {
                    apiPath += "?" + queryString;
                }
            }

            ConfigureHttpClient(httpClient, apiPath, apiMethod);

            try
            {
                if (apiMethod == "GET")
                {
                    Console.WriteLine(apiPath);
                    var res = await httpClient.GetAsync(_baseURL + apiPath);
                    
                    Console.WriteLine(res.StatusCode + " ----");
                    var stringRes = await res.Content.ReadAsStringAsync();
                    Console.WriteLine(stringRes);
                    Console.WriteLine("----");

                    return JsonConvert.DeserializeObject<T>(stringRes);
                }
                else
                {
                    throw new NotImplementedException(apiMethod + " method is not (yet) supported");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception " + e.Message);
                Console.WriteLine(e.StackTrace);
            }

            return default(T);
        }
        
        public async Task<Stream> Download(string apiPath, string apiMethod = "GET", Dictionary<string,string> parameters = null)
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler()
            {
                Proxy = _proxy
            };
            HttpClient httpClient = new HttpClient(httpClientHandler);
            if (parameters != null)
            {
                var queryString = string.Join("&", 
                    parameters.Select(kv 
                        => HttpUtility.UrlEncode(kv.Key) + "=" + HttpUtility.UrlEncode(kv.Value)));
                if (!string.IsNullOrEmpty(queryString))
                {
                    apiPath += "?" + queryString;
                }
            }
            
            ConfigureHttpClient(httpClient, apiPath, apiMethod);

            try
            {
                if (apiMethod == "GET")
                {
                    var res = await httpClient.GetAsync(_baseURL + apiPath);
                    return await res.Content.ReadAsStreamAsync();
                }
                else
                {
                    throw new NotImplementedException(apiMethod + " method is not (yet) supported");
                }
            }
            catch (Exception e) 
            {
                Console.WriteLine("Exception " + e.Message);
                Console.WriteLine(e.StackTrace);
            }

            return null;
        }

        private void ConfigureHttpClient(HttpClient httpClient, string apiPath, string apiMethod)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var signature = $"{apiPath}:{apiMethod}:{timestamp}";

            byte[] message = Encoding.ASCII.GetBytes(signature);
            var hash = new HMACSHA256(Encoding.ASCII.GetBytes(_secretKey));
            var hmac_signature =
                System.Convert.ToBase64String(hash.ComputeHash(message));
            var authorization = $"TC {_accessId}:{hmac_signature}";

            httpClient.DefaultRequestHeaders.Add("Authorization", authorization);
            httpClient.DefaultRequestHeaders.Add("Timestamp", timestamp.ToString());
        }
    }
}