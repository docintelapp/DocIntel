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

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace DocIntel.Core.Repositories.EFCore
{
    public class ClassificationEFRepository : IClassificationRepository
    {
        private readonly IAppAuthorizationService _appAuthorizationService;
        private readonly IPublishEndpoint _busClient;
        private readonly IUserClaimsPrincipalFactory<AppUser> _userClaimsPrincipalFactory;

        public ClassificationEFRepository(IPublishEndpoint busClient,
            IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory,
            IAppAuthorizationService appAuthorizationService)
        {
            _busClient = busClient;
            _appAuthorizationService = appAuthorizationService;
            _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
        }

        public async Task<Classification> AddAsync(AmbientContext ambientContext, Classification classification)
        {
            if (!await _appAuthorizationService.CanAddClassification(ambientContext.Claims, classification))
                throw new UnauthorizedOperationException();

            if (IsValid(classification, out var modelErrors))
            {
                if (ambientContext.DatabaseContext.Classifications.AsQueryable().Any())
                {
                    if (classification.Default)
                    // If more than one classification, ensure that we only have one default classification 
                    {
                        foreach (var c in ambientContext.DatabaseContext.Classifications)
                        {
                            c.Default = false;
                        }
                    }
                }
                else
                {
                    // If it is the first classification encoded, force the default flag.
                    classification.Default = true;
                }
                var trackingEntity = await ambientContext.DatabaseContext.AddAsync(classification);
                ambientContext.DatabaseContext.OnSaveCompleteTasks.Add(
                    () => _busClient.Publish(new ClassificationCreatedMessage
                    {
                        ClassificationId = trackingEntity.Entity.ClassificationId,
                        UserId = ambientContext.CurrentUser.Id
                    })
                );
                return trackingEntity.Entity;
            }

            throw new InvalidArgumentException(modelErrors);
        }

        public async Task<bool> Exists(AmbientContext ambientContext, Guid classificationId)
        {
            IQueryable<Classification> enumerable = ambientContext.DatabaseContext.Classifications;

            var classification = enumerable.SingleOrDefault(_ => _.ClassificationId == classificationId);
            if (classification == null) throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanViewClassification(ambientContext.Claims, classification))
                throw new UnauthorizedOperationException();

            return true;
        }

        public async IAsyncEnumerable<Classification> GetAllAsync(AmbientContext ambientContext,
            string[] includeRelatedData = null)
        {
            IQueryable<Classification> enumerable = ambientContext.DatabaseContext.Classifications;
            if (includeRelatedData != default)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            foreach (var classification in enumerable)
                if (await _appAuthorizationService.CanViewClassification(ambientContext.Claims, classification))
                    yield return classification;
        }

        public async Task<Classification> GetAsync(AmbientContext ambientContext, Guid classificationId,
            string[] includeRelatedData = null)
        {
            IQueryable<Classification> enumerable = ambientContext.DatabaseContext.Classifications;
            if (includeRelatedData != default)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            var classification = enumerable.SingleOrDefault(_ => _.ClassificationId == classificationId);
            if (classification == null) throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanViewClassification(ambientContext.Claims, classification))
                throw new UnauthorizedOperationException();

            return classification;
        }

        public Classification GetDefault(AmbientContext ambientContext)
        {
            // TODO What if the user is not able to see the default classification?
            return ambientContext.DatabaseContext.Classifications.SingleOrDefault(_ => _.Default);
        }

        public async Task RemoveAsync(AmbientContext ambientContext, Guid classificationId)
        {
            // TODO Removing a classification should have a fallback value or delete all elements with that classification
            // TODO What about the children classification?
            
            var classification =
                ambientContext.DatabaseContext.Classifications.SingleOrDefault(_ =>
                    _.ClassificationId == classificationId);
            if (classification == null) throw new InvalidArgumentException();
            if (classification.Default) throw new Exception("Cannot remove default classification");

            if (!await _appAuthorizationService.CanDeleteClassification(ambientContext.Claims, classification))
                throw new UnauthorizedOperationException();

            ambientContext.DatabaseContext.Remove(
                classification
            );
            
            ambientContext.DatabaseContext.OnSaveCompleteTasks.Add(
                () => _busClient.Publish(new ClassificationRemovedMessage()
                {
                    ClassificationId = classificationId,
                    UserId = ambientContext.CurrentUser.Id
                })
            );
        }

        public async Task<Classification> UpdateAsync(AmbientContext ambientContext, Classification classification)
        {
            if (!await _appAuthorizationService.CanUpdateClassification(ambientContext.Claims, classification))
                throw new UnauthorizedOperationException();

            if (IsValid(classification, out var modelErrors))
            {
                if (classification.Default)
                    foreach (var c in ambientContext.DatabaseContext.Classifications.AsQueryable().Where(c => c.ClassificationId != classification.ClassificationId))
                    {
                        c.Default = false;
                        ambientContext.DatabaseContext.Update(classification);
                    }
                var trackingEntity = ambientContext.DatabaseContext.Update(classification);
                ambientContext.DatabaseContext.OnSaveCompleteTasks.Add(
                    () => _busClient.Publish(new ClassificationUpdatedMessage
                    {
                        ClassificationId = trackingEntity.Entity.ClassificationId,
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

        private bool IsValid(Classification classification,
            out List<ValidationResult> modelErrors)
        {
            var validationContext = new ValidationContext(classification);
            modelErrors = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(classification,
                validationContext,
                modelErrors);
            return isValid;
        }
    }
}