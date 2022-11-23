using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;

namespace DocIntel.WebApp.Areas.API.Models;

public class APIGroup
{
    /// <summary>
    /// The group identifier
    /// </summary>
    [JsonPropertyName("group_id")]
    [SwaggerSchema(ReadOnly = true)]
    public Guid GroupId { get; set; }
    
    /// <summary>
    /// The name
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// The description
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// Whether the group is a default group
    /// </summary>
    public bool Default { get; set; }
    
    /// <summary>
    /// Whether the group is hidden
    /// </summary>
    public bool Hidden { get; set; }
    
    /// <summary>
    /// The parent group, if any.
    /// </summary>
    public Guid? ParentGroupId { get; set; }
}