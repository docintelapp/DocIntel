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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json.Linq;

namespace DocIntel.Core.Models
{
    /// <summary>
    ///     Represents a role in the application.
    /// </summary>
    public class AppRole : IdentityRole<string>
    {
        /// <summary>
        ///     Initializes a new role.
        /// </summary>
        public AppRole() : base()
        {
        }

        /// <summary>
        ///     Initializes a new role with the specified name.
        /// </summary>
        /// <param name="name">The name of the role</param>
        public AppRole(string name) : base(name)
        {
        }

        /// <summary>
        ///     An identifier for the urls
        /// </summary>
        /// <value>The identifier</value>
        [RegularExpression(@"[a-zA-Z0-9-_]*")]
        public string Slug { get; set; }

        /// <summary>
        ///     A short description of the role and its members
        /// </summary>
        /// <value>Description</value>
        public string Description { get; set; }

        public DateTime CreationDate { get; internal set; }
        public DateTime ModificationDate { get; internal set; }
        
        public string CreatedById { get; set; }
        public AppUser CreatedBy { get; set; }
        public string LastModifiedById { get; set; }
        public AppUser LastModifiedBy { get; set; }
        
        [Column(TypeName = "jsonb")] public Dictionary<string, JsonObject> MetaData { get; set; }
    }
}