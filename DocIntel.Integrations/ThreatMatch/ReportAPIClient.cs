using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

using DocIntel.Integrations.ThreatMatch.Model;

using PdfSharp.Pdf.IO;

namespace DocIntel.Integrations.ThreatMatch
{
    public class ReportAPIClient
    {
        private readonly APIClient _client;

        public ReportAPIClient(APIClient client)
        {
            _client = client;
        }

        public IEnumerable<Report> GetReports(DateTime since)
        {
            var request = _client.BuildPostRequest("GET", "reports/all", new
            {
                mode = "extended", date_since = since.ToString("yyyy-MM-dd HH:mm")
            });

            return _client.GetResponseObject<Response<Report>>(request)?.List ?? Enumerable.Empty<Report>();
        }
        
        public IEnumerable<ReportSearchResult> GetReports(ReportFilter filter)
        {
            var request = _client.BuildPostRequest("GET", "reports/refine", new
            {
                dateFrom = filter.DateFrom?.ToString("yyyy-MM-dd"),
                dateTo = filter.DateTo?.ToString("yyyy-MM-dd"),
                keywords = filter.Keywords ?? "",
                relevance = string.Join(",", filter.Relevance?? Enumerable.Empty<Relevance>()),
                sectors = string.Join(",", filter.Sectors?? Enumerable.Empty<int>()),
                status = string.Join(",", (filter.Status?? Enumerable.Empty<ReadStatus>()).Select(_ => ((int) _).ToString())),
                types = string.Join(",", (filter.Types?? Enumerable.Empty<int>()).Select(_ => _.ToString()))
            });

            return _client.GetResponseObject<SingleResponse<PaginatedData<ReportSearchResult>>>(request)?.List?.Data ?? Enumerable.Empty<ReportSearchResult>();
        }

        public ReportDetails GetReport(int id)
        {
            var request = _client.BuildPostRequest("GET", $"reports/{id}/edit");
            var profile = _client.GetResponseObject<SingleResponse<ReportDetails>>(request);
            return profile.Data;
        }
        
        public Stream DownloadReport(int id)
        {
            var request = _client.BuildPostRequest("POST", $"pdf/report/{id}/download", new { password = "temporary" });

            var pdfLocation = _client.GetResponseString(request);
            if (!Uri.IsWellFormedUriString(pdfLocation, UriKind.Absolute))
            {
                Console.WriteLine("Invalid URL");
                return null;
            }
            
            HttpWebRequest pdfRequest = (HttpWebRequest)WebRequest.Create(pdfLocation + "?access_token=" + _client.AccessToken.AccessToken);
            
            HttpStatusCode httpStatusCode;
            try
            {
                
                using WebResponse response = pdfRequest.GetResponse();
                httpStatusCode = ((System.Net.HttpWebResponse) response).StatusCode;
                Console.WriteLine(httpStatusCode.ToString() +  "--");
                Console.WriteLine(response.ContentType);
                using Stream responseStream = response.GetResponseStream();
                var stream = new MemoryStream();

                byte[] buffer = new byte[4096];
                int count = 0;
                do
                {
                    count = responseStream.Read(buffer, 0, buffer.Length);
                    stream.Write(buffer, 0, count);
                } while (count != 0);
                
                // Remove the password protection
                stream.Position = 0;
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                var pdf = PdfReader.Open(stream, "temporary", PdfDocumentOpenMode.Modify);
                
                var unprotectedStream = new MemoryStream();
                pdf.Save(unprotectedStream);
                
                return unprotectedStream;
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

            return null;
        }

        public ReportMetadata GetMetadata()
        {
            var request = _client.BuildPostRequest("GET", $"reports/refine-sidebar-options");
            return _client.GetResponseObject<ReportMetadata>(request);
        }
    }
}