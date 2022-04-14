using Newtonsoft.Json;

namespace DocIntel.Integrations.ThreatMatch
{
    public class ReportType
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("slug")] public string Slug { get; set; }
        [JsonProperty("acronym")] public string Acronym { get; set; }
    }
}