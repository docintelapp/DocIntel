/* DocIntel
 * Copyright (C) 2018-2023 Belgian Defense, Antoine Cailliau
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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Nodes;
using Newtonsoft.Json.Linq;

namespace DocIntel.Core.Models
{
    public class TagFacet
    {
        public TagFacet()
        {
            FacetId = Guid.NewGuid();
        }

        [Key] public Guid FacetId { get; set; }

        public string Title { get; set; }

        [RegularExpression(@"([a-zA-Z0-9-\.]*)", ErrorMessage = "Enter only letters, numbers, dashes or dots.")]
        public string Prefix { get; set; }

        [DataType(DataType.MultilineText)] public string Description { get; set; }

        public bool Mandatory { get; set; }

        public bool Hidden { get; set; }

        [DataType(DataType.DateTime)] public DateTime CreationDate { get; set; }

        [DataType(DataType.DateTime)] public DateTime ModificationDate { get; set; }
        public DateTime LastIndexDate { get; set; }

        public string CreatedById { get; set; }

        public AppUser CreatedBy { get; set; }

        public string LastModifiedById { get; set; }

        public AppUser LastModifiedBy { get; set; }

        [Column(TypeName = "jsonb")] public Dictionary<string, JsonObject> MetaData { get; set; }

        public ICollection<UserFacetSubscription> SubscribedUsers { get; set; }

        public ICollection<Tag> Tags { get; set; }
        
        [DisplayName("Automated Extraction")] public bool AutoExtract { get; set; }
        [DisplayName("Extraction Regex")] public string ExtractionRegex { get; set; }
        public string TagNormalization { get; set; }
    }
}