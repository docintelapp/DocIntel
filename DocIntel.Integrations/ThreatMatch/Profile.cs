using System;

using DocIntel.Integrations.ThreatMatch.Model;

using Newtonsoft.Json;

namespace DocIntel.Integrations.ThreatMatch
{
    public class Profile
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("type_id")] public ProfileTypeEnum Type { get; set; } 
        [JsonProperty("title")] public string Title { get; set; } 
        [JsonProperty("published_updated_at")] public DateTime? PublishedUpdatedAt { get; set; }
        [JsonProperty("published_insignificant_updated_at")] public DateTime? PublishedInsignificantUpdatedAt { get; set; } 
        [JsonProperty("published_at")] public DateTime? PublishedAt { get; set; } 
        [JsonProperty("capability_id")] public int CapabilityId { get; set; } 
    }
}