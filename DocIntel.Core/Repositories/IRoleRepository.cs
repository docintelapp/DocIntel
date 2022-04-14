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
using System.Threading.Tasks;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories.Query;

namespace DocIntel.Core.Repositories
{
    /// <summary>
    ///     Represents a repository for roles.
    /// </summary>
    public interface IRoleRepository
    {
        /// <summary>
        ///     Add the role to the database
        /// </summary>
        /// <param name="role">The role</param>
        /// <returns>
        ///     <c>True</c> if the role was added, <c>False</c> otherwise.
        /// </returns>
        Task<AppRole> AddAsync(AmbientContext ambientContext, AppRole role);

        /// <summary>
        ///     Removes the role from the database
        /// </summary>
        /// <param name="role">The role</param>
        /// <returns>
        ///     <c>True</c> if the role was added, <c>False</c> otherwise.
        /// </returns>
        Task RemoveAsync(AmbientContext ambientContext, string roleId);

        /// <summary>
        ///     Updates the role in the database
        /// </summary>
        /// <param name="role">The role</param>
        /// <returns>
        ///     <c>True</c> if the role was updated, <c>False</c> otherwise.
        /// </returns>
        Task<AppRole> UpdateAsync(AmbientContext ambientContext, AppRole role);

        /// <summary>
        ///     Returns whether the role is in the database
        /// </summary>
        /// <param name="roleName">The role</param>
        /// <returns>
        ///     <c>True</c> if the role exists, <c>False</c> otherwise.
        /// </returns>
        Task<bool> Exists(AmbientContext ambientContext, string roleName);

        /// <summary>
        ///     Returns all the users that belong to the specified role
        /// </summary>
        /// <param name="role">The role</param>
        /// <returns>The users</returns>
        IAsyncEnumerable<AppUser> GetUsersForRoleAsync(AmbientContext ambientContext,
            string roleId,
            string[] includeRelatedData = null);

        /// <summary>
        ///     Returns all roles
        /// </summary>
        /// <returns>The roles</returns>
        IAsyncEnumerable<AppRole> GetAllAsync(AmbientContext ambientContext,
            RoleQuery query = null,
            string[] includeRelatedData = null);

        /// <summary>
        ///     Returns the role matching the specified name.
        /// </summary>
        /// <param name="roleName">The name</param>
        /// <returns>The role</returns>
        Task<AppRole> GetByNameAsync(AmbientContext ambientContext,
            string roleName,
            string[] includeRelatedData = null);

        /// <summary>
        ///     Returns the role matching the specified identifier.
        /// </summary>
        /// <param name="id">The identifier</param>
        /// <returns>The role</returns>
        Task<AppRole> GetAsync(AmbientContext ambientContext,
            string id,
            string[] includeRelatedData = null);

        /// <summary>
        ///     Adds the specified user to the specified role
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="role">Role</param>
        Task AddUserRoleAsync(AmbientContext ambientContext,
            string userId,
            string roleId);

        /// <summary>
        ///     Removes the specified user form the specified role
        /// </summary>
        /// <param name="user">The user to remove</param>
        /// <param name="role">The role to remove to the user</param>
        Task RemoveUserRoleAsync(AmbientContext ambientContext,
            string userId,
            string roleId);
    }
}