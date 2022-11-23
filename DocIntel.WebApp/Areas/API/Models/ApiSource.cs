using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using DocIntel.Core.Models;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;

namespace DocIntel.WebApp.Areas.API.Models;

public class ApiSource
{   
    /// <summary>
    /// The source identifier
    /// </summary>
    [JsonPropertyName("source_id")]
    [SwaggerSchema(ReadOnly = true)]
    public Guid SourceId { get; set; }
    
    /// <summary>
    /// The title
    /// </summary>
    public string Title { get; set; }
    
    /// <summary>
    /// The description
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// The URL to the homepage
    /// </summary>
    [JsonPropertyName("homepage")]
    public string HomePage { get; set; }
    
    /// <summary>
    /// The URL to the syndication feed
    /// </summary>
    [JsonPropertyName("rss")]
    public string RSSFeed { get; set; }
    
    /// <summary>
    /// The URL to the facebook page
    /// </summary>
    public string Facebook { get; set; }
    
    /// <summary>
    /// The URL to the twitter page
    /// </summary>
    public string Twitter { get; set; }
    
    /// <summary>
    /// The URL to the Reddit page
    /// </summary>
    public string Reddit { get; set; }
    
    /// <summary>
    /// The URL to the LinkedIn page
    /// </summary>
    public string LinkedIn { get; set; }

    /// <summary>
    /// The keywords used for search
    /// </summary>
    public List<string> Keywords { get; set; }
    
    /// <summary>
    /// The assessed reliablity of the source, according to the admiralty scale.
    /// </summary>
    public SourceReliability Reliability { get; set; }

    /// <summary>
    /// The country code, in M49 format.
    /// </summary>
    public string Country { get; set; }
}