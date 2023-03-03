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
    ///     Represents a repository for classifications.
    /// </summary>
    public interface IClassificationRepository
    {
        /// <summary>
        ///     Add the classification to the database
        /// </summary>
        /// <param name="classification">The classification</param>
        /// <returns>
        ///     <c>True</c> if the classification was added, <c>False</c> otherwise.
        /// </returns>
        Task<Classification> AddAsync(AmbientContext ambientContext, Classification classification);

        /// <summary>
        ///     Removes the classification from the database
        /// </summary>
        /// <param name="classification">The classification</param>
        /// <returns>
        ///     <c>True</c> if the classification was added, <c>False</c> otherwise.
        /// </returns>
        Task RemoveAsync(AmbientContext ambientContext, Guid classificationId);

        /// <summary>
        ///     Updates the classification in the database
        /// </summary>
        /// <param name="classification">The classification</param>
        /// <returns>
        ///     <c>True</c> if the classification was updated, <c>False</c> otherwise.
        /// </returns>
        Task<Classification> UpdateAsync(AmbientContext ambientContext, Classification classification);

        /// <summary>
        ///     Returns whether the classification is in the database
        /// </summary>
        /// <param name="classificationId">The classification</param>
        /// <returns>
        ///     <c>True</c> if the classification exists, <c>False</c> otherwise.
        /// </returns>
        Task<bool> Exists(AmbientContext ambientContext, Guid classificationId);

        /// <summary>
        ///     Returns all classifications
        /// </summary>
        /// <returns>The classifications</returns>
        IAsyncEnumerable<Classification> GetAllAsync(AmbientContext ambientContext,
            string[] includeRelatedData = null);

        /// <summary>
        ///     Returns the classification matching the specified identifier.
        /// </summary>
        /// <param name="id">The identifier</param>
        /// <returns>The classification</returns>
        Task<Classification> GetAsync(AmbientContext ambientContext,
            Guid id,
            string[] includeRelatedData = null);

        /// <summary>
        /// Returns the default classification for the application
        /// </summary>
        /// <param name="ambientContext"></param>
        /// <returns>The classification</returns>
        Classification GetDefault(AmbientContext ambientContext);
    }
}