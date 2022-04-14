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

using DocIntel.Core.Helpers;

namespace DocIntel.Core.Models.STIX
{
    public class Report : BaseObject
    {
        public Report() : base("report")
        {
            Labels = new List<ReportLabel> {ReportLabel.ThreatReport};
        }

        public string Name { get; set; }

        [JsonConverter(typeof(StixDateTimeJsonConverter))]
        public DateTime Published { get; set; }

        public IList<ReportLabel> Labels { get; set; }

        [JsonProperty("object_refs")] public IList<string> ObjectRefs { get; set; }

        public string Description { get; set; }

        
        [JsonProperty("external_references")]
        public IList<ExternalReference> ExternalReferences { get; set; }
    }
}