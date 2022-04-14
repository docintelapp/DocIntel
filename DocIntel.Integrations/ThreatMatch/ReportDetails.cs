using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace DocIntel.Integrations.ThreatMatch
{
    public class ReportDetails
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("status_id")] public int StatusId { get; set; }
        [JsonProperty("type_id")] public int TypeId { get; set; }
        [JsonProperty("title")] public string Title { get; set; }
        [JsonProperty("known_as")] public string KnownAs { get; set; }
        [JsonProperty("slug")] public string Slug { get; set; }
        [JsonProperty("content")] public string Content { get; set; }
        [JsonProperty("published")] public bool Published { get; set; }
        [JsonProperty("publish_now")] public bool PublishNow { get; set; }
        [JsonProperty("published_at")] public DateTime PublishedAt { get; set; }
        [JsonProperty("updated_at")] public DateTime UpdatedAt { get; set; }
        [JsonProperty("author")] public string Author { get; set; }
        [JsonProperty("author_id")] public int AuthorId { get; set; }
        [JsonProperty("relevanceName")] public string RelevanceName { get; set; }
        [JsonProperty("relevanceSlug")] public string RelevanceSlug { get; set; }
        [JsonProperty("text_relevance")] public string TextRelevance { get; set; }
        [JsonProperty("total_comments_count")] public int TotalCommentsCount { get; set; }
        [JsonProperty("related")] public IEnumerable<int> Related { get; set; }
// [JsonProperty("tlp")] public TLP tlp { get; set; }
// [JsonProperty("type")] public ReportType { get; set; }
// [JsonProperty("pages.0.headline"
// [JsonProperty("linked_profiles.0.id"
// [JsonProperty("profile_types.0.id"
    }
}