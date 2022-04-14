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

using DocIntel.Core.Helpers;

using Newtonsoft.Json;

namespace DocIntel.Core.Models.ThreatQuotient
{
    public class Report : BaseObjectRelations
    {
        public Report() : base("Report")
        {
        }

        public string TLP { get; set; }

        public string[] Tags { get; set; }

        [JsonConverter(typeof(JsonConverterKeyValueList))]
        public List<KeyValuePair<string, string>> Attributes { get; set; }

        [JsonProperty("report_url")]
        public string ReportURL { get; set; }

        [JsonProperty("docintel_url")]
        public string DocIntelURL { get; set; }

        [JsonConverter(typeof(ThreatQDateTimeJsonConverter))]
        [JsonProperty("published_at")]
        public DateTime PublishedAt { get; set; }
    }
}