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
using System.Collections.Generic;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;

namespace DocIntel.WebApp.Areas.API.Models;

public class ApiFacetDetails : ApiFacet
{
    
    /// <summary>
    /// The creation date
    /// </summary>
    [JsonProperty("creation_date")]
    public DateTime CreationDate { get; set; }
        
    /// <summary>
    /// The modification date
    /// </summary>
    [JsonProperty("modification_date")]
    public DateTime ModificationDate { get; set; }

    /// <summary>
    /// The user who created the tag
    /// </summary>
    [JsonProperty("created_by")]
    public APIAppUser CreatedBy { get; set; }
        
    /// <summary>
    /// The user who last modified the tag
    /// </summary>
    [JsonProperty("last_modified_by")]
    public APIAppUser LastModifiedBy { get; set; }
    
    /// <summary>
    /// The tags
    /// </summary>
    public List<APITag> Tags { get; set; }
}