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

namespace DocIntel.WebApp.Areas.API.Models
{
    public class APIRole
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string[] Permissions { get; set; }
    }
    
    public class APIRoleDetails : APIRole
    {
        [JsonProperty("role_id")]
        public string Id { get; set; }
        public string Slug { get; set; }
        public DateTime CreationDate { get; internal set; }
        public DateTime ModificationDate { get; internal set; }
        public string CreatedById { get; internal set; }
        public APIAppUser CreatedBy { get; set; }
        public string LastModifiedById { get; internal set; }
        public APIAppUser LastModifiedBy { get; set; }
        public IEnumerable<APIAppUser> Users { get; set; }
    }
}