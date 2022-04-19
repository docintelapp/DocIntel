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

using DocIntel.Core.Models;

using SolrNet.Attributes;

namespace DocIntel.Core.Utils.Indexation.SolR
{
    public class IndexedSource
    {
        [SolrUniqueKey("id")] public Guid SourceId { get; set; }

        [SolrField("title")] public string Title { get; set; }

        [SolrField("description")] public string Description { get; set; }

        [SolrField("homepage")] public string HomePage { get; set; }

        [SolrField("keywords")] public IEnumerable<string> Keywords { get; set; }

        [SolrField("creation_date")] public DateTime CreationDate { get; set; }

        [SolrField("modification_date")] public DateTime ModificationDate { get; set; }

        [SolrField("created_by_id")] public string CreatedById { get; set; }

        [SolrField("modified_by_id")] public string LastModifiedById { get; set; }

        public SourceReliability Reliability
        {
            get => (SourceReliability) Enum.ToObject(typeof(SourceReliability), ReliabilityScore);
            set => ReliabilityScore = (int) value;
        }
        [SolrField("reliability")] public int ReliabilityScore { get; set; } 

        [SolrField("country")] public string Country { get; set; }
        
        [SolrField("title_order")] public string TitleOrder { get; set; }
        
        [SolrField("num_docs")] public int NumDocs { get; set; }
        
        [SolrField("last_doc_update")] public DateTime LastDocumentDate { get; set; }
        
        [SolrField("suggest_label")] public string SuggestLabel { get; set; }
    }
}