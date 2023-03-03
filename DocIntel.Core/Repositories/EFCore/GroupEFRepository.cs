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
using System.Threading.Tasks;
using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Messages;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories.Query;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace DocIntel.Core.Repositories.EFCore
{
    public class GroupEFRepository : IGroupRepository
    {
        private readonly IAppAuthorizationService _appAuthorizationService;
        private readonly IPublishEndpoint _busClient;
        private readonly ILogger<GroupEFRepository> _logger;

        public GroupEFRepository(IPublishEndpoint busClient,
            IAppAuthorizationService appAuthorizationService, ILogger<GroupEFRepository> logger)
        {
            _busClient = busClient;
            _appAuthorizationService = appAuthorizationService;
            _logger = logger;
        }

        public async Task AddAsync(AmbientContext ambientContext, Group group)
        {
            if (!await _appAuthorizationService.CanAddGroup(ambientContext.Claims, group))
                throw new UnauthorizedOperationException();

            if (IsValid(group, out var modelErrors))
            {
                group.CreationDate = DateTime.UtcNow;
                group.ModificationDate = group.CreationDate;

                var trackingEntity = await ambientContext.DatabaseContext.AddAsync(group);
                
                var association = new Member
                {
                    GroupId = trackingEntity.Entity.GroupId,
                    UserId = ambientContext.CurrentUser.Id
                };
                
                var trackingEntityMembership = await ambientContext.DatabaseContext.AddAsync(association);
                
                ambientContext.DatabaseContext.OnSaveCompleteTasks.Add(
                    () => _busClient.Publish(new GroupCreatedMessage
                    {
                        GroupId = trackingEntity.Entity.GroupId,
                        UserId = ambientContext.CurrentUser.Id
                    })
                );
            }
            else
            {
                throw new InvalidArgumentException(modelErrors);
            }
        }

        public async Task AddUserToGroupAsync(AmbientContext ambientContext, string userId, Guid groupId)
        {
            var user = ambientContext.DatabaseContext.Users.SingleOrDefault(_ => _.Id == userId);
            var group = ambientContext.DatabaseContext.Groups.SingleOrDefault(_ => _.GroupId == groupId);
            if (user == null || group == null) throw new InvalidArgumentException();

            if (!await _appAuthorizationService.CanAddGroupMember(ambientContext.Claims, group, user))
                throw new UnauthorizedOperationException();

            if (IsValid(group, out var modelErrors))
            {
                var association = new Member
                {
                    GroupId = group.GroupId,
                    UserId = user.Id
                };

                var trackingEntity = await ambientContext.DatabaseContext.AddAsync(association);
            }
            else
            {
                throw new InvalidArgumentException(modelErrors);
            }
        }

        public async Task<bool> Exists(AmbientContext ambientContext, Guid groupId)
        {
            IQueryable<Group> enumerable = ambientContext.DatabaseContext.Groups;
            enumerable = enumerable.Where(_ => !_.Hidden || _.Members.Any(_ => _.UserId == ambientContext.CurrentUser.Id));
            
            var group = enumerable.SingleOrDefault(_ => _.GroupId == groupId);
            if (group == null) throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanViewGroup(ambientContext.Claims, group))
                throw new UnauthorizedOperationException();

            return true;
        }

        public async IAsyncEnumerable<Group> GetAllAsync(AmbientContext ambientContext, 
            GroupQuery query = null, string[] includeRelatedData = null)
        {
            IQueryable<Group> enumerable = ambientContext.DatabaseContext.Groups;
            if (includeRelatedData != default)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            enumerable = BuildQuery(ambientContext.CurrentUser, enumerable, query);
            
            foreach (var group in enumerable)
            {
                if (await _appAuthorizationService.CanViewGroup(ambientContext.Claims, @group))
                {
                    yield return @group;
                }
                else
                {
                    _logger.LogInformation("Not allowed to see " + group.Name);
                }
            }
        }

        public async Task<Group> GetAsync(AmbientContext ambientContext, Guid groupId,
            string[] includeRelatedData = null)
        {
            IQueryable<Group> enumerable = ambientContext.DatabaseContext.Groups;
            enumerable = enumerable.Where(_ => !_.Hidden || _.Members.Any(_ => _.UserId == ambientContext.CurrentUser.Id));
            if (includeRelatedData != default)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            var group = enumerable.SingleOrDefault(_ => _.GroupId == groupId);
            if (group == null) throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanViewGroup(ambientContext.Claims, group))
                throw new UnauthorizedOperationException();

            return group;
        }

        public async Task RemoveAsync(AmbientContext ambientContext, Guid groupId)
        {
            var group = ambientContext.DatabaseContext.Groups.SingleOrDefault(_ => _.GroupId == groupId);
            if (group == null) throw new InvalidArgumentException();

            if (!await _appAuthorizationService.CanDeleteGroup(ambientContext.Claims, group))
                throw new UnauthorizedOperationException();

            ambientContext.DatabaseContext.Remove(
                group
            );
        }

        public async Task RemoveUserFromGroupAsync(AmbientContext ambientContext, string userId, Guid groupId)
        {
            var user = ambientContext.DatabaseContext.Users.SingleOrDefault(_ => _.Id == userId);
            var group = ambientContext.DatabaseContext.Groups.SingleOrDefault(_ => _.GroupId == groupId);
            if (user == null || group == null) throw new InvalidArgumentException();

            if (!await _appAuthorizationService.CanRemoveGroupMember(ambientContext.Claims, group, user))
                throw new UnauthorizedOperationException();

            ambientContext.DatabaseContext.RemoveRange(
                ambientContext.DatabaseContext.Members.AsQueryable()
                    .Where(_ => _.GroupId == groupId && _.UserId == userId)
            );
        }

        public IEnumerable<Group> GetDefaultGroups(AmbientContext ambientContext)
        {
            return ambientContext.DatabaseContext.Groups.AsQueryable().Where(_ => _.Default);
        }

        public async Task UpdateAsync(AmbientContext ambientContext, Group group)
        {
            if (!await _appAuthorizationService.CanUpdateGroup(ambientContext.Claims, group))
                throw new UnauthorizedOperationException();

            if (IsValid(group, out var modelErrors))
            {
                group.CreationDate = DateTime.UtcNow;
                group.ModificationDate = group.CreationDate;

                var trackingEntity = ambientContext.DatabaseContext.Update(group);
                ambientContext.DatabaseContext.OnSaveCompleteTasks.Add(
                    () => _busClient.Publish(new GroupUpdatedMessage
                    {
                        GroupId = trackingEntity.Entity.GroupId,
                        UserId = ambientContext.CurrentUser.Id
                    })
                );
            }
            else
            {
                throw new InvalidArgumentException(modelErrors);
            }
        }

        private IQueryable<Group> BuildQuery(AppUser currentUser, IQueryable<Group> groups,
            GroupQuery query)
        {
            groups = groups.Where(_ => currentUser.Bot || !_.Hidden || _.Members.Any(__ => __.UserId == currentUser.Id));

            if (query == null)
                return groups;

            if (query.Id != null)
            {
                _logger.LogTrace("Filtering by identifiers");
                groups = groups.Where(_ => query.Id.Contains(_.GroupId));

            }
                
            return groups;
        }

        private bool IsValid(Group group,
            out List<ValidationResult> modelErrors)
        {
            var validationContext = new ValidationContext(group);
            modelErrors = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(group,
                validationContext,
                modelErrors);
            return isValid;
        }
    }
}