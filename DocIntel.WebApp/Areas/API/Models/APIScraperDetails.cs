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
using Newtonsoft.Json.Linq;

namespace DocIntel.WebApp.Areas.API.Models
{
    public class APIScraper
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Enabled { get; set; }
        public JObject Settings { get; set; }
        public Guid ReferenceClass { get; set; }
        public bool OverrideSource { get; set; }
        public Guid? SourceId { get; set; }
        public bool SkipInbox { get; set; }
        public int Position { get; set; }
        public bool OverrideClassification { get; set; }
        public Guid? ClassificationId { get; set; }
        public List<Guid> ReleasableToId { get; set; }
        public List<Guid> EyesOnlyId { get; set; }
        public bool OverrideReleasableTo { get; set; }
        public bool OverrideEyesOnly { get; set; }
    }
    
    public class APIScraperDetails
    {
        public Guid ScraperId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Enabled { get; set; }
        public JObject Settings { get; set; }
        public Guid ReferenceClass { get; set; }
        public bool OverrideSource { get; set; }
        public Guid? SourceId { get; set; }
        public ApiSource Source { get; set; }
        public bool SkipInbox { get; set; }
        public int Position { get; set; }
        public bool OverrideClassification { get; set; }
        public APIClassification Classification { get; set; }
        public Guid? ClassificationId { get; set; }
        public ICollection<APIGroup> ReleasableTo { get; set; }
        public ICollection<APIGroup> EyesOnly { get; set; }
        public bool OverrideReleasableTo { get; set; }
        public bool OverrideEyesOnly { get; set; }
    }
}