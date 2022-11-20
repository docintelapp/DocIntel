using System;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;

namespace DocIntel.WebApp.Areas.API.Models;

public class ApiFacet
{
    /// <summary>
    /// The facet identifier
    /// </summary>
    [JsonProperty("facet_id")]
    [SwaggerSchema(ReadOnly = true)]
    public Guid FacetId { get; set; }
    
    /// <summary>
    /// The title
    /// </summary>
    public string Title { get; set; }
    
    /// <summary>
    /// The prefix
    /// </summary>
    public string Prefix { get; set; }
    
    /// <summary>
    /// The description
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// Whether the facet is mandatory
    /// </summary>
    public bool Mandatory { get; set; }
    
    /// <summary>
    /// Whether the facet is displayed in search results
    /// </summary>
    public bool Hidden { get; set; }
    
    /// <summary>
    /// The regex to use for extracting new tags
    /// </summary>
    [JsonProperty("extraction_regex")]
    public string ExtractionRegex { get; set; }
    
    /// <summary>
    /// Whether the tags should be extracted
    /// </summary>
    [JsonProperty("auto_extract")]
    public bool AutoExtract { get; set; }
    
    /// <summary>
    /// How tags should be normalized
    /// </summary>
    [JsonProperty("tag_normalization")]
    public string TagNormalization { get; set; }
}