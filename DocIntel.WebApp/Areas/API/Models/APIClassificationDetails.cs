/* DocIntel
 * Copyright (C) 2018-2021 Belgian Defense, Antoine Cailliau
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;

namespace DocIntel.WebApp.Areas.API.Models;

/// <summary>
/// A classification
/// </summary>
public class APIClassification
{
    /// <summary>
    /// The title
    /// </summary>
    /// <example>Unclassified</example>
    [Required]
    public string Title { get; set; }
        
    /// <summary>
    /// The subtitle
    /// </summary>
    /// <example>Don't share outside company</example>
    public string Subtitle { get; set; }
        
    /// <summary>
    /// The abbreviated form
    /// </summary>
    /// <example>U</example>
    public string Abbreviation { get; set; }
        
    /// <summary>
    /// The background color of the classification banner
    /// </summary>
    /// <example>info-bg-500</example>
    public string Color { get; set; }
        
    /// <summary>
    /// The description
    /// </summary>
    /// <example>Indicates that the information cannot be disclosed outside the company.</example>
    public string Description { get; set; }
        
    /// <summary>
    /// The identifier of the parent classification
    /// </summary>
    /// <example>9e1d962e-7155-45e9-af02-40ee6356ccd6</example>
    [JsonProperty("parent_classification_id")]
    public Guid? ParentClassificationId { get; set; }
        
    /// <summary>
    /// Whether the classification is the default one.
    /// </summary>
    /// <example>true</example>
    public bool? Default { get; set; } = false;
}
    
public class APIClassificationDetails : APIClassification
{
    /// <summary>
    /// The classification identifier
    /// </summary>
    /// <example>f0a8ebb6-dcad-45ac-a0c9-1bc0f15b22c3</example>
    [JsonProperty("classification_id")]
    [SwaggerSchema(ReadOnly = true)]
    public Guid ClassificationId { get; set; }
    
    [JsonProperty("parent_classification")]
    [SwaggerSchema("The parent classification", ReadOnly = true)]
    public APIClassification ParentClassification { get; set; }
}