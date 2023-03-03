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
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace DocIntel.WebApp.Areas.API.Models
{
    public class APIRewritingRule
    {
        public int Position { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        [JsonPropertyName("search_pattern")] public string SearchPattern { get; set; }
        public string Replacement { get; set; }
    }
    public class APIRewritingRuleDetails : APIRewritingRule
    {
        [JsonPropertyName("rule_id")] public Guid RuleId { get; set; }
        [JsonPropertyName("rule_set_id")] public Guid RuleSetId { get; set; }
        public APIRewritingRuleSet RuleSet { get; set; }
    }
}