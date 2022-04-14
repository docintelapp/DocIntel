/* DocIntel
 * Copyright (C) 2018-2021 Belgian Defense, Antoine Cailliau, Kevin Menten
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

using System.Collections.Generic;

using DocIntel.Core.Helpers;
using DocIntel.Core.Models;

namespace DocIntel.Core.Utils.Search.Documents
{
    public class DocumentSearchQuery
    {
        public DocumentSearchQuery()
        {
            Page = 1;
            PageSize = 10;
            FacetLimit = 25;
            SortCriteria = SortCriteria.Relevance;
        }

        public string SearchTerms { get; set; }

        public SortCriteria SortCriteria { get; set; }

        public IEnumerable<Tag> Tags { get; set; }

        public IEnumerable<Source> Sources { get; set; }

        public IEnumerable<Classification> SelectedClassifications { get; set; }

        public IEnumerable<AppUser> SelectedRegistrants { get; set; }

        public int FacetLimit { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }

        public Interval<int> FactualScore { get; set; }
        
        public SourceReliability[] SourceReliability { get; set; }
    }
}