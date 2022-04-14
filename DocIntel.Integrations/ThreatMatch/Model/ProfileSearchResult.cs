using System;

using Newtonsoft.Json;

namespace DocIntel.Integrations.ThreatMatch.Model
{
    public class ProfileSearchResult
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("title")] public string Title { get; set; } 
        [JsonProperty("type_id")] public ProfileType? TypeId { get; set; } 
        [JsonProperty("capability_id")] public ProfileCapability Capability { get; set; }
        [JsonProperty("image")] public string Image { get; set; } 
        [JsonProperty("listing_description")] public string ListingDescription { get; set; } 
        [JsonProperty("published_at")] public DateTime? PublishedAt { get; set; }
        [JsonProperty("published_updated_at")] public DateTime? PublishedUpdatedAt { get; set; }
        [JsonProperty("typeName")] public string TypeName { get; set; }
        [JsonProperty("typeSlug")] public string TypeSlug { get; set; } 
        [JsonProperty("relevanceName")] public string RelevanceName { get; set; } 
        [JsonProperty("relevanceSlug")] public string RelevanceSlug { get; set; } 
        [JsonProperty("capabilityName")] public string CapabilityName { get; set; }
        [JsonProperty("author")] public string Author { get; set; } 
        [JsonProperty("text_relevance")] public string TextRelevance { get; set; } 
        [JsonProperty("is_flagged")] public bool IsFlagged { get; set; }
        [JsonProperty("read")] public bool Read { get; set; }
        [JsonProperty("content_label")] public string ContentLabel { get; set; }
    }
}