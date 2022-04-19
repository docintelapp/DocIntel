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

using System.Collections.Generic;

using DocIntel.Core.Models;

namespace DocIntel.WebApp.ViewModels.DocumentViewModel
{
    public class InboxViewModel
    {
        public IEnumerable<Document> Documents { get; set; }
        public int DocumentCount { get; set; }
        public int Page { get; set; }
        public int PageCount { get; set; }
        public IEnumerable<Document> OwnDocuments { get; internal set; }
        public IEnumerable<SubmittedDocument> SubmittedDocuments { get; set; }
        public AppUser RegisteredBy { get; set; }
    }
}