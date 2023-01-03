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

using System;
using System.Collections.Generic;

using DocIntel.Core.Models;

namespace DocIntel.Core.Utils.Search.Documents
{
    public class VerticalResult<T>
    {
        public VerticalResult(T value, int count)
        {
            Value = value;
            Count = count;
        }

        public T Value { get; set; }
        public int Count { get; set; }
    }

    public class HierarchicalVerticalResult<T1, T2>
    {
        public HierarchicalVerticalResult(T1 value, int count)
        {
            Value = value;
            Count = count;
            Elements = new List<VerticalResult<T2>>();
        }

        public T1 Value { get; set; }
        public int Count { get; set; }
        public List<VerticalResult<T2>> Elements { get; set; }
    }

    public class SearchResults
    {
        public SearchResults()
        {
            TotalHits = 0;
            Hits = new List<SearchHit>();
            FacetRegistrants = new List<VerticalResult<string>>();
            Classifications = new List<VerticalResult<Guid>>();
            Sources = new List<VerticalResult<Guid>>();
            Reliabilities = new List<VerticalResult<SourceReliability>>();
        }

        public long TotalHits { get; internal set; }
        public List<SearchHit> Hits { get; internal set; }

        public ICollection<KeyValuePair<string, int>> FacetTags { get; internal set; }

        public List<VerticalResult<Guid>> Classifications { get; internal set; }

        public List<VerticalResult<SourceReliability>> Reliabilities { get; internal set; }

        public List<VerticalResult<Guid>> Sources { get; internal set; }
        public List<VerticalResult<string>> FacetRegistrants { get; internal set; }
    }
}