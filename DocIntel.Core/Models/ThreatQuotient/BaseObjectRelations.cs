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

using System.Collections.Generic;

using Newtonsoft.Json;

namespace DocIntel.Core.Models.ThreatQuotient
{
    public class BaseObjectRelations : BaseObject
    {
        public BaseObjectRelations(string observableType) : base(observableType)
        {
        }

        public BaseObjectRelations(BaseObject bo) : base(bo.Type)
        {
            Value = bo.Value;
            Description = bo.Description;
            Id = bo.Id;
            CreatedAt = bo.CreatedAt;
            UpdatedAt = bo.UpdatedAt;
        }

        public IEnumerable<BaseObject> Adversary { get; set; }

        [JsonProperty("attack_pattern")]
        public IEnumerable<BaseObject> AttackPattern { get; set; }

        public IEnumerable<BaseObject> Campaign { get; set; }

        public IEnumerable<BaseObject> Event { get; set; }

        public IEnumerable<BaseObject> File { get; set; }

        public IEnumerable<BaseObject> Identity { get; set; }

        public IEnumerable<BaseObject> Incident { get; set; }

        public IEnumerable<BaseObject> Indicator { get; set; }

        public IEnumerable<BaseObject> Malware { get; set; }

        public IEnumerable<BaseObject> Vulnerability { get; set; }

        public IEnumerable<BaseObject> Report { get; set; }
    }
}