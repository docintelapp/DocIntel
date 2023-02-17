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

using MassTransit;
using Microsoft.EntityFrameworkCore;

using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace DocIntel.Core.Repositories.EFCore
{
    public class SavedSearchEFRepository : ISavedSearchRepository
    {
        private readonly IAppAuthorizationService _appAuthorizationService;
        private readonly IPublishEndpoint _busClient;

        public SavedSearchEFRepository(IPublishEndpoint busClient,
            IAppAuthorizationService appAuthorizationService)
        {
            _busClient = busClient;
            _appAuthorizationService = appAuthorizationService;
        }

        public async Task<SavedSearch> AddAsync(AmbientContext ambientContext, SavedSearch SavedSearch)
        {
            if (!await _appAuthorizationService.CanAddSavedSearch(ambientContext.Claims, SavedSearch))
                throw new UnauthorizedOperationException();

            if (IsValid(SavedSearch, out var modelErrors))
            {
                SavedSearch.CreatedById = ambientContext.CurrentUser.Id;
                SavedSearch.LastModifiedById = ambientContext.CurrentUser.Id;

                SavedSearch.CreationDate = DateTime.UtcNow;
                SavedSearch.ModificationDate = DateTime.UtcNow;


                var trackingEntity = await ambientContext.DatabaseContext.AddAsync(SavedSearch);
                ambientContext.DatabaseContext.OnSaveCompleteTasks.Add(
                    () => _busClient.Publish(new SavedSearchCreatedMessage
                    {
                        SavedSearchId = trackingEntity.Entity.SavedSearchId,
                        UserId = ambientContext.CurrentUser.Id
                    })
                );
                return trackingEntity.Entity;
            }

            throw new InvalidArgumentException(modelErrors);
        }

        public async Task<bool> Exists(AmbientContext ambientContext, Guid SavedSearchId)
        {
            IQueryable<SavedSearch> enumerable = ambientContext.DatabaseContext.SavedSearches;

            var SavedSearch = enumerable.SingleOrDefault(_ => _.SavedSearchId == SavedSearchId);
            if (SavedSearch == null) throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanViewSavedSearch(ambientContext.Claims, SavedSearch))
                throw new UnauthorizedOperationException();

            return true;
        }

        public async IAsyncEnumerable<SavedSearch> GetAllAsync(AmbientContext ambientContext,
            string[] includeRelatedData = null)
        {
            IQueryable<SavedSearch> enumerable = ambientContext.DatabaseContext.SavedSearches;
            if (includeRelatedData != default)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            foreach (var SavedSearch in enumerable)
                if (await _appAuthorizationService.CanViewSavedSearch(ambientContext.Claims, SavedSearch))
                    yield return SavedSearch;
        }

        public async Task<SavedSearch> GetAsync(AmbientContext ambientContext, Guid SavedSearchId,
            string[] includeRelatedData = null)
        {
            IQueryable<SavedSearch> enumerable = ambientContext.DatabaseContext.SavedSearches;
            if (includeRelatedData != default)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            var SavedSearch = enumerable.SingleOrDefault(_ => _.SavedSearchId == SavedSearchId);
            if (SavedSearch == null) throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanViewSavedSearch(ambientContext.Claims, SavedSearch))
                throw new UnauthorizedOperationException();

            return SavedSearch;
        }

        public UserSavedSearch GetDefault(AmbientContext ambientContext)
        {
            return ambientContext.DatabaseContext.UserSavedSearches
                .Include(_ => _.SavedSearch)
                .Include(_ => _.User)
                .SingleOrDefault(_ => _.UserId == ambientContext.CurrentUser.Id && _.Default);
        }

        public async Task<UserSavedSearch> SetDefault(AmbientContext ambientContext, SavedSearch savedSearch)
        {
            savedSearch = await AddAsync(ambientContext, savedSearch);
            var userSavedSearch = new UserSavedSearch()
            {
                SavedSearchId = savedSearch.SavedSearchId,
                Default = true,
                UserId = ambientContext.CurrentUser.Id
            };
            return (await ambientContext.DatabaseContext.UserSavedSearches.AddAsync(userSavedSearch)).Entity;
        }

        public async Task RemoveAsync(AmbientContext ambientContext, Guid SavedSearchId)
        {
            // TODO Removing a SavedSearch should have a fallback value or delete all elements with that SavedSearch
            // TODO What about the children SavedSearch?
            
            var SavedSearch =
                ambientContext.DatabaseContext.SavedSearches.SingleOrDefault(_ =>
                    _.SavedSearchId == SavedSearchId);
            if (SavedSearch == null) throw new InvalidArgumentException();
            
            if (!await _appAuthorizationService.CanDeleteSavedSearch(ambientContext.Claims, SavedSearch))
                throw new UnauthorizedOperationException();

            ambientContext.DatabaseContext.Remove(
                SavedSearch
            );
            
            ambientContext.DatabaseContext.OnSaveCompleteTasks.Add(
                () => _busClient.Publish(new SavedSearchRemovedMessage()
                {
                    SavedSearchId = SavedSearchId,
                    UserId = ambientContext.CurrentUser.Id
                })
            );
        }

        public async Task<SavedSearch> UpdateAsync(AmbientContext ambientContext, SavedSearch SavedSearch)
        {
            if (!await _appAuthorizationService.CanUpdateSavedSearch(ambientContext.Claims, SavedSearch))
                throw new UnauthorizedOperationException();

            if (IsValid(SavedSearch, out var modelErrors))
            {
                SavedSearch.LastModifiedById = ambientContext.CurrentUser.Id;
                SavedSearch.ModificationDate = DateTime.UtcNow;
                
                var trackingEntity = ambientContext.DatabaseContext.Update(SavedSearch);
                ambientContext.DatabaseContext.OnSaveCompleteTasks.Add(
                    () => _busClient.Publish(new SavedSearchUpdatedMessage
                    {
                        SavedSearchId = trackingEntity.Entity.SavedSearchId,
                        UserId = ambientContext.CurrentUser.Id
                    })
                );
                return trackingEntity.Entity;
            }
            else
            {
                throw new InvalidArgumentException(modelErrors);
            }
        }

        private bool IsValid(SavedSearch SavedSearch,
            out List<ValidationResult> modelErrors)
        {
            var validationContext = new ValidationContext(SavedSearch);
            modelErrors = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(SavedSearch,
                validationContext,
                modelErrors);
            return isValid;
        }
    }
}