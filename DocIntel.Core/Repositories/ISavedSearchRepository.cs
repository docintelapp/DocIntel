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
using System.Threading.Tasks;
using DocIntel.Core.Models;

namespace DocIntel.Core.Repositories
{
    /// <summary>
    ///     Represents a repository for savedSearchs.
    /// </summary>
    public interface ISavedSearchRepository
    {
        /// <summary>
        ///     Add the savedSearch to the database
        /// </summary>
        /// <param name="savedSearch">The savedSearch</param>
        /// <returns>
        ///     <c>True</c> if the savedSearch was added, <c>False</c> otherwise.
        /// </returns>
        Task<SavedSearch> AddAsync(AmbientContext ambientContext, SavedSearch savedSearch);

        /// <summary>
        ///     Removes the savedSearch from the database
        /// </summary>
        /// <param name="savedSearch">The savedSearch</param>
        /// <returns>
        ///     <c>True</c> if the savedSearch was added, <c>False</c> otherwise.
        /// </returns>
        Task RemoveAsync(AmbientContext ambientContext, Guid savedSearchId);

        /// <summary>
        ///     Updates the savedSearch in the database
        /// </summary>
        /// <param name="savedSearch">The savedSearch</param>
        /// <returns>
        ///     <c>True</c> if the savedSearch was updated, <c>False</c> otherwise.
        /// </returns>
        Task<SavedSearch> UpdateAsync(AmbientContext ambientContext, SavedSearch savedSearch);

        /// <summary>
        ///     Returns whether the savedSearch is in the database
        /// </summary>
        /// <param name="savedSearchId">The savedSearch</param>
        /// <returns>
        ///     <c>True</c> if the savedSearch exists, <c>False</c> otherwise.
        /// </returns>
        Task<bool> Exists(AmbientContext ambientContext, Guid savedSearchId);

        /// <summary>
        ///     Returns all savedSearchs
        /// </summary>
        /// <returns>The savedSearchs</returns>
        IAsyncEnumerable<SavedSearch> GetAllAsync(AmbientContext ambientContext,
            string[] includeRelatedData = null);

        /// <summary>
        ///     Returns the savedSearch matching the specified identifier.
        /// </summary>
        /// <param name="id">The identifier</param>
        /// <returns>The savedSearch</returns>
        Task<SavedSearch> GetAsync(AmbientContext ambientContext,
            Guid id,
            string[] includeRelatedData = null);

        /// <summary>
        /// Returns the default savedSearch for the current user
        /// </summary>
        /// <param name="ambientContext"></param>
        /// <returns>The savedSearch</returns>
        UserSavedSearch GetDefault(AmbientContext ambientContext);


        /// <summary>
        /// Sets the default savedSearch for the current user
        /// </summary>
        /// <param name="ambientContext"></param>
        /// <param name="savedSearch"></param>
        /// <returns>The savedSearch</returns>
        Task<UserSavedSearch> SetDefault(AmbientContext ambientContext, SavedSearch savedSearch);
    }
}