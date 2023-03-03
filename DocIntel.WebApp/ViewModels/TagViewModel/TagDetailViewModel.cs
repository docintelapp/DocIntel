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

using System.Collections.Generic;

using DocIntel.Core.Models;

namespace DocIntel.WebApp.ViewModels.TagViewModel
{
    public class TagDetailViewModel : BaseViewModel
    {
        public Tag Tag { get; set; }
        public int Page { get; set; }
        public int PageCount { get; set; }

        public IEnumerable<Document> Documents { get; set; }

        public SubscriptionStatus Subscribed { get; set; }
        public MuteStatus Muted { get; set; }
        public int DocumentCount { get; internal set; }
    }
}