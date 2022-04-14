using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

using DocIntel.Integrations.ThreatMatch.Model;

using Newtonsoft.Json;

using PdfSharp.Pdf.IO;

namespace DocIntel.Integrations.ThreatMatch
{
    public class ProfileAPIClient
    {
        private readonly APIClient _client;

        public ProfileAPIClient(APIClient client)
        {
            _client = client;
        }

        public IEnumerable<Profile> GetProfiles(DateTime since)
        {
            var request = _client.BuildPostRequest("GET", "profiles/all", new
            {
                mode = "extended", date_since = since.ToString("yyyy-MM-dd HH:mm")
            });

            return _client.GetResponseObject<Response<Profile>>(request).List;
        }

        public IEnumerable<ProfileSearchResult> GetProfiles(ProfileFilter filter)
        {
            if (filter.DateFrom != null && filter.DateTo == null)
                filter.DateTo = DateTime.Now;
                
            var request = _client.BuildPostRequest("GET", "profiles/refine", new
            {
                capability = string.Join(",", filter.Capability ?? Enumerable.Empty<ProfileCapability>()),
                tags = string.Join(",", filter.Tags ?? Enumerable.Empty<string>()),
                dateFrom = filter.DateFrom?.ToString("yyyy-MM-dd"),
                dateTo = filter.DateTo?.ToString("yyyy-MM-dd"),
                keywords = string.Join(",", filter.Keywords?? Enumerable.Empty<string>()),
                relevance = string.Join(",", filter.Relevance?? Enumerable.Empty<Relevance>()),
                sectors = string.Join(",", filter.Sectors?? Enumerable.Empty<int>()),
                status = string.Join(",", (filter.Status?? Enumerable.Empty<ReadStatus>()).Select(_ => ((int) _).ToString())),
                types = string.Join(",", (filter.Types?? Enumerable.Empty<ProfileTypeEnum>()).Select(_ => ((int) _).ToString()))
            });

            return _client.GetResponseObject<SingleResponse<PaginatedData<ProfileSearchResult>>>(request).List.Data;
        }

        public ProfileDetails GetProfile(int id)
        {
            var request = _client.BuildPostRequest("GET", $"profiles/{id}/edit");
            return _client.GetResponseObject<SingleResponse<ProfileDetails>>(request).Data;
        }

        public ProfileMetadata GetMetadata()
        {
            var request = _client.BuildPostRequest("GET", $"profiles/refine-sidebar-options");
            return _client.GetResponseObject<ProfileMetadata>(request);
        }

        public Stream DownloadProfile(int id)
        {
            var request = _client.BuildPostRequest("POST", $"pdf/profile/{id}/download", new { password = "temporary" });

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
    }

    public class ProfileMetadata
    {
        [JsonProperty("types")] public Response<ProfileType> Types { get; set; }
        [JsonProperty("connector_groups")]  public IEnumerable<ConnectorGroup> connector_groups { get; set; }
        [JsonProperty("sectors")]  public IEnumerable<Sector> sectors { get; set; }
    }

    public class Sector
    {
        [JsonProperty("id")] public int id { get; set; }
        [JsonProperty("name")] public string name { get; set; }
    }

    public class ConnectorGroup
    {
        [JsonProperty("connector_id")] public IEnumerable<string> connector_id { get; set; }
        [JsonProperty("name")] public string name { get; set; }
        [JsonProperty("categorySlug")] public string categorySlug { get; set; }
        [JsonProperty("categoryId")] public string CategoryId { get; set; }

    }

    public class ProfileType
    {
    [JsonProperty("id")] public string id { get; set; }                 
    [JsonProperty("name")] public string name { get; set; }             
    [JsonProperty("slug")] public string slug { get; set; }             
    [JsonProperty("created_at")] public DateTime created_at { get; set; } 
    [JsonProperty("updated_at")] public DateTime updated_at { get; set; } 
    [JsonProperty("stix_type")] public string stix_type { get; set; }   
    [JsonProperty("acronym")] public string acronym { get; set; }       
    }
}