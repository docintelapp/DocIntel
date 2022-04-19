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

namespace DocIntel.WebApp.Areas.API.Models
{
    public class APITag
    {
        public Guid TagId { get; set; }

        public string Label { get; set; }

        public string Description { get; set; }

        public IEnumerable<string> Keywords { get; set; }
        public IEnumerable<string> ExtractionKeywords { get; set; }

        public string BackgroundColor { get; set; }

        public APIAppUser CreatedBy { get; set; }
        public APIAppUser LastModifiedBy { get; set; }

        public DateTime CreationDate { get; set; }
        public DateTime ModificationDate { get; set; }

        public APITagFacet Facet { get; set; }

        public string FriendlyName => string.IsNullOrEmpty(Facet?.Prefix) ? Label : Facet.Prefix + ":" + Label;
    }
}