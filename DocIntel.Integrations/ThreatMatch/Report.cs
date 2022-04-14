using System;

using Newtonsoft.Json;

namespace DocIntel.Integrations.ThreatMatch
{
    public class Report
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("type_id")] public int TypeId { get; set; }
        [JsonProperty("title")] public string Title { get; set; }
        [JsonProperty("published_updated_at")] public DateTime? PublishedUpdatedAt { get; set; }
        [JsonProperty("published_insignificant_updated_at")] public DateTime? PublishedInsignificantUpdatedAt { get; set; }
        [JsonProperty("published_at")] public DateTime? PublishedAt { get; set; }
        [JsonProperty("priority_id")] public string PriorityId { get; set; }

    }
}