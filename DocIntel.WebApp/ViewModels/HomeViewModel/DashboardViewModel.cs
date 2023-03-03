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

namespace DocIntel.WebApp.ViewModels.HomeViewModel
{
    /// <summary>
    ///     Encapsulates the data required to display the home dashboard of the current user.
    /// </summary>
    public class DashboardViewModel : BaseViewModel
    {
        /// <summary>
        ///     The 15 most recent documents from subscriptions (tags and sources)
        /// </summary>
        /// <value>Documents</value>
        public IEnumerable<Document> NewsFeed { get; internal set; }

        /// <summary>
        ///     The top 10 most recent documents
        /// </summary>
        /// <value>Documents</value>
        public IEnumerable<Document> RecentDocs { get; internal set; }
        
        /// <summary>
        ///     The total number of documents registered in the platform.
        /// </summary>
        /// <value>Number of documents</value>
        public int DocumentCount { get; internal set; }
    }
}