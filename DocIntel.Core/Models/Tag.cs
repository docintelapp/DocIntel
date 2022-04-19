/*
 * DocIntel
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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json.Linq;

namespace DocIntel.Core.Models
{
    public class Tag
    {
        public Tag()
        {
            TagId = Guid.NewGuid();
            CreationDate = DateTime.Now;
            ModificationDate = DateTime.Now;
        }

        [Key] public Guid TagId { get; set; }

        [Required]
        [RegularExpression("([^/]+)", ErrorMessage = "Labels cannot contains forward slashes.")]
        public string Label { get; set; }

        [DataType(DataType.MultilineText)] public string Description { get; set; }

        public string Keywords { get; set; }
        
        [DisplayName("Extraction Keywords")] public string ExtractionKeywords { get; set; }

        public string BackgroundColor { get; set; }

        public AppUser CreatedBy { get; set; }

        public string CreatedById { get; set; }

        public AppUser LastModifiedBy { get; set; }

        public string LastModifiedById { get; set; }

        [DataType(DataType.DateTime)] public DateTime CreationDate { get; set; }

        [DataType(DataType.DateTime)] public DateTime ModificationDate { get; set; }
        public DateTime LastIndexDate { get; set; }

        public ICollection<DocumentTag> Documents { get; } = new List<DocumentTag>();
        public ICollection<UserTagSubscription> SubscribedUser { get; set; }

        [Required] public Guid FacetId { get; set; }

        public TagFacet Facet { get; set; }

        [NotMapped]
        public string FriendlyName => string.IsNullOrEmpty(Facet?.Prefix) ? Label : Facet.Prefix + ":" + Label;

        [Column(TypeName = "jsonb")] public JObject MetaData { get; set; }

        public string URL { get; set; }
    }
}