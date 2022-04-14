using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

using Newtonsoft.Json;

namespace DocIntel.Integrations.PassiveTotal
{
    public class PassiveTotalAPI
    {
        private readonly string _username;
        private readonly string _apiKey;
        private readonly WebProxy _proxy;
        private const string SERVER = "api.passivetotal.org";
        private readonly CredentialCache _credentialCache;

        public WhoisAPIClient Whois { get; set; }
        public ArticleAPIClient Articles { get; set; }

        public PassiveTotalAPI(string username, string apiKey, WebProxy proxy = null)
        {
            this._username = username;
            this._apiKey = apiKey;
            this._proxy = proxy;

            _credentialCache = new CredentialCache
            {
                { new System.Uri("https://" + SERVER + "/"), "Basic", new NetworkCredential(username, apiKey) }
            };

            Whois = new WhoisAPIClient(this);
            Articles = new ArticleAPIClient(this);
        }

        // public IEnumerable<PassiveTotalArticle> GetArticles()
        // {
        //     var query = new Dictionary<string, string>();
        //
        //     string resultData = GetJSON(query, "articles", out HttpStatusCode code);
        //     System.Console.WriteLine("---- " + code);
        //     // System.Console.WriteLine(resultData);
        //     File.WriteAllText("temp.json", resultData);
        //     System.Console.WriteLine("----");
        //
        //     var o = JsonConvert.DeserializeObject<PassiveTotalResponseArticles>(resultData);
        //
        //     return o.articles;
        // }

        public T GetObject<T>(object query, string endpointString, out HttpStatusCode httpStatusCode)
        {
            return JsonConvert.DeserializeObject<T>(GetJSON(query, endpointString, out httpStatusCode));
        }
        
        public string GetJSON(object query, string endpointString, out HttpStatusCode httpStatusCode)
        {
            var stream = new MemoryStream();
            GetFile(query, endpointString, stream, AcceptedFormat.JSON, "GET", out httpStatusCode);

            stream.Position = 0;
            StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
            string resultData = reader.ReadToEnd();
            Console.WriteLine(resultData);
            
            return resultData;
        }

        private void GetFile(object query, string endpointString, Stream stream, AcceptedFormat format, string method, out HttpStatusCode httpStatusCode)
        {
            var mime = format switch
            {
                AcceptedFormat.JSON => "application/json",
                AcceptedFormat.XML => "text/xml",
                AcceptedFormat.HTML => "text/html",
                AcceptedFormat.PDF => "application/pdf",
                AcceptedFormat.STIX => "application/stix",
                AcceptedFormat.CSV => "text/csv",
                AcceptedFormat.SNORT => "application/snort",
                AcceptedFormat.ZIP => "application/zip",
                _ => throw new NotImplementedException(),
            };
            HttpWebRequest request = BuildRequest(query, endpointString, method);

            byte[] buffer = new byte[4096];
            int count = 0;

            try {
                using WebResponse response = request.GetResponse();
                httpStatusCode = ((System.Net.HttpWebResponse)response).StatusCode;

                using Stream responseStream = response.GetResponseStream();
                do
                {
                    count = responseStream.Read(buffer, 0, buffer.Length);
                    stream.Write(buffer, 0, count);

                } while (count != 0);

            } catch (WebException e) {
                HttpWebResponse response = ((System.Net.HttpWebResponse)e.Response);
                if (response != null) {
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

        private HttpWebRequest BuildRequest(object query, string endpointString, string method)
        {
            
            // string queryString = string.Join("&", query.Select(_ => _.Key + "=" + _.Value));
            string end_point = endpointString;
            string url = "https://" + SERVER + "/v2/" + end_point;
            
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

            Console.WriteLine(url);
            
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            SetHeaders(request);
            if (_proxy != null)
                request.Proxy = _proxy;

            if (query != null && method == "POST") 
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

        private void SetHeaders(HttpWebRequest request)
        {
            // request.Credentials = _credentialCache;
            string authInfo = _username + ":" + _apiKey;
            authInfo = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(authInfo));
            request.Headers["Authorization"] = "Basic " + authInfo;
        }
    }
}