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
using System.Threading.Tasks;

using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Messages;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories.Query;

using MassTransit;

using Microsoft.EntityFrameworkCore;

using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;
namespace DocIntel.Core.Repositories.EFCore
{
    public class RoleEFRepository : IRoleRepository
    {
        private readonly IAppAuthorizationService _appAuthorizationService;
        private readonly IPublishEndpoint _busClient;

        public RoleEFRepository(IPublishEndpoint busClient,
            IAppAuthorizationService appAuthorizationService)
        {
            _busClient = busClient;
            _appAuthorizationService = appAuthorizationService;
        }

        public async Task<AppRole> AddAsync(AmbientContext ambientContext,
            AppRole role)
        {
            if (!await _appAuthorizationService.CanCreateRole(ambientContext.Claims, role))
                throw new UnauthorizedOperationException();

            if (IsValid(role, out var modelErrors))
            {
                role.CreationDate = DateTime.Now;
                role.ModificationDate = role.CreationDate;
                role.NormalizedName = role.Name.ToUpperInvariant();

                if (ambientContext.CurrentUser != null)
                {
                    role.CreatedById = ambientContext.CurrentUser.Id;
                    role.LastModifiedById = ambientContext.CurrentUser.Id;
                }

                var trackingEntity = await ambientContext.DatabaseContext.AddAsync(role);
                ambientContext.DatabaseContext.OnSaveCompleteTasks.Add(
                    () => _busClient.Publish(new RoleCreatedMessage
                    {
                        RoleId = trackingEntity.Entity.Id
                    })
                );

                return trackingEntity.Entity;
            }
            else
            {
                throw new InvalidArgumentException(modelErrors);
            }
        }

        public async Task<AppRole> UpdateAsync(AmbientContext ambientContext,
            AppRole role)
        {
            if (!await _appAuthorizationService.CanEditRole(ambientContext.Claims, role))
                throw new UnauthorizedOperationException();

            if (IsValid(role, out var modelErrors))
            {
                role.ModificationDate = role.CreationDate;
                if (ambientContext.CurrentUser != null) role.LastModifiedById = ambientContext.CurrentUser.Id;
                role.NormalizedName = role.Name.ToUpperInvariant();

                var trackingEntity = ambientContext.DatabaseContext.Update(role);
                ambientContext.DatabaseContext.OnSaveCompleteTasks.Add(
                    () => _busClient.Publish(new RoleUpdatedMessage
                    {
                        RoleId = trackingEntity.Entity.Id
                    })
                );
                return trackingEntity.Entity;
            }
            else
            {
                throw new InvalidArgumentException(modelErrors);
            }
        }

        public async Task RemoveAsync(AmbientContext ambientContext,
            string roleId)
        {
            var role = await ambientContext.DatabaseContext.Roles.FindAsync(roleId);
            if (role == null)
                throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanDeleteRole(ambientContext.Claims, role))
                throw new UnauthorizedOperationException();

            var trackingEntity = ambientContext.DatabaseContext.Roles.Remove(role);
            ambientContext.DatabaseContext.OnSaveCompleteTasks.Add(
                () => _busClient.Publish(new RoleRemovedMessage
                {
                    RoleId = trackingEntity.Entity.Id
                })
            );
        }

        public async Task<bool> Exists(AmbientContext ambientContext,
            string roleName)
        {
            if (roleName == null) return false;
            var role = await ambientContext.DatabaseContext.Roles
                .AsQueryable()
                .FirstOrDefaultAsync(_ => _.NormalizedName == roleName.ToUpperInvariant());
            if (role != null)
                return await _appAuthorizationService.CanViewRole(ambientContext.Claims, role);

            return false;
        }

        public async Task AddUserRoleAsync(AmbientContext ambientContext,
            string userId,
            string roleId)
        {
            var user = await ambientContext.DatabaseContext.Users.FindAsync(userId);
            if (user == null)
                throw new NotFoundEntityException();

            var role = await ambientContext.DatabaseContext.Roles.FindAsync(roleId);
            if (role == null)
                throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanAddUserRole(ambientContext.Claims, user, role))
                throw new UnauthorizedOperationException();

            if (ambientContext.DatabaseContext.UserRoles.Any(_ =>
                (_.UserId == user.Id) & (_.RoleId == role.Id)))
                return;

            ambientContext.DatabaseContext.UserRoles.Add(new AppUserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            });
        }

        public async Task RemoveUserRoleAsync(AmbientContext ambientContext,
            string userId,
            string roleId)
        {
            var user = await ambientContext.DatabaseContext.Users.FindAsync(userId);
            if (user == null)
                throw new NotFoundEntityException();

            var role = await ambientContext.DatabaseContext.Roles.FindAsync(roleId);
            if (role == null)
                throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanRemoveUserRole(ambientContext.Claims, user, role))
                throw new UnauthorizedOperationException();

            if (!ambientContext.DatabaseContext.UserRoles.Any(_ =>
                (_.UserId == user.Id) & (_.RoleId == role.Id)))
                return;

            ambientContext.DatabaseContext.UserRoles.RemoveRange(
                ambientContext.DatabaseContext.UserRoles.AsQueryable().Where(x =>
                    (x.RoleId == role.Id) & (x.UserId == user.Id))
            );
        }

        public async IAsyncEnumerable<AppRole> GetAllAsync(AmbientContext ambientContext,
            RoleQuery query = null,
            string[] includeRelatedData = null)
        {
            IQueryable<AppRole> enumerable = ambientContext.DatabaseContext.Roles;

            if (includeRelatedData != default)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            var filteredComments = BuildQuery(enumerable, query);

            foreach (var role in filteredComments)
                if (await _appAuthorizationService.CanViewRole(ambientContext.Claims, role))
                {
                    yield return role;
                }
        }

        public async Task<AppRole> GetAsync(AmbientContext ambientContext,
            string id,
            string[] includeRelatedData = null)
        {
            IQueryable<AppRole> enumerable = ambientContext.DatabaseContext.Roles;

            if (includeRelatedData != default)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            var role = enumerable.SingleOrDefault(_ => _.Id == id);

            if (role == null)
                throw new NotFoundEntityException();

            if (await _appAuthorizationService.CanViewRole(ambientContext.Claims, role))
                return role;
            throw new UnauthorizedOperationException();
        }

        public async Task<AppRole> GetByNameAsync(AmbientContext ambientContext,
            string roleName,
            string[] includeRelatedData = null)
        {
            IQueryable<AppRole> enumerable = ambientContext.DatabaseContext.Roles;

            if (includeRelatedData != default)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            var role = enumerable.SingleOrDefault(_ => _.NormalizedName == roleName.ToUpperInvariant());

            if (role == null)
                throw new NotFoundEntityException();

            if (await _appAuthorizationService.CanViewRole(ambientContext.Claims, role))
                return role;
            throw new UnauthorizedOperationException();
        }

        public async IAsyncEnumerable<AppUser> GetUsersForRoleAsync(AmbientContext ambientContext,
            string roleId,
            string[] includeRelatedData = null)
        {
            var role = await ambientContext.DatabaseContext.Roles.FindAsync(roleId);
            if (role == null)
                throw new NotFoundEntityException();

            var users = ambientContext.DatabaseContext.UserRoles
                .AsQueryable()
                .Where(x => x.RoleId == role.Id)
                .Cast<AppUserRole>()
                .Select(_ => _.User);

            if (includeRelatedData != default)
                foreach (var relatedData in includeRelatedData)
                    users = users.Include(relatedData);
            
            foreach (var user in users)
                if (await _appAuthorizationService.CanViewUserRole(ambientContext.Claims, user, role))
                    yield return user;
        }

        private static IQueryable<AppRole> BuildQuery(IQueryable<AppRole> comments,
            RoleQuery query)
        {
            if (query == null)
                return comments;

            if ((query.Page > 0) & (query.Limit > 0))
                comments = comments.Skip((query.Page - 1) * query.Limit).Take(query.Limit);

            return comments;
        }

        private static bool IsValid(AppRole role,
            out List<ValidationResult> modelErrors)
        {
            var validationContext = new ValidationContext(role);
            modelErrors = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(role,
                validationContext,
                modelErrors);
            return isValid;
        }
    }
}