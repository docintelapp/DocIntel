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
using DocIntel.Core.Models;
using Newtonsoft.Json.Linq;

namespace DocIntel.WebApp.Areas.API.Models
{
    public class APIImporter
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ImporterStatus Status { get; set; }
        public TimeSpan CollectionDelay { get; set; }
        public string FetchingUserId { get; set; }
        public JObject Settings { get; set; }
        public Guid ReferenceClass { get; set; }
        public int Limit { get; set; } = 10;
        public int Priority { get; set; }
        public bool OverrideClassification { get; set; }
        public Guid? ClassificationId { get; set; }
        public bool OverrideReleasableTo { get; set; }
        public bool OverrideEyesOnly { get; set; }
        public List<Guid> ReleasableToId { get; set; }
        public List<Guid> EyesOnlyId { get; set; }
    }
    
    public class APIImporterDetails
    {
        public Guid ImporterId { get; init; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ImporterStatus Status { get; set; }
        public TimeSpan CollectionDelay { get; set; }
        public DateTime? LastCollection { get; set; }
        public string FetchingUserId { get; set; }
        public APIAppUser FetchingUser { get; set; }
        public JObject Settings { get; set; }
        public Guid ReferenceClass { get; set; }
        public int Limit { get; set; } = 10;
        public int Priority { get; set; }
        public bool OverrideClassification { get; set; }
        public APIClassification Classification { get; set; }
        public Guid? ClassificationId { get; set; }

        public bool OverrideReleasableTo { get; set; }
        public ICollection<APIGroup> ReleasableTo { get; set; }
        public bool OverrideEyesOnly { get; set; }
        public ICollection<APIGroup> EyesOnly { get; set; }
    }
}