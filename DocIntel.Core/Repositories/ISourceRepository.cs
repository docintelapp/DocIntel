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
using System.Linq;
using System.Threading.Tasks;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories.Query;

namespace DocIntel.Core.Repositories
{
    /// <summary>
    ///     Provides a data abstraction layer for sources. The repository
    ///     is responsible for checking the user rights to perform the requested
    ///     operation, and the validity of provided objects. The repository will
    ///     announce relevant operations on a bus.
    ///     The application shall use the repository and avoid direct calls to
    ///     database or its abstraction in order to prevent leaks that would bypass
    ///     additional checks.
    /// </summary>
    public interface ISourceRepository
    {
        /// <summary>
        ///     Creates a new source in the repository.
        /// </summary>
        /// <param name="source">The source to add to the repository.</param>
        /// <param name="user">The user requesting the addition.</param>
        /// <returns>A task representing the operation.</returns>
        /// <exception cref="UnauthorizedOperationException">
        ///     Thrown when the user does not have the right create a new
        ///     source in the repository.
        /// </exception>
        /// <exception cref="InvalidArgumentException">
        ///     Thrown when the specified source is not valid according to its
        ///     data annotations.
        /// </exception>
        Task<Source> CreateAsync(AmbientContext ambientContext, Source source);

        /// <summary>
        ///     Updates an existing source in the repository.
        /// </summary>
        /// <param name="source">The source to update to the repository.</param>
        /// <param name="user">The user requesting the update.</param>
        /// <returns>A task representing the operation.</returns>
        /// <exception cref="UnauthorizedOperationException">
        ///     Thrown when the user does not have the right update the
        ///     source in the repository.
        /// </exception>
        /// <exception cref="InvalidArgumentException">
        ///     Thrown when the specified source is not valid according to its
        ///     data annotations.
        /// </exception>
        /// <exception cref="NotFoundEntityException">
        ///     Thrown when the specified source is not known in the repository.
        /// </exception>
        Task<Source> UpdateAsync(AmbientContext ambientContext, Source source);

        /// <summary>
        ///     Removes an existing source in the repository.
        /// </summary>
        /// <param name="sourceId">
        ///     The identifier of the source to remove from the
        ///     repository.
        /// </param>
        /// <param name="user">The user requesting the removal.</param>
        /// <returns>A task representing the operation.</returns>
        /// <exception cref="UnauthorizedOperationException">
        ///     Thrown when the user does not have the right remove the
        ///     source in the repository.
        /// </exception>
        /// <exception cref="NotFoundEntityException">
        ///     Thrown when the specified source is not known in the repository.
        /// </exception>
        Task<Source> RemoveAsync(AmbientContext ambientContext, Guid sourceId);

        /// <summary>
        ///     Merges two existing source in the repository.
        /// </summary>
        /// <param name="source1">
        ///     The identifier of the primary source, to be preserved.
        /// </param>
        /// <param name="source2">
        ///     The identifier of the secondary source, to be discarded.
        /// </param>
        /// <param name="user">The user requesting the merge.</param>
        /// <returns>A task representing the operation.</returns>
        /// <exception cref="UnauthorizedOperationException">
        ///     Thrown when the user does not have the right merge the
        ///     sources in the repository.
        /// </exception>
        /// <exception cref="NotFoundEntityException">
        ///     Thrown when a specified source is not known in the repository.
        /// </exception>
        Task MergeAsync(AmbientContext ambientContext, Guid source1, Guid source2);

        /// <summary>
        ///     Returns whether a source that the user has the right to view
        ///     exists in the repository.
        /// </summary>
        /// <param name="sourceId">The identifier of the source.</param>
        /// <param name="user">The user requesting the check.</param>
        /// <returns>
        ///     <c>True</c> if the source exists and the user has the right
        ///     to view, <c>False</c> otherwise.
        /// </returns>
        Task<bool> ExistsAsync(AmbientContext ambientContext, Guid sourceId);

        /// <summary>
        ///     Returns the list of sources that the user has the right to
        ///     view.
        /// </summary>
        /// <param name="ambientContext"></param>
        /// <param name="relatedData"></param>
        /// <param name="includeRelatedData">
        ///     An array of string
        ///     representing the related data to be included in the results
        ///     of the query.
        /// </param>
        /// <param name="page">The requested page.</param>
        /// <param name="limit">The maximum number of items to be returned.</param>
        /// <param name="user">The user requesting the check.</param>
        /// <returns>
        ///     The sources that exist and the user has the right
        ///     to view.
        /// </returns>
        /// <exception cref="UnauthorizedOperationException">
        ///     Thrown when the user does not have the right to list the
        ///     source in the repository.
        /// </exception>
        IAsyncEnumerable<Source> GetAllAsync(AmbientContext ambientContext, object relatedData,
            string[] includeRelatedData = null,
            int page = 0, int limit = 10);

        IAsyncEnumerable<Source> GetAllAsync(AmbientContext ambientContext, SourceQuery query,
            string[] includeRelatedData = null);

        /// <summary>
        ///     Returns the source matching the specified identifier.
        /// </summary>
        /// <param name="sourceId">The identifier of the source.</param>
        /// <param name="user">The user requesting the source.</param>
        /// <returns>
        ///     The source if the source exists and the user has the right
        ///     to view
        /// </returns>
        /// <param name="includeRelatedData">
        ///     An array of string
        ///     representing the related data to be included in the results
        ///     of the query.
        /// </param>
        /// <exception cref="UnauthorizedOperationException">
        ///     Thrown when the user does not have the right view the
        ///     source in the repository.
        /// </exception>
        /// <exception cref="NotFoundEntityException">
        ///     Thrown when the specified source is not known in the repository.
        /// </exception>
        Task<Source> GetAsync(AmbientContext ambientContext, Guid sourceId, string[] includeRelatedData = null);

        Task<Source> GetAsync(AmbientContext ambientContext, SourceQuery query, string[] includeRelatedData = null);

        /// <summary>
        ///     Returns the source matching the specified title.
        /// </summary>
        /// <param name="title">The title of the source.</param>
        /// <param name="user">The user requesting the source.</param>
        /// <returns>
        ///     The source if the source exists and the user has the right
        ///     to view
        /// </returns>
        /// <param name="includeRelatedData">
        ///     An array of string
        ///     representing the related data to be included in the results
        ///     of the query.
        /// </param>
        /// <exception cref="UnauthorizedOperationException">
        ///     Thrown when the user does not have the right view the
        ///     source in the repository.
        /// </exception>
        /// <exception cref="NotFoundEntityException">
        ///     Thrown when the specified source is not known in the repository.
        /// </exception>
        Task<Source> GetAsync(AmbientContext ambientContext, string title, string[] includeRelatedData = null);

        /// <summary>
        ///     Returns the number of sources, visible to the user.
        /// </summary>
        /// <param name="user">The user requesting the source.</param>
        /// <returns>
        ///     The source if the source exists and the user has the right
        ///     to view
        /// </returns>
        /// <exception cref="UnauthorizedOperationException">
        ///     Thrown when the user does not have the right to list the sources
        ///     in the repository.
        /// </exception>
        Task<int> CountAsync(AmbientContext ambientContext);

        /// <summary>
        ///     Subscribe the user to the source.
        /// </summary>
        /// <param name="sourceId">The identifier of the source.</param>
        /// <param name="user">The user to subscribe.</param>
        /// <param name="requestingUser">The user requesting the action.</param>
        /// <returns>A task representing the operation</returns>
        /// <exception cref="UnauthorizedOperationException">
        ///     Thrown when the user does not have the right to subscribe to the source.
        /// </exception>
        /// <exception cref="NotFoundEntityException">
        ///     Thrown when the specified source is not known in the repository.
        /// </exception>
        Task SubscribeAsync(AmbientContext ambientContext, AppUser user, Guid sourceId, bool notification);

        /// <summary>
        ///     Unsubscribe the user to the source.
        /// </summary>
        /// <param name="sourceId">The identifier of the source.</param>
        /// <param name="user">The user to unsubscribe.</param>
        /// <param name="requestingUser">The user requesting the action.</param>
        /// <returns>A task representing the operation</returns>
        /// <exception cref="UnauthorizedOperationException">
        ///     Thrown when the user does not have the right to unsubscribe to the source.
        /// </exception>
        /// <exception cref="NotFoundEntityException">
        ///     Thrown when the specified source is not known in the repository.
        /// </exception>
        Task UnsubscribeAsync(AmbientContext ambientContext, AppUser user, Guid sourceId);

        /// <summary>
        ///     Returns the subscription status for the specified source and user.
        /// </summary>
        /// <param name="sourceId">The identifier of the source.</param>
        /// <param name="user">The user.</param>
        /// <param name="requestingUser">The user requesting the action.</param>
        /// <returns>A task representing the operation</returns>
        /// <exception cref="NotFoundEntityException">
        ///     Thrown when the specified source is not known in the repository.
        /// </exception>
        Task<SubscriptionStatus> IsSubscribedAsync(AmbientContext ambientContext, AppUser user, Guid sourceId);

        
        /// <summary>
        ///     Mute the source for a user.
        /// </summary>
        /// <param name="sourceId">The identifier of the source to mute.</param>
        /// <param name="user">The user.</param>
        /// <param name="requestingUser">The user requesting the action.</param>
        /// <returns>A task representing the operation</returns>
        /// <exception cref="UnauthorizedOperationException">
        ///     Thrown when the user does not have the right to mute the source.
        /// </exception>
        /// <exception cref="NotFoundEntityException">
        ///     Thrown when the specified source is not known in the repository.
        /// </exception>
        Task MuteAsync(AmbientContext ambientContext, AppUser user, Guid sourceId);

        /// <summary>
        ///     Unmute the source for the user.
        /// </summary>
        /// <param name="sourceId">The identifier of the source to mute.</param>
        /// <param name="user">The user.</param>
        /// <param name="requestingUser">The user requesting the action.</param>
        /// <returns>A task representing the operation</returns>
        /// <exception cref="UnauthorizedOperationException">
        ///     Thrown when the user does not have the right to mute the source.
        /// </exception>
        /// <exception cref="NotFoundEntityException">
        ///     Thrown when the specified source is not known in the repository.
        /// </exception>
        Task UnmuteAsync(AmbientContext ambientContext, AppUser user, Guid sourceId);

        /// <summary>
        ///     Returns the mute status for the specified source and user.
        /// </summary>
        /// <param name="sourceId">The identifier of the source.</param>
        /// <param name="user">The user.</param>
        /// <param name="requestingUser">The user requesting the action.</param>
        /// <returns>A task representing the operation</returns>
        /// <exception cref="NotFoundEntityException">
        ///     Thrown when the specified source is not known in the repository.
        /// </exception>
        Task<MuteStatus> IsMutedAsync(AmbientContext ambientContext, AppUser user, Guid sourceId);

        /// <summary>
        ///     Returns the subscriptions to sources for the specified users.
        /// </summary>
        /// <param name="user">The user to unsubscribe.</param>
        /// <param name="requestingUser">The user requesting the action.</param>
        /// <returns>A task representing the operation</returns>
        /// <exception cref="UnauthorizedOperationException">
        ///     Thrown when the user does not have the right to list subscriptions.
        /// </exception>
        IAsyncEnumerable<(Source source, SubscriptionStatus status)> GetSubscriptionsAsync(
            AmbientContext ambientContext, AppUser user, int page = 0, int limit = 10);

        IAsyncEnumerable<Source> GetAllAsync(AmbientContext ambientContext, Func<IQueryable<Source>, IQueryable<Source>> relatedData);
        IAsyncEnumerable<Source> GetMutedSourcesAsync(AmbientContext ambientContext, AppUser user);
    }
}