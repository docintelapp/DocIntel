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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Helpers;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories.Query;

using MassTransit;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace DocIntel.Core.Repositories.EFCore
{
    /// <summary>
    ///     Represents a repository of users tied to a database, provided via
    ///     <see
    ///         cref="DocIntelContext" />
    ///     and
    ///     <see
    ///         cref="Microsoft.AspNetCore.Identity.UserManager{TUser}" />
    ///     . The users
    ///     are stored in <c>Users</c> property of <c>DocIntelContext</c> while
    ///     roles are stored in <c>Roles</c> property.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly IAppAuthorizationService _appAuthorizationService;

        private readonly IPublishEndpoint _busClient;
        private readonly ILogger<UserRepository> _logger;
        private readonly IUserClaimsPrincipalFactory<AppUser> _userClaimsPrincipalFactory;
        private readonly UserManager<AppUser> _userManager;

        /// <summary>
        ///     Initializes the user repository
        /// </summary>
        /// <param name="userManager">The user manager</param>
        public UserRepository(UserManager<AppUser> userManager,
            IAppAuthorizationService appAuthorizationService,
            IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory,
            IPublishEndpoint busClient, ILogger<UserRepository> logger)
        {
            _userManager = userManager;
            _appAuthorizationService = appAuthorizationService;
            _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
            _busClient = busClient;
            _logger = logger;
        }

        /// <summary>
        ///     Resets the password of the user by generating and directly using a
        ///     new reset token via
        ///     <see
        ///         cref="Microsoft.AspNetCore.Identity.UserManager{TUser}" />
        ///     .
        /// </summary>
        /// <param name="user">The user</param>
        /// <param name="newPassword">The password</param>
        /// <returns>
        ///     <c>True</c> if the password was successfully resets, <c>False</c>
        ///     otherwise.
        /// </returns>
        public async Task<bool> ResetPassword(AmbientContext ambientContext, AppUser user,
            string newPassword)
        {
            var retreivedUser = await ambientContext.DatabaseContext.Users.FindAsync(user.Id);
            if (retreivedUser == null)
                throw new NotFoundEntityException();

            // var claims = await _userClaimsPrincipalFactory.CreateAsync(requestingUser);
            if (!await _appAuthorizationService.CanResetPassword(ambientContext.Claims, retreivedUser))
                throw new UnauthorizedOperationException();

            var resetToken = await _userManager
                .GeneratePasswordResetTokenAsync(user);

            var passwordChangeResult
                = await _userManager.ResetPasswordAsync(user,
                    resetToken,
                    newPassword);
            return passwordChangeResult.Succeeded;
        }

        /// <summary>
        ///     Resets the password via
        ///     <see
        ///         cref="Microsoft.AspNetCore.Identity.UserManager{TUser}" />
        ///     with the
        ///     provided reset token.
        /// </summary>
        /// <param name="user">The user</param>
        /// <param name="resetToken">The reset token</param>
        /// <param name="newPassword">The password</param>
        /// <returns>
        ///     <c>True</c> if the password was successfully reset, <c>False</c>
        ///     otherwise.
        /// </returns>
        public async Task<bool> ResetPassword(AmbientContext ambientContext, AppUser user,
            string resetToken,
            string newPassword)
        {
            var retreivedUser = await ambientContext.DatabaseContext.Users.FindAsync(user.Id);
            if (retreivedUser == null)
                throw new NotFoundEntityException();

            // var claims = await _userClaimsPrincipalFactory.CreateAsync(requestingUser);
            if (!await _appAuthorizationService.CanResetPassword(ambientContext.Claims, retreivedUser))
                throw new UnauthorizedOperationException();

            var passwordChangeResult
                = await _userManager.ResetPasswordAsync(user,
                    resetToken,
                    newPassword);
            return passwordChangeResult.Succeeded;
        }

        /// <summary>
        ///     Change the password by removing the existing password first and
        ///     then adding the new one.
        /// </summary>
        /// <param name="user">The user</param>
        /// <param name="newPassword">The password</param>
        /// <returns>
        ///     <c>True</c> if the password was successfully changed, <c>False</c>
        ///     otherwise.
        /// </returns>
        public async Task<bool> ChangePassword(AmbientContext ambientContext, AppUser user,
            string newPassword)
        {
            var retreivedUser = await ambientContext.DatabaseContext.Users.FindAsync(user.Id);
            if (retreivedUser == null)
                throw new NotFoundEntityException();

            // var claims = await _userClaimsPrincipalFactory.CreateAsync(requestingUser);
            if (!await _appAuthorizationService.CanChangePassword(ambientContext.Claims, retreivedUser))
                throw new UnauthorizedOperationException();

            var passwordRemoveResult
                = await _userManager.RemovePasswordAsync(user);
            if (passwordRemoveResult.Succeeded)
            {
                var passwordChangeResult
                    = await _userManager.AddPasswordAsync(user, newPassword);
                return passwordChangeResult.Succeeded;
            }

            return false;
        }

        public async Task<AppUser> GetByUserName(AmbientContext ambientContext, string userName,
            string[] includeRelatedData = null)
        {
            IQueryable<AppUser> enumerable = ambientContext.DatabaseContext.Users;

            if (includeRelatedData != null)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            var user = enumerable.SingleOrDefault(x => x.NormalizedUserName == userName.ToUpperInvariant());

            if (user == null)
                throw new NotFoundEntityException();

            if (await _appAuthorizationService.CanViewUser(ambientContext.Claims, user))
                return user;
            throw new UnauthorizedOperationException();
        }

        public async Task<AppUser> GetById(AmbientContext ambientContext, string id, string[] includeRelatedData = null)
        {
            IQueryable<AppUser> enumerable = ambientContext.DatabaseContext.Users;

            if (includeRelatedData != null)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            var user = enumerable.SingleOrDefault(x => x.Id == id);

            if (user == null)
                throw new NotFoundEntityException();

            if (await _appAuthorizationService.CanViewUser(ambientContext.Claims, user))
                return user;
            throw new UnauthorizedOperationException();
        }

        public async IAsyncEnumerable<AppUser> GetById(AmbientContext ambientContext, IEnumerable<string> id,
            string[] includeRelatedData = null)
        {
            IQueryable<AppUser> enumerable = ambientContext.DatabaseContext.Users;

            if (includeRelatedData != null)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            var users = enumerable.Where(_ => id.Contains(_.Id));

            if (users == null)
                throw new NotFoundEntityException();

            // var claims = await _userClaimsPrincipalFactory.CreateAsync(requestingUser);

            foreach (var user in users)
                if (await _appAuthorizationService.CanViewUser(ambientContext.Claims, user))
                    yield return user;
                else
                    throw new UnauthorizedOperationException();
        }

        /// <summary>
        ///     Adds the provided user to the database and sets the password to
        ///     the specified one. The password will be hashed according to the
        ///     password hasher provided in
        ///     <see cref="Microsoft.AspNetCore.Identity.UserManager{TUser}" />.
        /// </summary>
        /// <param name="user">The user</param>
        /// <param name="password">The password. If the specified password is null or empty, a random password will be generated.</param>
        /// <returns>
        ///     <c>true</c> if the user was successfully added, <c>false</c>
        ///     otherwise.
        /// </returns>
        public async Task<AppUser> CreateAsync(AmbientContext ambientContext, AppUser user, string password = "", Group[] groups = null)
        {
            if (!await _appAuthorizationService.CanCreateUser(ambientContext.Claims, user))
                throw new UnauthorizedOperationException();

            if (ambientContext.DatabaseContext.Users.Any(
                _ => (!string.IsNullOrEmpty(user.Id) && _.Id == user.Id)
                     | (_.UserName == user.UserName)
                     | (!string.IsNullOrEmpty(user.Email)
                        && _.Email == user.Email)))
                throw new InvalidArgumentException();

            if (IsValid(user, out var modelErrors))
            {
                var utcNow = DateTime.UtcNow;
                user.RegistrationDate = utcNow;

                if (string.IsNullOrEmpty(password))
                {
                    password = UserHelper.GenerateRandomPassword();
                }

                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    var currentUser = await _userManager.FindByNameAsync(user.UserName);
                    await UpdateGroupsAsync(ambientContext, currentUser, groups);
                    return currentUser;
                }

                return null;
            }

            throw new InvalidArgumentException(modelErrors);
        }

        /// <summary>
        ///     Removes the user from the database.
        /// </summary>
        /// <param name="user">The user</param>
        /// <returns>
        ///     <c>true</c> if the user was successfully removed, <c>false</c>
        ///     otherwise.
        /// </returns>
        public async Task Remove(AmbientContext ambientContext, AppUser user)
        {
            var retreivedUser = await ambientContext.DatabaseContext.Users.FindAsync(user.Id);
            if (retreivedUser == null)
                throw new NotFoundEntityException();

            // var claims = await _userClaimsPrincipalFactory.CreateAsync(requestingUser);
            if (!await _appAuthorizationService.CanRemoveUser(ambientContext.Claims, retreivedUser))
                throw new UnauthorizedOperationException();

            ambientContext.DatabaseContext.Remove(retreivedUser);
        }

        /// <summary>
        ///     Updates the user in the database.
        /// </summary>
        /// <param name="ambientContext"></param>
        /// <param name="user">The user</param>
        /// <param name="groups"></param>
        /// <returns>
        ///     <c>true</c> if the user was successfully updated,
        ///     <c>false</c> otherwise.
        /// </returns>
        public async Task Update(AmbientContext ambientContext, AppUser user, Group[] groups)
        {
            var retreivedUser = await ambientContext.DatabaseContext.Users.FindAsync(user.Id);
            if (retreivedUser == null)
                throw new NotFoundEntityException();

            // var claims = await _userClaimsPrincipalFactory.CreateAsync(user);
            if (!await _appAuthorizationService.CanEditUser(ambientContext.Claims, retreivedUser))
                throw new UnauthorizedOperationException();

            await UpdateGroupsAsync(ambientContext, user, groups);

            ambientContext.DatabaseContext.Update(user);
        }

        /// <summary>
        ///     Adds the specified role to the specified user in the database. The
        ///     relationship between the two is represented as a
        ///     <see
        ///         cref="DocIntel.Core.Models.AppUserRole" />
        ///     .
        /// </summary>
        /// <param name="user">The user</param>
        /// <param name="role">The role</param>
        /// <returns>
        ///     <c>true</c> if the role was successfully added, <c>false</c>
        ///     otherwise.
        /// </returns>
        public async Task AddRole(AmbientContext ambientContext, AppUser user,
            AppRole role)
        {
            var retreivedUser = await ambientContext.DatabaseContext.Users.FindAsync(user.Id);
            if (retreivedUser == null)
                throw new NotFoundEntityException();

            var retreivedRole = await ambientContext.DatabaseContext.Roles.FindAsync(role.Id);
            if (retreivedRole == null)
                throw new NotFoundEntityException();

            // var claims = await _userClaimsPrincipalFactory.CreateAsync(requestingUser);
            if (!await _appAuthorizationService.CanAddUserRole(ambientContext.Claims, retreivedUser, retreivedRole))
                throw new UnauthorizedOperationException();

            await ambientContext.DatabaseContext.UserRoles.AddAsync(new AppUserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            });
        }

        /// <summary>
        ///     Removes the specified role from the specified user.
        /// </summary>
        /// <param name="user">The user</param>
        /// <param name="role">The role</param>
        /// <returns>
        ///     <c>true</c> if the role was successfully removed, <c>false</c>
        ///     otherwise.
        /// </returns>
        public async Task RemoveRole(AmbientContext ambientContext, AppUser user, AppRole role)
        {
            var retreivedUser = await ambientContext.DatabaseContext.Users.FindAsync(user.Id);
            if (retreivedUser == null)
                throw new NotFoundEntityException();

            var retreivedRole = await ambientContext.DatabaseContext.Roles.FindAsync(role.Id);
            if (retreivedRole == null)
                throw new NotFoundEntityException();

            // var claims = await _userClaimsPrincipalFactory.CreateAsync(requestingUser);
            if (!await _appAuthorizationService.CanAddUserRole(ambientContext.Claims, retreivedUser, retreivedRole))
                throw new UnauthorizedOperationException();

            var entities = ambientContext.DatabaseContext.UserRoles
                .AsQueryable().Where(_ => (_.UserId == user.Id) & (_.RoleId == role.Id));
            ambientContext.DatabaseContext.UserRoles.RemoveRange(entities);
        }

        /// <summary>
        ///     Returns whether a user matching the specified username exists in
        ///     the database.
        /// </summary>
        /// <param name="username">The username</param>
        /// <returns>
        ///     <c>true</c> if the user exists, <c>false</c>
        ///     otherwise.
        /// </returns>
        public async Task<bool> Exists(AmbientContext ambientContext, string username)
        {
            // var claims = await _userClaimsPrincipalFactory.CreateAsync(requestingUser);
            var user = ambientContext.DatabaseContext.Users.SingleOrDefault(_ =>
                _.NormalizedUserName == username.ToUpperInvariant());
            if (user != null) return await _appAuthorizationService.CanViewUser(ambientContext.Claims, user);

            return false;
        }

        public async IAsyncEnumerable<AppUser> GetAllAsync(AmbientContext ambientContext, UserQuery query = null,
            string[] includeRelatedData = null)
        {
            IQueryable<AppUser> enumerable = ambientContext.DatabaseContext.Users;

            if (includeRelatedData != null)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            var users = BuildQuery(enumerable, query);

            foreach (var user in users)
                if (await _appAuthorizationService.CanViewUser(ambientContext.Claims, user))
                    yield return user;
        }

        public async IAsyncEnumerable<AppUser> GetUsersForNewsletter(AmbientContext ambientContext)
        {
            IQueryable<AppUser> enumerable = ambientContext.DatabaseContext.Users;
            var users = enumerable.Where(_ =>
                _.Preferences.Digest.Frequency == UserPreferences.DigestPreferences.DigestFrequency.Daily);

            if (users == null)
                throw new NotFoundEntityException();

            foreach (var user in users)
                if (await _appAuthorizationService.CanViewUser(ambientContext.Claims, user))
                    yield return user;
                else
                    throw new UnauthorizedOperationException();
        }

        public async Task<int> CountAsync(AmbientContext ambientContext)
        {
            return await ambientContext.DatabaseContext.Users.ToAsyncEnumerable().CountAsync();
        }

        public async Task AddAPIKeyAsync(AmbientContext ambientContext, APIKey apiKey)
        {
            var v = UserHelper.GenerateRandomPassword(new PasswordOptions
            {
                RequiredLength = 64
            });
            apiKey.Key = Convert.ToBase64String(Encoding.UTF8.GetBytes(v));

            apiKey.CreationDate = DateTime.UtcNow;
            apiKey.ModificationDate = apiKey.CreationDate;

            await ambientContext.DatabaseContext.APIKeys.AddAsync(apiKey);
        }

        public Task<APIKey> ValidateAPIKey(AmbientContext ambientContext, string userName, string apikey,
            string[] includeRelatedData = null)
        {
            IQueryable<APIKey> enumerable = ambientContext.DatabaseContext.APIKeys;

            if (includeRelatedData != null)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            var apiKey = enumerable
                .SingleOrDefault(x => (x.User.NormalizedUserName == userName.ToUpperInvariant())
                                      & (x.Key == apikey));

            if (apiKey == null)
                throw new NotFoundEntityException();

            return Task.FromResult(apiKey);
        }

        private async Task UpdateGroupsAsync(AmbientContext ambientContext, AppUser user, Group[] groups)
        {
            var currentGroups = ambientContext.DatabaseContext.Members
                .AsQueryable()
                .Where(_ => _.UserId == user.Id)
                .Select(_ => _.Group)
                .ToHashSet();

            var groupsToAdd = (groups ?? Enumerable.Empty<Group>()).Except(currentGroups, _ => _.GroupId);
            var groupsToRemove = currentGroups.Except(groups ?? Enumerable.Empty<Group>(), _ => _.GroupId);

            _logger.LogWarning("User is member of " + string.Join(",", currentGroups.Select(_ => _.Name)));
            _logger.LogWarning("User will be member of " + string.Join(",", (groups ?? Array.Empty<Group>()).Select(_ => _.Name)));
            _logger.LogWarning("Groups to add " + string.Join(",", groupsToAdd.Select(_ => _.Name)));
            _logger.LogWarning("Groups to remove " + string.Join(",", groupsToRemove.Select(_ => _.Name)));
            
            if (groupsToRemove != null && groupsToRemove.Any())
            {
                var hashset = groupsToRemove.Select(_ => _.GroupId).ToArray();
                _logger.LogDebug("Remove groups: " + string.Join(", ", hashset));
                ambientContext.DatabaseContext.Members.RemoveRange(
                    from dt in ambientContext.DatabaseContext.Members.AsQueryable()
                    where (dt.UserId == user.Id) & hashset.Contains(dt.GroupId)
                    select dt);
            }

            if (groupsToAdd != null && groupsToAdd.Any())
            {
                var hashset = groupsToAdd.Select(_ => _.GroupId).ToArray();
                foreach (var groupToAdd in hashset)
                    if (!ambientContext.DatabaseContext.Members.Any(_ => _.UserId == user.Id
                                                                         && _.GroupId == groupToAdd))
                    {
                        _logger.LogDebug("Add group: " + groupToAdd);
                        await ambientContext.DatabaseContext.Members.AddAsync(new Member
                        {
                            UserId = user.Id,
                            GroupId = groupToAdd
                        });
                    }
            }
        }

        private static IQueryable<AppUser> BuildQuery(IQueryable<AppUser> users,
            UserQuery query)
        {
            if (query == null)
                return users;

            if (query.Page > 1 && query.Limit > 0)
                users = users.Skip((query.Page - 1) * query.Limit);

            if (query.Page <= 1 && query.Limit > 0)
                users = users.Take(query.Limit);

            return users;
        }

        private bool IsValid(AppUser user, out List<ValidationResult> modelErrors)
        {
            var validationContext = new ValidationContext(user);
            modelErrors = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(user,
                validationContext,
                modelErrors);
            return isValid;
        }
    }
}