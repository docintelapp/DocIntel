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
    public class Source
    {
        public Source()
        {
            SourceId = Guid.NewGuid();
        }

        [Key] public Guid SourceId { get; set; }

        [Required] public string Title { get; set; }

        [DataType(DataType.MultilineText)] public string Description { get; set; }

        [DisplayName("Home Page")] public string HomePage { get; set; }

        [DisplayName("RSS Feed")] public string RSSFeed { get; set; }

        public string Facebook { get; set; }
        public string Twitter { get; set; }
        public string Reddit { get; set; }
        public string LinkedIn { get; set; }

        public string Keywords { get; set; }

        [DataType(DataType.Date)] public DateTime CreationDate { get; set; }

        [DataType(DataType.Date)] public DateTime ModificationDate { get; set; }

        public AppUser RegisteredBy { get; set; }
        public string RegisteredById { get; set; }
        public AppUser LastModifiedBy { get; set; }
        public string LastModifiedById { get; set; }
        public ICollection<Document> Documents { get; set; }

        public string LogoFilename { get; set; }

        [DefaultValue(SourceReliability.F)] public SourceReliability Reliability { get; set; } = SourceReliability.F;

        public string Country { get; set; }

        [Column(TypeName = "jsonb")]
        public JObject MetaData { get; set; }

        public string URL { get; set; }
    }
}