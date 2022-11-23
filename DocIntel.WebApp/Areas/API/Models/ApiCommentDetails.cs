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
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace DocIntel.WebApp.Areas.API.Models;

public class ApiCommentDetails : ApiComment
{
    /// <summary>
    /// The identifier of the comment
    /// </summary>
    /// <example>0125f9ff-d026-48f3-9726-83bbf2c56d24</example>
    [JsonPropertyName("comment_id")]
    public Guid CommentId { get; set; }
    
    /// <summary>
    /// The date of the comment
    /// </summary>
    /// <example>2022-10-31T12:11:29.677902+01:00</example>
    [JsonPropertyName("comment_date")]
    public DateTime CommentDate { get; set; }
    
    /// <summary>
    /// The last modification date of the comment
    /// </summary>
    /// <example>2022-10-31T12:11:29.677902+01:00</example>
    [JsonPropertyName("modification_id")]
    public DateTime ModificationDate { get; set; }
    
    /// <summary>
    /// The author
    /// </summary>
    public APIAppUser Author { get; set; }
    
    /// <summary>
    /// The last modifier
    /// </summary>
    [JsonPropertyName("last_modified_by")]
    public APIAppUser LastModifiedBy { get; set; }
}