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
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Nodes;

namespace DocIntel.Core.Models
{
    public class Classification
    {
        public Guid ClassificationId { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Abbreviation { get; set; }
        public string Color { get; set; }
        public string Description { get; set; }
        public Guid? ParentClassificationId { get; set; }
        public Classification ParentClassification { get; set; }
        public bool Default { get; set; } = false;
        [Column(TypeName = "jsonb")] public Dictionary<string, JsonObject> MetaData { get; set; }
    }
}