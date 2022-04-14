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


using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Newtonsoft.Json;

using DocIntel.Core.Helpers;

using Elasticsearch.Net;

using Newtonsoft.Json.Converters;

namespace DocIntel.Core.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    [Serializable]
    [StringEnum]
    public enum PatternType
    {
        [Display(Name = "stix")] [EnumMember(Value = "stix")]
        STIX,

        [Display(Name = "pcre")] [EnumMember(Value = "pcre")]
        PCRE,

        [Display(Name = "sigma")] [EnumMember(Value = "sigma")]
        Sigma,

        [Display(Name = "snort")] [EnumMember(Value = "snort")]
        Snort,

        [Display(Name = "suricata")] [EnumMember(Value = "suricata")]
        Suricata,

        [Display(Name = "yara")] [EnumMember(Value = "yara")]
        Yara
    }
}