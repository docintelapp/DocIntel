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
using System.Linq;
using DocIntel.Core.Models;
using DocIntel.Core.Utils.Search.Documents;
using DocIntel.WebApp.ViewModels.Shared;

namespace DocIntel.WebApp.ViewModels.SearchViewModel
{
    public class DocumentSearchResult
    {
        public Document Document { get; set; }
        public string Excerpt { get; set; }
        public float Position { get; set; }
        public string TitleExcerpt { get; internal set; }
    }

    public class SearchIndexViewModel : BaseViewModel
    {
        /// <summary>
        ///     Gets or sets the keywords to search for.
        /// </summary>
        /// <value>The search term.</value>
        public string SearchTerm { get; set; }

        public IEnumerable<DocumentSearchResult> SearchResultDocuments { get; set; }

        public IEnumerable<IGrouping<TagFacet, Tag>> Tags { get; set; }
        public IEnumerable<Tag> SelectedTags { get; set; }

        public IEnumerable<VerticalResult<Classification>> Classifications { get; set; }
        public IEnumerable<Classification> SelectedClassifications { get; set; }

        public IEnumerable<VerticalResult<Source>> Sources { get; set; }
        public IEnumerable<Source> SelectedSources { get; set; }

        public IEnumerable<VerticalResult<AppUser>> Registrants { get; set; }
        public IEnumerable<AppUser> SelectedRegistrants { get; set; }

        public long DocumentCount { get; set; }

        public TimeSpan Elapsed { get; set; }

        public int Page { get; set; }

        public long PageCount { get; set; }

        public int PageSize { get; set; }

        public SortCriteria SortBy { get; set; }

        public int FacetLimit { get; set; }
        public string DidYouMean { get; internal set; }
        public List<VerticalResult<SourceReliability>> Reliabilities { get; internal set; }
        public SourceReliability[] SelectedReliabilities { get; internal set; }
        public int FactualScoreLow { get; internal set; }
        public int FactualScoresHigh { get; internal set; }
    }
}