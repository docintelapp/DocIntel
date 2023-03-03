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
using DocIntel.Core.Utils.Search.Documents;

namespace DocIntel.Core.Repositories.Query
{
    public class DocumentQuery
    {
        public DocumentQuery()
        {
            Statuses = new HashSet<DocumentStatus>(new[] {DocumentStatus.Registered});
            OrderBy = SortCriteria.Relevance;
        }

        public Source Source { get; set; }
        public string SourceUrl { get; set; }
        public IEnumerable<Guid> TagIds { get; set; }
        public ISet<DocumentStatus> Statuses { get; set; }
        public SortCriteria OrderBy { get; set; }
        public DateTime? RegisteredAfter { get; set; }

        public Guid? DocumentId { get; set; }
        public Guid[] DocumentIds { get; set; }
        public int Page { get; set; } = 0;
        public int Limit { get; set; } = 10;
        public string Reference { get; set; }
        public string RegisteredBy { get; set; }
        public string ReferencePrefix { get; set; }
        public string URL { get; set; }
        public string ExternalReference { get; set; }
        public Guid? SourceId { get; set; }
        public Guid[] SourceIds { get; set; }

        public DateTime? ModifiedAfter { get; set; }
        public DateTime? ModifiedBefore { get; set; }

        /// <summary>
        /// Do not return document with a muted tag for the current user.
        /// </summary>
        public bool ExcludeMuted { get; set; }
    }
}