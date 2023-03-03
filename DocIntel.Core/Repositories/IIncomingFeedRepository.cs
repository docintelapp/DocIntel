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
using System.Linq;
using System.Threading.Tasks;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Models;

namespace DocIntel.Core.Repositories
{
    /// <summary>
    ///     Provides a data abstraction layer for incoming feeds. The repository
    ///     is responsible for checking the user rights to perform the requested
    ///     operation, and the validity of provided objects. The repository will
    ///     announce relevant operations on a bus.
    ///     The application shall use the repository and avoid direct calls to
    ///     database or its abstraction in order to prevent leaks that would bypass
    ///     additional checks.
    /// </summary>
    public interface IIncomingFeedRepository
    {
        /// <summary>
        ///     Creates a new incoming feed in the repository.
        /// </summary>
        /// <param name="feed">The feed to add to the repository.</param>
        /// <param name="user">The user requesting the addition.</param>
        /// <returns>A task representing the operation.</returns>
        /// <exception cref="UnauthorizedOperationException">
        ///     Thrown when the user does not have the right create a new
        ///     incoming feed in the repository.
        /// </exception>
        /// <exception cref="InvalidArgumentException">
        ///     Thrown when the specified feed is not valid according to its
        ///     data annotations.
        /// </exception>
        Task<Importer> CreateAsync(AmbientContext ambientContext, 
            Importer feed);

        /// <summary>
        ///     Updates an existing incoming feed in the repository.
        /// </summary>
        /// <param name="feed">The feed to update to the repository.</param>
        /// <param name="user">The user requesting the update.</param>
        /// <returns>A task representing the operation.</returns>
        /// <exception cref="UnauthorizedOperationException">
        ///     Thrown when the user does not have the right update the
        ///     incoming feed in the repository.
        /// </exception>
        /// <exception cref="InvalidArgumentException">
        ///     Thrown when the specified feed is not valid according to its
        ///     data annotations.
        /// </exception>
        /// <exception cref="NotFoundEntityException">
        ///     Thrown when the specified feed is not known in the repository.
        /// </exception>
        Task<Importer> UpdateAsync(AmbientContext ambientContext, 
            Importer feed);

        /// <summary>
        ///     Removes an existing incoming feed in the repository.
        /// </summary>
        /// <param name="feedId">
        ///     The identifier of the feed to remove from the
        ///     repository.
        /// </param>
        /// <param name="user">The user requesting the removal.</param>
        /// <returns>A task representing the operation.</returns>
        /// <exception cref="UnauthorizedOperationException">
        ///     Thrown when the user does not have the right remove the
        ///     incoming feed in the repository.
        /// </exception>
        /// <exception cref="NotFoundEntityException">
        ///     Thrown when the specified feed is not known in the repository.
        /// </exception>
        Task<Importer> RemoveAsync(AmbientContext ambientContext, 
            Guid feedId);

        /// <summary>
        ///     Returns whether an incoming feed that the user has the right to view
        ///     exists in the repository.
        /// </summary>
        /// <param name="feedId">The identifier of the feed.</param>
        /// <param name="user">The user requesting the check.</param>
        /// <returns>
        ///     <c>True</c> if the feed exists and the user has the right
        ///     to view, <c>False</c> otherwise.
        /// </returns>
        Task<bool> ExistsAsync(AmbientContext ambientContext, 
            Guid feedId);

        /// <summary>
        ///     Returns the feed matching the specified identifier.
        /// </summary>
        /// <param name="feedId">The identifier of the feed.</param>
        /// <param name="user">The user requesting the feed.</param>
        /// <returns>
        ///     The feed if the feed exists and the user has the right
        ///     to view
        /// </returns>
        /// <param name="includeRelatedData">
        ///     An array of string
        ///     representing the related data to be included in the results
        ///     of the query.
        /// </param>
        /// <exception cref="UnauthorizedOperationException">
        ///     Thrown when the user does not have the right view the
        ///     incoming feed in the repository.
        /// </exception>
        /// <exception cref="NotFoundEntityException">
        ///     Thrown when the specified feed is not known in the repository.
        /// </exception>
        Task<Importer> GetAsync(AmbientContext ambientContext, 
            Guid feedId, 
            Func<IQueryable<Importer>,IQueryable<Importer>> includes = null);

        /// <summary>
        ///     Returns the list of incoming feeds that the user has the right to
        ///     view.
        /// </summary>
        /// <param name="user">The user requesting the check.</param>
        /// <param name="includeRelatedData">
        ///     An array of string
        ///     representing the related data to be included in the results
        ///     of the query.
        /// </param>
        /// <returns>
        ///     The feeds that exist and the user has the right
        ///     to view.
        /// </returns>
        /// <exception cref="UnauthorizedOperationException">
        ///     Thrown when the user does not have the right list the
        ///     incoming feed in the repository.
        /// </exception>
        IAsyncEnumerable<Importer> GetAllAsync(AmbientContext ambientContext,
            Func<IQueryable<Importer>,IQueryable<Importer>> query = null,
            Func<IQueryable<Importer>,IQueryable<Importer>> includes = null,
            int page = 0, int limit = 25);
    }
}