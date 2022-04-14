using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

using Newtonsoft.Json;

namespace DocIntel.Integrations.FireEye
{
    public class FireEyeResponses<T> {
        public bool success { get; set; }
        public IEnumerable<T> message { get; set; }
        public int numRecords { get; set; }
    }
    public class FireEyeResponse<T> {
        public bool Success { get; set; }
        public T message { get; set; }
        public int numRecords { get; set; }
        [JsonProperty("user_name")]
        public string Username { get; set; }
    }

    public class FireEyeReportIndexMessage {
        public string reportId { get; set; }
        public string title { get; set; }
        public IEnumerable<string> ThreatScape { get; set; }
        public IEnumerable<string> audience { get; set; }
        public int publishDate { get; set; }
        public string version { get; set; }
        public int version1PublishDate { get; set; }
        public string intelligenceType { get; set; }
        public string reportType { get; set; }
        public string reportLink { get; set; }
        public string webLink { get; set; }
    }

    public class FireEyeTMHIndexMessage {
        public string reportId { get; set; }
        public string title { get; set; }
        public string[] ThreatScape { get; set; }
        public int publishDate { get; set; }
        public string version { get; set; }
        public string intelligenceType { get; set; }
        public string reportLink { get; set; }
        public string webLink { get; set; }
    }

    public class FireEyeReportMessage<T> {
        public T Report { get; set; }
        public override string ToString () { return $"Report={Report.ToString()}"; }
    }

    public class FireEyeAPI
    {
        private static string SERVER = "api.isightpartners.com";

        private static string _APIKey;
        private static string _secret;
        private static string _APIVersion;
        private static WebProxy _proxy;

        public FireEyeAPI(string APIKey, string secret, string APIVersion = "2.6", WebProxy proxy = default)
        {
            _APIKey = APIKey;
            _secret = secret;
            _APIVersion = APIVersion;
            _proxy = proxy;
        }

        public static string ByteToString(byte[] buff)
        {
            string sbinary = "";

            for (int i = 0; i < buff.Length; i++)
            {
                sbinary += buff[i].ToString("X2"); // hex format
            }
            return (sbinary);
        }

        private static void SetHeaders(string end_point, HttpWebRequest request, string accept = "application/json")
        {
            // TimeZone tz = TimeZone.CurrentTimeZone;
            string rfc822 = (DateTime.UtcNow).ToString("r");
            // string offset = tz.GetUtcOffset(DateTime.Now).ToString();
            // if (!offset.StartsWith("-"))
            //     offset = "+" + offset;
            // offset = offset.Substring(0, 6);
            // offset = offset.Replace(":", "");
            // rfc822 = rfc822.Replace("GMT", offset);
            string accept_version = _APIVersion;
            request.Accept = accept;

            string new_data = end_point + accept_version + accept + rfc822;

            var encoding = new System.Text.ASCIIEncoding();
            byte[] keyByte = encoding.GetBytes(_secret);
            byte[] messageBytes = encoding.GetBytes(new_data);
            string hash = "";
            using (var hmacsha256 = new System.Security.Cryptography.HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                hash = ByteToString(hashmessage).ToLower();
            }

            StringBuilder headers = new StringBuilder();
            request.Headers.Add("X-Auth: " + _APIKey);
            request.Headers.Add("X-Auth-Hash: " + hash);
            request.Headers.Add("Accept-Version: " + accept_version);

            // Console.WriteLine(request.Headers.Get("Date"));

            string date_header = request.Headers.Get("Date");
            MethodInfo priMethod = request.Headers.GetType().GetMethod("AddWithoutValidate", BindingFlags.Instance | BindingFlags.NonPublic);
            priMethod.Invoke(request.Headers, new[] { "Date", rfc822 });

            // Console.WriteLine(end_point);
            // Console.WriteLine(request.Headers.Get("Date"));
            // Console.WriteLine(request.Headers.Get("X-Auth"));
            // Console.WriteLine(request.Headers.Get("X-Auth-Hash"));
            // Console.WriteLine(request.Headers.Get("Accept-Version"));
        }

        public IEnumerable<FireEyeReportIndexMessage> ReportIndex(ReportParameters parameters
        )
        {
            var query = new Dictionary<string, string>();

            if (parameters.since != default)
                query.Add("since", WebUtility.UrlEncode(((DateTimeOffset)parameters.since).ToUnixTimeSeconds().ToString()));

            if (parameters.sinceReport != default)
                query.Add("sinceReport", WebUtility.UrlEncode(parameters.sinceReport));

            if (parameters.sinceReportVersion != default)
                query.Add("sinceReportVersion", WebUtility.UrlEncode(parameters.sinceReportVersion));

            if (parameters.threatScape != default)
                query.Add("threatScape", WebUtility.UrlEncode(parameters.threatScape));

            if (parameters.pubType != default)
                query.Add("pubType", WebUtility.UrlEncode(parameters.pubType));

            if (parameters.intelligenceType != default)
                query.Add("intelligenceType", WebUtility.UrlEncode(parameters.intelligenceType));

            if (parameters.intelligenceType != default)
                query.Add("intelligenceType", WebUtility.UrlEncode(parameters.intelligenceType));

            if (parameters.limit != 1000)
                query.Add("limit", WebUtility.UrlEncode(parameters.limit.ToString()));

            if (parameters.offset > 0)
                query.Add("offset", WebUtility.UrlEncode(parameters.offset.ToString()));

            if (parameters.startDate != default)
                query.Add("startDate", WebUtility.UrlEncode(((DateTimeOffset)parameters.startDate).ToUnixTimeSeconds().ToString()));

            if (parameters.endDate != default)
                query.Add("endDate", WebUtility.UrlEncode(((DateTimeOffset)parameters.endDate).ToUnixTimeSeconds().ToString()));

            if (parameters.audience != default)
                query.Add("audience", WebUtility.UrlEncode(parameters.audience));

            if (parameters.reportType != default)
                query.Add("reportType", WebUtility.UrlEncode(parameters.reportType));

            if (parameters.sortBy != default)
                query.Add("sortBy", WebUtility.UrlEncode(parameters.sortBy));

            string resultData = GetJSON(query, "report/index", out HttpStatusCode code);
            System.Console.WriteLine("--- " + code);
            Console.WriteLine(resultData);
            System.Console.WriteLine("---");

            if (string.IsNullOrEmpty(resultData))
            {
                return Enumerable.Empty<FireEyeReportIndexMessage>();
            }

            // convert result to JSON
            var resultingJson = JsonConvert.DeserializeObject<FireEyeResponses<FireEyeReportIndexMessage>>(resultData);
            if (resultingJson.success)
            {
                return resultingJson.message;   
            }
            else 
            {
                return Enumerable.Empty<FireEyeReportIndexMessage>();
            }
        }

        public IEnumerable<FireEyeTMHIndexMessage> TMHReportIndex(
            ThreatMediaHighlightParameters parameters
        )
        {
            var query = new Dictionary<string, string>();

            if (parameters.since != default)
                query.Add("since", WebUtility.UrlEncode(((DateTimeOffset)parameters.since).ToUnixTimeSeconds().ToString()));

            if (parameters.limit != 1000)
                query.Add("limit", WebUtility.UrlEncode(parameters.limit.ToString()));

            if (parameters.offset > 0)
                query.Add("offset", WebUtility.UrlEncode(parameters.offset.ToString()));

            if (parameters.startDate != default)
                query.Add("startDate", WebUtility.UrlEncode(((DateTimeOffset)parameters.startDate).ToUnixTimeSeconds().ToString()));

            if (parameters.endDate != default)
                query.Add("endDate", WebUtility.UrlEncode(((DateTimeOffset)parameters.endDate).ToUnixTimeSeconds().ToString()));

            if (parameters.sortBy != default)
                query.Add("sortBy", WebUtility.UrlEncode(parameters.sortBy));

            string resultData = GetJSON(query, "tmh/index", out HttpStatusCode code);
            // System.Console.WriteLine("--- " + code);
            // Console.WriteLine(resultData);
            // System.Console.WriteLine("---");

            if (string.IsNullOrEmpty(resultData))
            {
                return Enumerable.Empty<FireEyeTMHIndexMessage>();
            }

            // convert result to JSON
            var resultingJson = JsonConvert.DeserializeObject<FireEyeResponses<FireEyeTMHIndexMessage>>(resultData);
            if (resultingJson.success)
                return resultingJson.message;
            else return Enumerable.Empty<FireEyeTMHIndexMessage>();
        }

        public FireEyeTMHReport tmh(
            string reportdId)
        {
            var query = new Dictionary<string, string>();

            string resultData = GetJSON(query, $"tmh/{WebUtility.UrlEncode(reportdId)}", out HttpStatusCode code);
            System.Console.WriteLine("--- " + code);
            Console.WriteLine(resultData);
            System.Console.WriteLine("---");

            // convert result to JSON
            var o = JsonConvert.DeserializeObject<FireEyeResponse<FireEyeReportMessage<FireEyeTMHReport>>>(resultData);
            // System.Console.WriteLine("--- ");
            // Console.WriteLine(o.Success);
            // Console.WriteLine(o.Username);
            // Console.WriteLine(o.message);
            // System.Console.WriteLine("---");

            return o.message.Report;
        }

        public void tmhDownload(
            string reportdId,
            Stream stream,
            AcceptedFormat format = AcceptedFormat.PDF)
        {
            var query = new Dictionary<string, string>();

            GetFile(query, $"tmh/{WebUtility.UrlEncode(reportdId)}", stream, format, out HttpStatusCode code);
        }

        public FireEyeReport Report(
            string reportdId,
            string detail = default,
            bool? noTags = default,
            bool? iocsOnly = default)
        {
            var query = new Dictionary<string, string>();
            if (detail != default)
                query.Add("detail", WebUtility.UrlEncode(detail));
            if (noTags != default)
                query.Add("noTags", WebUtility.UrlEncode((bool)noTags ? "true" : "false"));
            if (iocsOnly != default)
                query.Add("iocsOnly", WebUtility.UrlEncode((bool)iocsOnly ? "true" : "false"));

            string resultData = GetJSON(query, $"report/{WebUtility.UrlEncode(reportdId)}", out HttpStatusCode code);
            // System.Console.WriteLine("--- " + code);
            // Console.WriteLine(resultData);
            // System.Console.WriteLine("---");

            // convert result to JSON
            var o = JsonConvert.DeserializeObject<FireEyeResponse<FireEyeReportMessage<FireEyeReport>>>(resultData);
            return o?.message.Report;
        }

        public void ReportDownload(
            string reportdId,
            Stream stream,
            string detail = default,
            bool? noTags = default,
            bool? iocsOnly = default,
            AcceptedFormat format = AcceptedFormat.PDF)
        {
            var query = new Dictionary<string, string>();
            if (detail != default)
                query.Add("detail", WebUtility.UrlEncode(detail));
            if (noTags != default)
                query.Add("noTags", WebUtility.UrlEncode((bool)noTags ? "true" : "false"));
            if (iocsOnly != default)
                query.Add("iocsOnly", WebUtility.UrlEncode((bool)iocsOnly ? "true" : "false"));

            GetFile(query, $"report/{WebUtility.UrlEncode(reportdId)}", stream, format, out HttpStatusCode code);
        }

        private static string GetJSON(Dictionary<string, string> query, string endpointString, out HttpStatusCode httpStatusCode)
        {
            var stream = new MemoryStream();
            GetFile(query, endpointString, stream, AcceptedFormat.JSON, out httpStatusCode);

            stream.Position = 0;
            StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
            string resultData = reader.ReadToEnd();
            return resultData;
        }

        public enum AcceptedFormat {
            JSON, PDF, XML, HTML, STIX, CSV, SNORT, ZIP
        }

        private static void GetFile(Dictionary<string, string> query, string endpointString, Stream stream, AcceptedFormat format, out HttpStatusCode httpStatusCode)
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
            HttpWebRequest request = BuildRequest(query, endpointString, mime);

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

        private static HttpWebRequest BuildRequest(Dictionary<string, string> query, string endpointString, string accept)
        {
            string queryString = string.Join("&", query.Select(_ => _.Key + "=" + _.Value));
            string end_point = endpointString + (query.Count > 0 ? "?" + queryString : "");
            string url = "https://" + SERVER + "/" + end_point;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            SetHeaders("/" + end_point, request, accept);
            if (_proxy != null)
                request.Proxy = _proxy;
            Console.WriteLine(url);
            return request;
        }
    }

    public class ThreatMediaHighlightParameters
    {
        public DateTime? since { get; set; }
        public int limit { get; set; } = 1000;
        public int offset { get; set; } = 0;
        public DateTime? startDate { get; set; }
        public DateTime? endDate { get; set; }
        public string sortBy { get; set; }
    }
}