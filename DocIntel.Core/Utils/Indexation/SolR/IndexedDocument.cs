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
using System.ComponentModel;

using DocIntel.Core.Helpers;
using DocIntel.Core.Models;

using SolrNet.Attributes;

namespace DocIntel.Core.Utils.Indexation.SolR
{
    public class IndexedDocument
    {
        [DisplayName("Document identifier")]
        [HelpText("Filter based on the document unique identifier.")]
        [SolrUniqueKey("id")] public Guid DocumentId { get; set; }

        [HelpText("Filter based on the document reference.")]
        [Example("reference:DI-*")]
        [SolrField("reference")] public string Reference { get; set; }

        [DisplayName("External Reference")]
        [HelpText("Filter based on the external reference.")]
        [Example("external_reference:21-00026115")]
        [SolrField("external_reference")] public string ExternalReference { get; set; }

        [HelpText("Filter based on the title.")]
        [Example("title:APT28")]
        [SolrField("title")] public string Title { get; set; }

        [DisplayName("Summary")]
        [HelpText("Filter based on the analyst summary.")]
        [Example("description:\"emissary panda\"")]
        [SolrField("description")] public string ShortDescription { get; set; }

        [DisplayName("Source identifier")]
        [HelpText("Filter based on the source identifier.")]
        [Example("source_id:a1bc72d6-aa00-4418-9d09-ea442c20cea1")]
        [SolrField("source_id")] public Guid? SourceId { get; set; }
        
        public SourceReliability Reliability
        {
            get => (SourceReliability) Enum.ToObject(typeof(SourceReliability), ReliabilityScore);
            set => ReliabilityScore = (int) value;
        }
        
        [DisplayName("Source reliability")]
        [HelpText("Filter based on the source reliability.")]
        [Example("source_reliability:0")]
        [SolrField("source_reliability")] public int ReliabilityScore { get; set; }

        [DisplayName("Source URL")]
        [HelpText("Filter based on the source URL.")]
        [Example("source_url:\"https://securelist.com/russian-speaking-cybercrime-evolution-2016-2021/104656/\"")]
        [SolrField("source_url")] public string SourceUrl { get; set; }

        [HelpText("Filter based on the classification.")]
        [Example("classification:1388be53-11be-429d-b948-7ffa93a86d8d")]
        [SolrField("classification")] public Guid? Classification { get; set; }
        
        [DisplayName("EYES ONLY")]
        [HelpText("Filter based on the EYES ONLY property.")]
        [Example("eyes_only:1388be53-11be-429d-b948-7ffa93a86d8d")]
        [SolrField("eyes_only")] public Guid[] EyesOnly { get; set; }
        
        [DisplayName("RELEASABLE TO")]
        [HelpText("Filter based on the RELEASABLE TO property.")]
        [Example("releasable_to:1388be53-11be-429d-b948-7ffa93a86d8d")]
        [SolrField("releasable_to")] public Guid[] ReleasableTo { get; set; }

        [DisplayName("Registration Date")]
        [HelpText("Filter based on the registration date.")]
        [Example("registration_date:NOW/DAY")]
        [SolrField("registration_date")] public DateTime RegistrationDate { get; set; }
        
        [DisplayName("Modification Date")]
        [HelpText("Filter based on the modification date.")]
        [Example("modification_date:[NOW/DAY-1DAY TO NOW/DAY+7DAYS]")]
        [SolrField("modification_date")] public DateTime ModificationDate { get; set; }
        
        [DisplayName("Document Date")]
        [HelpText("Filter based on the document date.")]
        [Example("document_date:[NOW/DAY-1MONTH TO NOW/DAY+1DAY]")]
        [SolrField("document_date")] public DateTime DocumentDate { get; set; }

        [DisplayName("Registered by")]
        [HelpText("Filter based on the user who registered the document.")]
        [Example("registered_by_id:[NOW/DAY-1MONTH TO NOW/DAY+1DAY]")]
        [SolrField("registered_by_id")] public string RegisteredById { get; set; }
        
        [DisplayName("Status")]
        [HelpText("Filter based on the document status.")]
        [Example("status:0")]
        [SolrField("status")] public DocumentStatus Status { get; set; }
        
        [DisplayName("URL")]
        [HelpText("Filter based on the URL.")]
        [Example("source_url:\"https://example.docintel.org/Document/Details/indicator-report-ursnif-activity-report-dec-9-2021\"")]
        [SolrField("url")] public string URL { get; set; }

        [DisplayName("Tags")]
        [HelpText("Filter based on the document tags.")]
        [Example("tags:apt31")]
        [SolrField("tags")] public IEnumerable<string> Tags { get; set; }

        [DisplayName("Tag Identifiers")]
        [HelpText("Filter based on the document tag identifier.")]
        [Example("tags_id:1388be53-11be-429d-b948-7ffa93a86d8d")]
        [SolrField("tags_id")]
        public IEnumerable<string> TagsId { get; set; }

        [DisplayName("Comments")]
        [HelpText("Filter based on the comments.")]
        [Example("comments:1388be53-11be-429d-b948-7ffa93a86d8d")]
        [SolrField("comments")] public IEnumerable<string> Comments { get; set; }

        [DisplayName("File content")]
        [HelpText("Filter based on the content of the files.")]
        [Example("contents:bitsadmin")]
        [SolrField("contents")] public IEnumerable<string> FileContents { get; set; }

        [DisplayName("Observables")]
        [HelpText("Filter based on the extracted observables.")]
        [Example("observables:8.8.8.8")]
        [SolrField("observables")] public IEnumerable<string> Observables { get; set; }
    }
}