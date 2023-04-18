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
using DocIntel.Core.Helpers;

namespace DocIntel.Core.Models
{
    public class Group
    {
        public Guid GroupId { get; set; }
        public string Name { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ModificationDate { get; set; }

        public string Description { get; set; }

        public bool Default { get; set; } = false;

        [HelpText("Membership to the group is hidden to other users.")]
        public bool Hidden { get; set; }

        public Guid? ParentGroupId { get; set; }
        public Group ParentGroup { get; set; }

        public ICollection<Member> Members { get; set; }

        public ICollection<Document> DocumentsReleasableTo { get; set; }
        public ICollection<Document> DocumentsEyesOnly { get; set; }
        public ICollection<DocumentFile> FilesReleasableTo { get; set; }
        public ICollection<DocumentFile> FilesEyesOnly { get; set; }
        public ICollection<Importer> ImporterReleasableTo { get; set; }
        public ICollection<Importer> ImporterEyesOnly { get; set; }
        public ICollection<Scraper> ScraperReleasableTo { get; set; }
        public ICollection<Scraper> ScraperEyesOnly { get; set; }
        public ICollection<SubmittedDocument> SubmittedDocumentReleasableTo { get; set; }
        public ICollection<SubmittedDocument> SubmittedDocumentEyesOnly { get; set; }
        public ICollection<Collector> CollectorReleasableTo { get; set; }
        public ICollection<Collector> CollectorEyesOnly { get; set; }

        [Column(TypeName = "jsonb")] public Dictionary<string, JsonObject> MetaData { get; set; }
    }
}