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

using DocIntel.Core.Helpers;

using Newtonsoft.Json;

namespace DocIntel.Core.Models.STIX
{
    public class BaseObject
    {
        protected BaseObject(string type)
        {
            Type = type;
        }

        public string Type { get; }
        public string Id { get; init; }

        [JsonConverter(typeof(StixDateTimeJsonConverter))]
        public DateTime Created { get; set; }

        [JsonConverter(typeof(StixDateTimeJsonConverter))]
        public DateTime Modified { get; set; }
    }
}