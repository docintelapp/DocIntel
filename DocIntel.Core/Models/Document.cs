/*
 * DocIntel
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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Nodes;
using Newtonsoft.Json.Linq;

namespace DocIntel.Core.Models
{
    public class Document
    {
        public Document()
        {
            DocumentId = Guid.NewGuid();
        }

        [Key] public Guid DocumentId { get; set; }

        /// <summary>
        ///     A sequence ID is generated at creation time. The sequence identifier
        ///     shall be set to the highest sequence identifier plus one from all
        ///     documents of the same year/month. The identifier is used to generate
        ///     a user friendly reference.
        /// </summary>
        public int SequenceId { get; set; }

        public string Reference { get; set; }

        [Display(Name = "External Reference")] public string ExternalReference { get; set; }

        public string Title { get; set; }

        public string ShortDescription { get; set; }

        public Guid? SourceId { get; set; }

        public Source Source { get; set; }

        [Display(Name = "Notes")] public string Note { get; set; }

        [Display(Name = "Document Date")]
        [DataType(DataType.Date)]
        public DateTime DocumentDate { get; set; }

        [Display(Name = "Registration Date")]
        [DataType(DataType.Date)]
        public DateTime RegistrationDate { get; set; }

        [Display(Name = "Modification Date")]
        [DataType(DataType.Date)]
        public DateTime ModificationDate { get; set; }

        public AppUser RegisteredBy { get; set; }
        public string RegisteredById { get; set; }
        public AppUser LastModifiedBy { get; set; }
        public string LastModifiedById { get; set; }

        [Display(Name = "Tags")] public ICollection<DocumentTag> DocumentTags { get; } = new List<DocumentTag>();


        public ICollection<Comment> Comments { get; set; }
        public ICollection<UserDocumentSubscription> SubscribedUsers { get; set; }

        public DocumentStatus Status { get; set; }

        public string URL { get; set; }

        public ICollection<DocumentFile> Files { get; set; }

        public Classification Classification { get; set; }
        public Guid ClassificationId { get; set; }

        public ICollection<Group> ReleasableTo { get; set; }
        public ICollection<Group> EyesOnly { get; set; }

        [Column(TypeName = "jsonb")] public Dictionary<string, JsonObject> MetaData { get; set; }

        public Guid? ThumbnailId { get; set; }
        [ForeignKey("ThumbnailId")]
        public DocumentFile Thumbnail { get; set; }

        [Url]
        [Display(Name = "Source URL")] 
        public string SourceUrl { get; set; }

        public DateTime LastIndexDate { get; set; }
    }

    public enum DocumentStatus
    {
        Submitted = 0,
        Analyzed = 1,
        Registered = 2
    }

}