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
using Ganss.Xss;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace DocIntel.Core.Repositories.EFCore
{
    public class TagFacetEFRepository : ITagFacetRepository
    {
        private readonly IAppAuthorizationService _appAuthorizationService;
        private readonly IPublishEndpoint _busClient;
        private HtmlSanitizer _sanitizer;
        private ILogger<TagFacetEFRepository> _logger;

        public TagFacetEFRepository(IPublishEndpoint busClient,
            IAppAuthorizationService appAuthorizationService, ILogger<TagFacetEFRepository> logger)
        {
            _busClient = busClient;
            _appAuthorizationService = appAuthorizationService;
            _logger = logger;

            _sanitizer = new HtmlSanitizer();
            _sanitizer.AllowedSchemes.Add("data");
        }

        public async Task<TagFacet> AddAsync(AmbientContext ambientContext,
            TagFacet tagFacet)
        {
            if (!await _appAuthorizationService.CanCreateFacetTag(ambientContext.Claims, tagFacet))
                throw new UnauthorizedOperationException();
            
            if (IsValid(tagFacet, out var modelErrors))
            {
                if (ambientContext.DatabaseContext.Facets.Any(_ => _.Prefix == tagFacet.Prefix && _.FacetId != tagFacet.FacetId))
                {
                    modelErrors.Add(new ValidationResult("Prefix already exists", new []{ nameof(TagFacet.Prefix) }));
                    throw new InvalidArgumentException(modelErrors);
                }

                tagFacet.Description = _sanitizer.Sanitize(tagFacet.Description);
                tagFacet.CreationDate = DateTime.UtcNow;
                tagFacet.CreatedById = ambientContext.CurrentUser.Id;
                tagFacet.ModificationDate = tagFacet.CreationDate;
                tagFacet.LastModifiedById = tagFacet.CreatedById;

                if (!new[] { "", "camelize", "capitalize", "downcase", "handleize", "upcase" }.Contains(
                        tagFacet.TagNormalization))
                {
                    tagFacet.TagNormalization = "";
                }
                
                var trackingEntity = await ambientContext.DatabaseContext.AddAsync(tagFacet);

                ambientContext.DatabaseContext.OnSaveCompleteTasks.Add(
                    () =>
                    {
                        _logger.LogDebug("Sending FacetCreatedMessage message");
                        return _busClient.Publish(new FacetCreatedMessage
                        {
                            FacetTagId = trackingEntity.Entity.FacetId,
                            UserId = ambientContext.CurrentUser.Id
                        });
                    });

                return trackingEntity.Entity;
            }

            throw new InvalidArgumentException(modelErrors);
        }

        public async Task<TagFacet> UpdateAsync(AmbientContext ambientContext,
            TagFacet tagFacet)
        {
            var retrievedTag = await ambientContext.DatabaseContext.Facets.FindAsync(tagFacet.FacetId);
            if (retrievedTag == null)
                throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanEditFacetTag(ambientContext.Claims, retrievedTag))
                throw new UnauthorizedOperationException();

            if (IsValid(tagFacet, out var modelErrors))
            {
                if (ambientContext.DatabaseContext.Facets.Any(_ => _.Prefix == tagFacet.Prefix && _.FacetId != tagFacet.FacetId))
                {
                    modelErrors.Add(new ValidationResult("Prefix already exists", new []{ nameof(TagFacet.Prefix) }));
                    throw new InvalidArgumentException(modelErrors);
                }
                
                tagFacet.Description = _sanitizer.Sanitize(tagFacet.Description);
                tagFacet.ModificationDate = DateTime.UtcNow;
                tagFacet.LastModifiedById = ambientContext.CurrentUser.Id;

                if (!new[] { "", "camelize", "capitalize", "downcase", "handleize", "upcase" }.Contains(
                        tagFacet.TagNormalization))
                {
                    tagFacet.TagNormalization = "";
                }

                var trackingEntity = ambientContext.DatabaseContext.Update(tagFacet);

                ambientContext.DatabaseContext.OnSaveCompleteTasks.Add(
                    () =>
                    {
                        _logger.LogDebug("Sending FacetUpdatedMessage message");
                        return _busClient.Publish(new FacetUpdatedMessage
                        {
                            FacetTagId = trackingEntity.Entity.FacetId,
                            UserId = ambientContext.CurrentUser.Id
                        });
                    });

                return trackingEntity.Entity;
            }

            throw new InvalidArgumentException(modelErrors);
        }

        public async Task<TagFacet> RemoveAsync(AmbientContext ambientContext,
            Guid tagFacetId)
        {
            var facet = ambientContext.DatabaseContext.Facets.Include(_ => _.Tags).SingleOrDefault(_ => _.FacetId == tagFacetId);
            if (facet == null)
                throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanDeleteFacetTag(ambientContext.Claims, facet))
                throw new UnauthorizedOperationException();

            var tags = facet.Tags.Select(_ => _.TagId).ToArray();

            var trackingEntity = ambientContext.DatabaseContext.Remove(facet);
            if (trackingEntity.Entity != null)
                ambientContext.DatabaseContext.OnSaveCompleteTasks.Add(
                    () =>
                    {
                        _logger.LogDebug("Sending FacetRemovedMessage message");
                        return _busClient.Publish(new FacetRemovedMessage
                        {
                            FacetTagId = trackingEntity.Entity.FacetId,
                            UserId = ambientContext.CurrentUser.Id,
                            Tags = tags
                        });
                    });

            return trackingEntity.Entity;
        }

        public async Task<bool> ExistsAsync(AmbientContext ambientContext,
            Guid tagFacetId)
        {
            var facet = await ambientContext.DatabaseContext.Facets.FindAsync(tagFacetId);
            if (facet != null) return await _appAuthorizationService.CanViewFacetTag(ambientContext.Claims, facet);

            return false;
        }

        public async IAsyncEnumerable<TagFacet> GetAllAsync(AmbientContext ambientContext,
            FacetQuery query = null,
            string[] includeRelatedData = null)
        {
            IQueryable<TagFacet> enumerable = ambientContext.DatabaseContext.Facets;

            if (includeRelatedData != null)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            var filteredTags = BuildQuery(enumerable, query);

            foreach (var facet in filteredTags)
                if (await _appAuthorizationService.CanViewFacetTag(ambientContext.Claims, facet))
                    yield return facet;
        }

        public async IAsyncEnumerable<TagFacet> GetAllAsync(AmbientContext ambientContext,
            Func<IQueryable<TagFacet>, IQueryable<TagFacet>> query)
        {
            IQueryable<TagFacet> enumerable = ambientContext.DatabaseContext.Facets;

            var filteredTags = query(enumerable);

            foreach (var facet in filteredTags)
                if (await _appAuthorizationService.CanViewFacetTag(ambientContext.Claims, facet))
                    yield return facet;
        }

        public async Task<TagFacet> GetAsync(AmbientContext ambientContext,
            Guid id,
            string[] includeRelatedData = null)
        {
            IQueryable<TagFacet> enumerable = ambientContext.DatabaseContext.Facets;

            if (includeRelatedData != null)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            var facet = enumerable.SingleOrDefault(_ => _.FacetId == id);

            if (facet == null)
                throw new NotFoundEntityException();

            if (await _appAuthorizationService.CanViewFacetTag(ambientContext.Claims, facet))
                return facet;
            throw new UnauthorizedOperationException();
        }

        public async Task<TagFacet> GetAsync(AmbientContext ambientContext,
            string prefix,
            string[] includeRelatedData = null)
        {
            IQueryable<TagFacet> enumerable = ambientContext.DatabaseContext.Facets;

            if (includeRelatedData != null)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            // Silently handles when there are multiple facets with the same prefix, so it can be solved by the user.
            var facet = enumerable.FirstOrDefault(_ =>
                string.IsNullOrEmpty(prefix) ? string.IsNullOrEmpty(_.Prefix) : _.Prefix == prefix);

            if (facet == null)
            {
                throw new NotFoundEntityException();
            }

            if (await _appAuthorizationService.CanViewFacetTag(ambientContext.Claims, facet))
                return facet;
            throw new UnauthorizedOperationException();
        }

        public async Task SubscribeAsync(
            AmbientContext ambientContext, AppUser user,
            Guid facetId,
            bool notification = false)
        {
            var retrievedFacet = await ambientContext.DatabaseContext.Facets.FindAsync(facetId);
            if (retrievedFacet == null) throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanSubscribeToFacetTag(ambientContext.Claims, retrievedFacet))
                throw new UnauthorizedOperationException();

            var subscription = ambientContext.DatabaseContext.UserFacetSubscriptions
                .FirstOrDefault(_ => (_.FacetId == facetId) & (_.UserId == user.Id));

            if (subscription != null)
            {
                subscription.Notify = notification;
                ambientContext.DatabaseContext.UserFacetSubscriptions.Update(subscription);
            }
            else
            {
                subscription = new UserFacetSubscription
                {
                    FacetId = facetId,
                    UserId = user.Id,
                    Notify = notification
                };
                await ambientContext.DatabaseContext.UserFacetSubscriptions.AddAsync(subscription);
            }
        }

        public async Task UnsubscribeAsync(
            AmbientContext ambientContext, AppUser user,
            Guid facetId)
        {
            var retrievedFacet = await ambientContext.DatabaseContext.Facets.FindAsync(facetId);
            if (retrievedFacet == null) throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanSubscribeToFacetTag(ambientContext.Claims, retrievedFacet))
                throw new UnauthorizedOperationException();

            ambientContext.DatabaseContext.UserFacetSubscriptions.RemoveRange(
                ambientContext.DatabaseContext.UserFacetSubscriptions.AsQueryable()
                    .Where(_ => (_.FacetId == facetId) & (_.UserId == user.Id))
            );
        }
        
        public Task<UserFacetSubscription> IsSubscribedAsync(
            AmbientContext ambientContext, AppUser user,
            Guid facetId)
        {
            var subscription = ambientContext.DatabaseContext.UserFacetSubscriptions
                .FirstOrDefault(_ => (_.FacetId == facetId) & (_.UserId == user.Id));

            if (subscription == null)
            {
                return Task.FromResult(new UserFacetSubscription()
                {
                    FacetId = facetId,
                    UserId = user.Id,
                    Subscribed = false,
                    Notify = false
                });
            }
            else
            {
                return Task.FromResult(subscription);
            }
        }

        public async Task MergeAsync(AmbientContext ambientContext, Guid primaryId, Guid secondaryId)
        {
            var facetPrimary = await ambientContext.DatabaseContext.Facets.FindAsync(primaryId);
            if (facetPrimary == null) throw new NotFoundEntityException();

            var facetSecondary = await ambientContext.DatabaseContext.Facets.FindAsync(secondaryId);
            if (facetSecondary == null) throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanMergeFacets(ambientContext.Claims,
                new[] {facetPrimary, facetSecondary})) throw new UnauthorizedOperationException();

            // Check for conflicting tags
            var tagsExistingLabel = ambientContext.DatabaseContext.Tags.AsQueryable()
                .Where(_ => _.FacetId == primaryId)
                .AsEnumerable()
                .Select(_ => new Tuple<Guid,Tag>(_.TagId, ambientContext.DatabaseContext.Tags
                    .FirstOrDefault(__ => __.FacetId == secondaryId && __.Label == _.Label)))
                .Where(_ => _.Item2 != null)
                .ToArray();

            // Update the documents with conflicting tags and remove the tags
            foreach (var tagPair in tagsExistingLabel)
            {
                foreach (var documentTag in ambientContext.DatabaseContext.DocumentTag
                    .AsQueryable()
                    .Where(_ => _.TagId == tagPair.Item2.TagId))
                {
                    documentTag.TagId = tagPair.Item1;
                }
                ambientContext.DatabaseContext.Tags.Remove(tagPair.Item2);
            }
            
            // Migrate the tags from one facet to the other.
            var enumerable = tagsExistingLabel.Select(_ => _.Item2).ToArray();
            var tags = ambientContext.DatabaseContext.Tags.AsQueryable().Where(_ => _.FacetId == secondaryId).AsEnumerable().Except(enumerable).ToArray();
            foreach (var t in tags)
            {
                t.FacetId = primaryId;
            }

            ambientContext.DatabaseContext.Remove(facetSecondary);
            ambientContext.DatabaseContext.OnSaveCompleteTasks.Add(
                () => _busClient.Publish(new FacetMergedMessage
                {
                    RetainedFacetId = facetPrimary.FacetId,
                    RemovedFacetId = facetSecondary.FacetId,
                    Tags = tags.Select(_ => _.TagId),
                    UserId = ambientContext.CurrentUser.Id
                })
            );
        }

        private static IQueryable<TagFacet> BuildQuery(IQueryable<TagFacet> tags, FacetQuery query)
        {
            if (query == null)
                return tags;

            if (query.Mandatory != null)
                tags = tags.Where(_ => _.Mandatory == query.Mandatory).AsQueryable();

            if (query.AutoExtract != null)
                tags = tags.Where(_ => _.AutoExtract == query.AutoExtract).AsQueryable();

            if (!string.IsNullOrEmpty(query.Prefix))
                tags = tags.Where(_ => _.Prefix == query.Prefix).AsQueryable();

            return tags;
        }

        private bool IsValid(TagFacet facet, out List<ValidationResult> modelErrors)
        {
            var validationContext = new ValidationContext(facet);
            modelErrors = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(facet,
                validationContext,
                modelErrors);
            return isValid;
        }
    }
}