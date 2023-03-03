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
    ///     Represents a repository for groups.
    /// </summary>
    public interface IGroupRepository
    {
        /// <summary>
        ///     Add the group to the database
        /// </summary>
        /// <param name="group">The group</param>
        /// <returns>
        ///     <c>True</c> if the group was added, <c>False</c> otherwise.
        /// </returns>
        Task AddAsync(AmbientContext ambientContext, Group group);

        /// <summary>
        ///     Removes the group from the database
        /// </summary>
        /// <param name="group">The group</param>
        /// <returns>
        ///     <c>True</c> if the group was added, <c>False</c> otherwise.
        /// </returns>
        Task RemoveAsync(AmbientContext ambientContext, Guid groupId);

        /// <summary>
        ///     Updates the group in the database
        /// </summary>
        /// <param name="group">The group</param>
        /// <returns>
        ///     <c>True</c> if the group was updated, <c>False</c> otherwise.
        /// </returns>
        Task UpdateAsync(AmbientContext ambientContext, Group group);

        /// <summary>
        ///     Returns whether the group is in the database
        /// </summary>
        /// <param name="groupId">The group</param>
        /// <returns>
        ///     <c>True</c> if the group exists, <c>False</c> otherwise.
        /// </returns>
        Task<bool> Exists(AmbientContext ambientContext, Guid groupId);

        /// <summary>
        ///     Returns all groups
        /// </summary>
        /// <returns>The groups</returns>
        IAsyncEnumerable<Group> GetAllAsync(AmbientContext ambientContext,
            GroupQuery query = null,
            string[] includeRelatedData = null);

        /// <summary>
        ///     Returns the group matching the specified identifier.
        /// </summary>
        /// <param name="id">The identifier</param>
        /// <returns>The group</returns>
        Task<Group> GetAsync(AmbientContext ambientContext,
            Guid id,
            string[] includeRelatedData = null);

        /// <summary>
        ///     Adds the specified user to the specified group
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="group">group</param>
        Task AddUserToGroupAsync(AmbientContext ambientContext,
            string userId,
            Guid groupId);

        /// <summary>
        ///     Removes the specified user form the specified group
        /// </summary>
        /// <param name="user">The user to remove</param>
        /// <param name="group">The group to remove to the user</param>
        Task RemoveUserFromGroupAsync(AmbientContext ambientContext,
            string userId,
            Guid groupId);

        /// <summary>
        /// Returns the default groups
        /// </summary>
        /// <param name="ambientContext"></param>
        /// <returns>The groups</returns>
        IEnumerable<Group> GetDefaultGroups(AmbientContext ambientContext);
    }
}