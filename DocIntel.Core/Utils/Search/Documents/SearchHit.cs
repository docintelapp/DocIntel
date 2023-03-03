/* DocIntel
 * Copyright (C) 2018-2023 Belgian Defense, Antoine Cailliau, Kevin Menten
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

namespace DocIntel.Core.Utils.Search.Documents
{
    public class SearchHit
    {
        public Guid DocumentId { get; set; }

        public string Reference { get; internal set; }
        
        public string Title { get; internal set; }

        public int Position { get; internal set; }

        public string Excerpt { get; set; }

        public string Description { get; set; }

        public string TitleExcerpt { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is SearchHit sh)
                return sh.Reference == Reference;


            return false;
        }

        public override int GetHashCode()
        {
            return Reference.GetHashCode();
        }
    }
}