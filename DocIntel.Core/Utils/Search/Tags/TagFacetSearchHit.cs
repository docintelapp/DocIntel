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

namespace DocIntel.Core.Utils.Search.Tags
{
    public class TagFacetSearchHit
    {
        public Guid FacetId { get; set; }

        public float Score { get; internal set; }

        public string Excerpt { get; internal set; }
        public string TitleExcerpt { get; internal set; }
        public override bool Equals(object obj)
        {
            if (obj is TagFacetSearchHit sh)
                return sh.FacetId == FacetId;

            return false;
        }

        public override int GetHashCode()
        {
            return FacetId.GetHashCode();
        }
    }
}