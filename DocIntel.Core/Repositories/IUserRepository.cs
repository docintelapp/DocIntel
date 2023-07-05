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
using DocIntel.Core.Repositories.Query;

namespace DocIntel.Core.Repositories
{
    /// <summary>
    ///     Represents a repository for users.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        ///     Returns the user matching the specified username.
        /// </summary>
        /// <param name="userName">The username</param>
        /// <returns>
        ///     The user, <c>null</c> if the user could not be found.
        /// </returns>
        Task<AppUser> GetByUserName(AmbientContext ambientContext,
            string userName,
            string[] includeRelatedData = null);

        /// <summary>
        ///     Returns the user matching the specified identifier.
        /// </summary>
        /// <param name="id">The identifier</param>
        /// <returns>
        ///     The user, <c>null</c> if the user could not be found.
        /// </returns>
        Task<AppUser> GetById(AmbientContext ambientContext,
            string id,
            string[] includeRelatedData = null);

        IAsyncEnumerable<AppUser> GetById(AmbientContext ambientContext,
            IEnumerable<string> id,
            string[] includeRelatedData = null);

        /// <summary>
        ///     Updates the user in the repository.
        /// </summary>
        /// <param name="ambientContext"></param>
        /// <param name="user">The user</param>
        /// <param name="groups"></param>
        /// <returns>
        ///     <c>true</c> if the user was successfully updated,
        ///     <c>false</c> otherwise.
        /// </returns>
        Task Update(AmbientContext ambientContext,
            AppUser user, Group[] groups);

        Task<int> CountAsync(AmbientContext ambientContext);

        /// <summary>
        ///     Returns whether a user exists with the specified username
        /// </summary>
        /// <param name="username">The username</param>
        /// <returns>
        ///     <c>true</c> if the user exists, <c>false</c> otherwise.
        /// </returns>
        Task<bool> Exists(AmbientContext ambientContext,
            string username);

        /// <summary>
        ///     Returns the users subscribed to the daily newsletter.
        /// </summary>
        /// <returns>
        ///     The list of users subscribed to the daily newsletter.
        /// </returns>
        IAsyncEnumerable<AppUser> GetUsersForNewsletter(AmbientContext ambientContext);

        /// <summary>
        ///     Returns all users
        /// </summary>
        /// <returns>The users</returns>
        IAsyncEnumerable<AppUser> GetAllAsync(AmbientContext ambientContext,
            UserQuery query = null,
            string[] includeRelatedData = null);

        Task AddAPIKeyAsync(AmbientContext ambientContext,
            APIKey apiKey);

        /// <summary>
        ///     Validates the provided API Key against the specificed user.
        /// </summary>
        /// <param name="ambientContext">Database context</param>
        /// <param name="username">Username</param>
        /// <param name="apikey">API Key</param>
        /// <param name="includeRelatedData">Additional Data to return</param>
        /// <returns>The user matching the username</returns>
        Task<APIKey> ValidateAPIKey(AmbientContext ambientContext,
            string username,
            string apikey,
            string[] includeRelatedData = null);

        Task<APIKey> GetApiKey(AmbientContext ambientContext, Guid id, string[] includeRelatedData = null);
        Task DeleteApiKey(AmbientContext ambientContext, Guid id);
    }
}