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

        /// <summary>
        ///     Initializes the user repository
        /// </summary>
        /// <param name="userManager">The user manager</param>
        public UserRepository(
            IAppAuthorizationService appAuthorizationService,
            IPublishEndpoint busClient, ILogger<UserRepository> logger)
        {
            _appAuthorizationService = appAuthorizationService;
            _busClient = busClient;
            _logger = logger;
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

            if (query.Ids != null && query.Ids.Any())
                users = users.AsQueryable().Where(_ => query.Ids.Contains(_.Id));
            
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