using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace DocIntel.WebApp.Areas.API.Models;

public class APITag
{
    /// <summary>
    /// The tag identifier
    /// </summary>
    [JsonPropertyName("tag_id")]
    public Guid TagId { get; set; }
        
    /// <summary>
    /// The label
    /// </summary>
    public string Label { get; set; }
        
    /// <summary>
    /// The description
    /// </summary>
    public string Description { get; set; }
        
    /// <summary>
    /// The alternative keywords used for search
    /// </summary>
    public IEnumerable<string> Keywords { get; set; }
        
    /// <summary>
    /// The keywords used for extraction
    /// </summary>
    public IEnumerable<string> ExtractionKeywords { get; set; }
        
    /// <summary>
    /// The background color
    /// </summary>
    public string BackgroundColor { get; set; }
        
    /// <summary>
    /// The facet identifier
    /// </summary>
    [JsonPropertyName("facet_id")]
    public Guid? FacetId { get; set; }
        
    /// <summary>
    /// The facet prefix
    /// </summary>
    [JsonPropertyName("facet_prefix")]
    public string? FacetPrefix { get; set; }
        
    /// <summary>
    /// The friendly name
    /// </summary>
    [JsonPropertyName("friendly_name")]
    public string FriendlyName { get; set; }
}