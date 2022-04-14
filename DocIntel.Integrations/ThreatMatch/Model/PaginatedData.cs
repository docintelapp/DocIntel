using System.Collections.Generic;

using Newtonsoft.Json;

namespace DocIntel.Integrations.ThreatMatch.Model
{
    public class PaginatedData<T> {
        [JsonProperty("current_page")] public int CurrentPage { get; set; }
        [JsonProperty("data")] public IEnumerable<T> Data { get; set; }
        [JsonProperty("first_page_url")] public string FirstPageUrl { get; set; }
        [JsonProperty("from")] public int From { get; set; }
        [JsonProperty("last_page")] public int LastPage { get; set; }
        [JsonProperty("last_page_url")] public string LastPageUrl { get; set; }
        [JsonProperty("next_page_url")] public string NextPageUrl { get; set; }
        [JsonProperty("path")] public string Path { get; set; }
        [JsonProperty("per_page")] public int PerPage { get; set; }
        [JsonProperty("prev_page_url")] public string PrevPageUrl { get; set; }
        [JsonProperty("to")] public int To { get; set; }
        [JsonProperty("total")] public int Total { get; set; }
    }
}