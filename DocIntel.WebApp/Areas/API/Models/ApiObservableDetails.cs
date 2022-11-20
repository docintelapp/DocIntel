using System.Collections.Generic;
using Swashbuckle.AspNetCore.Annotations;

namespace DocIntel.WebApp.Areas.API.Models;


public class ApiObservableDetails
{
    /// <summary>
    /// The observable identifier
    /// </summary>
    [SwaggerSchema(ReadOnly = true)]
    public string Iden { get; set; }
    
    /// <summary>
    /// The observable data form, as in [Synapse Data Model](https://synapse.docs.vertex.link/en/latest/synapse/autodocs/datamodel_forms.html).
    /// </summary>
    public string Type { get; set; }
    
    /// <summary>
    /// The observable value
    /// </summary>
    public string Value { get; set; }
    
    /// <summary>
    /// The tags
    /// </summary>
    public string[] Tags { get; set; }
    
    /// <summary>
    /// The additional properties
    /// </summary>
    public Dictionary<string,object> Properties { get; set; }
}