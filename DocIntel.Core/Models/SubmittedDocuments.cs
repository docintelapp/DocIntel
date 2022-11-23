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
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Nodes;
using Newtonsoft.Json.Linq;

namespace DocIntel.Core.Models
{
    public class SubmittedDocument
    {
        public Guid SubmittedDocumentId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string[] Tags { get; set; }
        public DateTime SubmissionDate { get; set; }
        public DateTime IngestionDate { get; set; }
        public Document Document { get; set; }
        public Guid? DocumentId  { get; set; }
        public Importer Importer { get; set; }
        public Guid? ImporterId  { get; set; }
        public string URL { get; set; }
        public AppUser Submitter { get; set; }
        public string SubmitterId { get; set; }
        public SubmissionStatus Status { get; set; }
        public int Priority { get; set; }

        public Classification Classification { get; set; }
        public Guid? ClassificationId { get; set; }

        public ICollection<Group> ReleasableTo { get; set; }
        public ICollection<Group> EyesOnly { get; set; }
        public bool OverrideClassification { get; set; }
        public bool OverrideReleasableTo { get; set; }
        public bool OverrideEyesOnly { get; set; }
        public Guid? SourceId { get; set; }
        public bool OverrideSource { get; set; }
        public bool SkipInbox { get; set; }
        
        [Column(TypeName = "jsonb")] public Dictionary<string, JsonObject> MetaData { get; set; }
    }

    public enum SubmissionStatus
    {
        Submitted, Processed, Discarded, Error,
        Duplicate
    }
}