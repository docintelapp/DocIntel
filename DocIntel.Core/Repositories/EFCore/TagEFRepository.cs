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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Messages;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories.Query;
using DotLiquid;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Tag = DocIntel.Core.Models.Tag;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace DocIntel.Core.Repositories.EFCore
{
    public class TagEFRepository :DefaultEFRepository<Tag>, ITagRepository
    {
        public TagEFRepository(
            IPublishEndpoint busClient,
            IAppAuthorizationService appAuthorizationService)
            : base(_ => _.DatabaseContext.Tags, busClient, appAuthorizationService)
        {
        }

        public async Task<Tag> CreateAsync(
            AmbientContext ambientContext,
            Tag tag)
        {
            if (!await _appAuthorizationService.CanCreateTag(ambientContext.Claims, tag))
                throw new UnauthorizedOperationException();

            if (IsValid(tag, out var modelErrors))
            {
                if (string.IsNullOrEmpty(tag.BackgroundColor)) tag.BackgroundColor = "bg-success-50";

                tag.Description = _sanitizer.Sanitize(tag.Description);
                
                tag.URL = GenerateSlug(ambientContext, tag);

                var tagNormalization = ambientContext.DatabaseContext.Facets
                    .SingleOrDefault(_ => _.FacetId == tag.FacetId)?.TagNormalization;
                if (!string.IsNullOrEmpty(tagNormalization))
                {
                    var labelTemplate = Template.Parse("{{label | " + tag.Facet.TagNormalization + "}}");
                    tag.Label = labelTemplate.Render(Hash.FromAnonymousObject(new { label = tag.Label }));
                }
                
                var trackingEntity = await ambientContext.DatabaseContext.AddAsync(tag);
                ambientContext.DatabaseContext.OnSaveCompleteTasks.Add(
                    () => _busClient.Publish(new TagCreatedMessage
                    {
                        TagId = trackingEntity.Entity.TagId,
                        UserId = ambientContext.CurrentUser.Id
                    })
                );

                return trackingEntity.Entity;
            }

            throw new InvalidArgumentException(modelErrors);
        }

        public async Task<Tag> UpdateAsync(
            AmbientContext ambientContext,
            Tag tag)
        {
            var retrievedTag = await ambientContext.DatabaseContext.Tags.FindAsync(tag.TagId);
            if (retrievedTag == null)
                throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanEditTag(ambientContext.Claims, retrievedTag))
                throw new UnauthorizedOperationException();

            if (IsValid(tag, out var modelErrors))
            {
                tag.ModificationDate = DateTime.UtcNow;

                if (ambientContext.CurrentUser != null) tag.LastModifiedById = ambientContext.CurrentUser.Id;

                tag.Description = _sanitizer.Sanitize(tag.Description);
                if (ambientContext.DatabaseContext.Entry(tag).Property(_ => _.Label).IsModified)
                    tag.URL = GenerateSlug(ambientContext, tag);

                if (!string.IsNullOrEmpty(tag.Facet.TagNormalization))
                {
                    var labelTemplate = Template.Parse("{{label | " + tag.Facet.TagNormalization + "}}");
                    tag.Label = labelTemplate.Render(Hash.FromAnonymousObject(new { label = tag.Label }));
                }
                
                var trackingEntity = ambientContext.DatabaseContext.Update(tag);
                ambientContext.DatabaseContext.OnSaveCompleteTasks.Add(
                    () => _busClient.Publish(new TagUpdatedMessage
                    {
                        TagId = trackingEntity.Entity.TagId,
                        UserId = ambientContext.CurrentUser.Id
                    })
                );

                return trackingEntity.Entity;
            }

            throw new InvalidArgumentException(modelErrors);
        }

        public async Task<Tag> RemoveAsync(
            AmbientContext ambientContext,
            Guid tagId)
        {
            var tag = ambientContext.DatabaseContext.Tags
                .Include(_ => _.Documents)
                .SingleOrDefault(_ => _.TagId == tagId);
            if (tag == null) throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanDeleteTag(ambientContext.Claims, tag))
                throw new UnauthorizedOperationException();

            var documentTags = tag.Documents.Select(_ => _.TagId).ToList();
            var trackingEntity = ambientContext.DatabaseContext.Tags.Remove(tag);
            ambientContext.DatabaseContext.OnSaveCompleteTasks.Add(
                () => _busClient.Publish(new TagRemovedMessage
                {
                    TagId = trackingEntity.Entity.TagId,
                    Documents = documentTags,
                    UserId = ambientContext.CurrentUser.Id
                })
            );

            return trackingEntity.Entity;
        }

        public Task<int> CountAsync(
            AmbientContext ambientContext,
            TagQuery tagQuery = null)
        {
            var query = BuildQuery(ambientContext.DatabaseContext.Tags, tagQuery);
            return query.CountAsync();
        }

        public async Task<bool> ExistsAsync(
            AmbientContext ambientContext,
            Guid tagId)
        {
            var tag = await ambientContext.DatabaseContext.Tags.FindAsync(tagId);
            if (tag != null) return await _appAuthorizationService.CanViewTag(ambientContext.Claims, tag);

            return false;
        }

        public async Task<bool> ExistsAsync(
            AmbientContext ambientContext,
            string facetPrefix,
            string label)
        {
            var tag = await ambientContext.DatabaseContext.Tags
                .AsQueryable()
                .Where(_ => (_.Facet.Prefix == facetPrefix) & (_.Label == label))
                .SingleOrDefaultAsync();
            if (tag != null) return await _appAuthorizationService.CanViewTag(ambientContext.Claims, tag);

            return false;
        }

        public async Task<bool> ExistsAsync(
            AmbientContext ambientContext,
            Guid facetId,
            string label,
            Guid? tagId = null)
        {
            var tag = await ambientContext.DatabaseContext.Tags
                .AsQueryable()
                .SingleOrDefaultAsync(_ => (_.FacetId == facetId) & (_.Label == label) & (_.TagId != tagId));
            if (tag != null) return await _appAuthorizationService.CanViewTag(ambientContext.Claims, tag);

            return false;
        }

        public async Task<Tag> GetAsync(
            AmbientContext ambientContext,
            Guid id,
            string[] includeRelatedData = null)
        {
            IQueryable<Tag> enumerable = ambientContext.DatabaseContext.Tags;

            if (includeRelatedData != null)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            var tag = enumerable.SingleOrDefault(_ => _.TagId == id);

            if (tag == null)
                throw new NotFoundEntityException();

            if (await _appAuthorizationService.CanViewTag(ambientContext.Claims, tag))
                return tag;
            throw new UnauthorizedOperationException();
        }

        public async Task<Tag> GetAsync(
            AmbientContext ambientContext,
            string label,
            string[] includeRelatedData = null)
        {
            IQueryable<Tag> enumerable = ambientContext.DatabaseContext.Tags;

            if (includeRelatedData != null)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            var tag = await enumerable.SingleOrDefaultAsync(_ => _.Label == label);

            if (tag == null) throw new NotFoundEntityException();

            if (await _appAuthorizationService.CanViewTag(ambientContext.Claims, tag))
                return tag;
            throw new UnauthorizedOperationException();
        }

        public async Task<Tag> GetAsync(
            AmbientContext ambientContext,
            Guid facetId,
            string label,
            string[] includeRelatedData = null)
        {
            IQueryable<Tag> enumerable = ambientContext.DatabaseContext.Tags;

            if (includeRelatedData != null)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            var tag = await enumerable.FirstOrDefaultAsync(_ => (_.FacetId == facetId) & (_.Label.ToUpper() == label.ToUpper()));

            if (tag == null) throw new NotFoundEntityException();

            if (await _appAuthorizationService.CanViewTag(ambientContext.Claims, tag))
                return tag;
            throw new UnauthorizedOperationException();
        }

        public async Task<Tag> GetAsync(
            AmbientContext ambientContext,
            TagQuery query,
            string[] includeRelatedData = null)
        {
            IQueryable<Tag> enumerable = ambientContext.DatabaseContext.Tags;

            if (includeRelatedData != null)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            var tag = BuildQuery(enumerable, query).SingleOrDefault();

            if (tag == null) throw new NotFoundEntityException();

            if (await _appAuthorizationService.CanViewTag(ambientContext.Claims, tag))
                return tag;
            throw new UnauthorizedOperationException();
        }

        public async IAsyncEnumerable<Tag> GetAllAsync(AmbientContext ambientContext, Func<IQueryable<Tag>, IQueryable<Tag>> query)
        {
            IQueryable<Tag> enumerable = ambientContext.DatabaseContext.Tags;

            enumerable = query(enumerable);

            foreach (var tag in enumerable)
                if (await _appAuthorizationService.CanViewTag(ambientContext.Claims, tag))
                    yield return tag;
        }


        public async IAsyncEnumerable<Tag> GetAllAsync(
            AmbientContext ambientContext,
            TagQuery query,
            string[] includeRelatedData = null)
        {
            IQueryable<Tag> enumerable = ambientContext.DatabaseContext.Tags;

            if (includeRelatedData != null)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            var filteredTags = BuildQuery(enumerable, query);

            foreach (var tag in filteredTags)
                if (await _appAuthorizationService.CanViewTag(ambientContext.Claims, tag))
                    yield return tag;
        }

        public async Task<Tag> MergeAsync(
            AmbientContext ambientContext,
            Guid tagPrimaryId,
            Guid tagSecondaryId)
        {
            var tagPrimary = await ambientContext.DatabaseContext.Tags.FindAsync(tagPrimaryId);
            if (tagPrimary == null) throw new NotFoundEntityException();

            var tagSecondary = await ambientContext.DatabaseContext.Tags.FindAsync(tagSecondaryId);
            if (tagSecondary == null) throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanMergeTags(ambientContext.Claims, new[] {tagPrimary, tagSecondary}))
                throw new UnauthorizedOperationException();

            // All documents from Tag2 to Tag1
            var documentTag1 = ambientContext.DatabaseContext.DocumentTag
                .AsQueryable()
                .Where(_ => _.TagId == tagPrimaryId)
                .Select(_ => _.DocumentId)
                .ToList();

            var documentTag2 = ambientContext.DatabaseContext.DocumentTag
                .AsQueryable()
                .Where(_ => _.TagId == tagSecondaryId)
                .Select(_ => _.DocumentId)
                .ToList();

            var documentIds = documentTag2.Except(documentTag1);
            foreach (var documentId in documentIds)
                ambientContext.DatabaseContext.DocumentTag.Add(new DocumentTag
                {
                    DocumentId = documentId,
                    TagId = tagPrimaryId
                });

            ambientContext.DatabaseContext.Remove(tagSecondary);
            ambientContext.DatabaseContext.OnSaveCompleteTasks.Add(
                () => _busClient.Publish(new TagMergedMessage
                {
                    RetainedTagId = tagPrimary.TagId,
                    RemovedTagId = tagSecondary.TagId,
                    Documents = documentIds,
                    UserId = ambientContext.CurrentUser.Id
                })
            );

            return tagPrimary;
        }

        public IAsyncEnumerable<(Tag tag, SubscriptionStatus status)> GetSubscriptionsAsync(
            AmbientContext ambientContext, AppUser user,
            int page = 0,
            int limit = 10)
        {
            var queryable = ambientContext.DatabaseContext.UserTagSubscriptions.Include(_ => _.Tag)
                .ThenInclude(_ => _.Facet).AsQueryable()
                .Where(_ => _.UserId == user.Id && _.Subscribed);

            if ((page > 0) & (limit > 0))
                queryable = queryable.Skip((page - 1) * limit).Take(limit);

            return queryable
                .AsAsyncEnumerable()
                .Select(_ => (tag: _.Tag, status: new SubscriptionStatus
                {
                    Subscribed = true,
                    Notification = _.Notify
                }));
        }

        public IAsyncEnumerable<Tag> GetMutedTagsAsync(
            AmbientContext ambientContext, AppUser user)
        {
            var queryable = ambientContext.DatabaseContext.UserTagSubscriptions
                .AsQueryable()
                .Where(_ => _.UserId == user.Id && _.Muted)
                .Select(_ => _.Tag);

            return queryable.AsAsyncEnumerable();
        }

        public async Task<SubscriptionStatus> IsSubscribedAsync(
            AmbientContext ambientContext, AppUser user,
            Guid tagId)
        {
            var retrievedTag = await ambientContext.DatabaseContext.Tags.FindAsync(tagId);
            if (retrievedTag == null) throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanSubscribeToTag(ambientContext.Claims, retrievedTag))
                return new SubscriptionStatus {Subscribed = false};

            var subscription =
                ambientContext.DatabaseContext.UserTagSubscriptions.FirstOrDefault(_ =>
                    (_.TagId == tagId) & (_.UserId == user.Id));
            if (subscription == null)
                return new SubscriptionStatus {Subscribed = false};
            return new SubscriptionStatus {Subscribed = subscription.Subscribed, Notification = subscription.Notify};
        }

        public async Task SubscribeAsync(
            AmbientContext ambientContext, AppUser user,
            Guid tagId,
            bool notification = false)
        {
            var retrievedTag = await ambientContext.DatabaseContext.Tags.FindAsync(tagId);
            if (retrievedTag == null) throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanSubscribeToTag(ambientContext.Claims, retrievedTag))
                throw new UnauthorizedOperationException();

            var subscription = ambientContext.DatabaseContext.UserTagSubscriptions
                .FirstOrDefault(_ => (_.TagId == tagId) & (_.UserId == user.Id));

            if (subscription != null)
            {
                subscription.Subscribed = true;
                subscription.Notify = notification;
                ambientContext.DatabaseContext.UserTagSubscriptions.Update(subscription);
            }
            else
            {
                subscription = new UserTagSubscription
                {
                    TagId = tagId,
                    UserId = user.Id,
                    Notify = notification,
                    Subscribed = true,
                    Muted = false
                };
                await ambientContext.DatabaseContext.UserTagSubscriptions.AddAsync(subscription);
            }
        }

        public async Task UnsubscribeAsync(
            AmbientContext ambientContext, AppUser user,
            Guid tagId)
        {
            var retrievedTag = await ambientContext.DatabaseContext.Tags.FindAsync(tagId);
            if (retrievedTag == null) throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanSubscribeToTag(ambientContext.Claims, retrievedTag))
                throw new UnauthorizedOperationException();

            var subscription = ambientContext.DatabaseContext.UserTagSubscriptions
                .FirstOrDefault(_ => (_.TagId == tagId) & (_.UserId == user.Id));

            if (subscription != null)
            {
                if (subscription.Muted)
                {
                    subscription.Subscribed = false;
                    subscription.Notify = false;
                    ambientContext.DatabaseContext.UserTagSubscriptions.Update(subscription);
                }
                else
                {
                    ambientContext.DatabaseContext.UserTagSubscriptions.RemoveRange(
                        ambientContext.DatabaseContext.UserTagSubscriptions.AsQueryable()
                            .Where(_ => (_.TagId == tagId) & (_.UserId == user.Id))
                    );
                }
            }
        }

        public async Task<MuteStatus> IsMutedAsync(
            AmbientContext ambientContext, AppUser user,
            Guid tagId)
        {
            var retrievedTag = await ambientContext.DatabaseContext.Tags.FindAsync(tagId);
            if (retrievedTag == null) throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanSubscribeToTag(ambientContext.Claims, retrievedTag))
                return new MuteStatus {Muted = false};

            var subscription =
                ambientContext.DatabaseContext.UserTagSubscriptions.FirstOrDefault(_ =>
                    (_.TagId == tagId) & (_.UserId == user.Id));
            if (subscription == null)
                return new MuteStatus {Muted = false};
            return new MuteStatus {Muted = subscription.Muted};
        }

        public async Task MuteAsync(
            AmbientContext ambientContext, AppUser user,
            Guid tagId)
        {
            var retrievedTag = await ambientContext.DatabaseContext.Tags.FindAsync(tagId);
            if (retrievedTag == null) throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanSubscribeToTag(ambientContext.Claims, retrievedTag))
                throw new UnauthorizedOperationException();

            var subscription = ambientContext.DatabaseContext.UserTagSubscriptions
                .FirstOrDefault(_ => (_.TagId == tagId) & (_.UserId == user.Id));

            if (subscription != null)
            {
                subscription.Muted = true;
                ambientContext.DatabaseContext.UserTagSubscriptions.Update(subscription);
            }
            else
            {
                subscription = new UserTagSubscription
                {
                    TagId = tagId,
                    UserId = user.Id,
                    Notify = false,
                    Subscribed = false,
                    Muted = true
                };
                await ambientContext.DatabaseContext.UserTagSubscriptions.AddAsync(subscription);
            }
        }

        public async Task UnmuteAsync(
            AmbientContext ambientContext, AppUser user,
            Guid tagId)
        {
            var retrievedTag = await ambientContext.DatabaseContext.Tags.FindAsync(tagId);
            if (retrievedTag == null) throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanSubscribeToTag(ambientContext.Claims, retrievedTag))
                throw new UnauthorizedOperationException();

            var subscription = ambientContext.DatabaseContext.UserTagSubscriptions
                .FirstOrDefault(_ => (_.TagId == tagId) & (_.UserId == user.Id));

            if (subscription != null)
            {
                if (subscription.Subscribed)
                {
                    subscription.Muted = false;
                    ambientContext.DatabaseContext.UserTagSubscriptions.Update(subscription);
                }
                else // not subscribed and not muted, we can remove the item from the database
                {
                    ambientContext.DatabaseContext.UserTagSubscriptions.RemoveRange(
                        ambientContext.DatabaseContext.UserTagSubscriptions.AsQueryable()
                            .Where(_ => (_.TagId == tagId) & (_.UserId == user.Id))
                    );
                }
            }
        }

        private static IQueryable<Tag> BuildQuery(
            IQueryable<Tag> tags,
            TagQuery query)
        {
            if (query.Ids != null && query.Ids.Any())
                tags = tags.Where(_ => query.Ids.Contains(_.TagId)).AsQueryable();
            
            if (query.Label != null)
                tags = tags.Where(_ => _.Label.ToUpper() == query.Label.ToUpper()).AsQueryable();

            if (query.FullLabel != null)
                tags = tags.Where(_ => _.Facet.Prefix + ":" + _.Label == query.FullLabel).AsQueryable();

            if (query.FacetPrefix != null)
                tags = tags.Where(_ => _.Facet.Prefix == query.FacetPrefix);

            if (query.StartsWith != null)
                tags = tags.Where(_ => _.Label.StartsWith(query.StartsWith));

            if (query.FacetId != null)
                tags = tags.Where(_ => _.FacetId == query.FacetId);

            if (!string.IsNullOrEmpty(query.URL))
                tags = tags.Where(_ => _.URL == query.URL);

            if (query.SubscribedUser != null)
                tags = tags.Where(_ => _.SubscribedUser.Select(__ => __.UserId).Contains(query.SubscribedUser));

            if ((query.Page > 0) & (query.Limit > 0))
                tags = tags.Skip((query.Page - 1) * query.Limit).Take(query.Limit);

            return tags;
        }

        private bool IsValid(
            Tag tag,
            out List<ValidationResult> modelErrors)
        {
            var validationContext = new ValidationContext(tag);
            modelErrors = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(tag,
                validationContext,
                modelErrors);
            return isValid;
        }

        private string GenerateSlug(AmbientContext context, Tag document)
        {
            var slug = GenerateSlug(document.Label, 0);
            if (slug == document.URL) return slug;

            var regexSlug = "^" + Regex.Escape(slug) + "(-[0-9]+)?$";
            var maxSlug = context.DatabaseContext.Tags
                .Where(_ => _.Label == document.Label & Regex.IsMatch(_.URL, regexSlug))
                .Select(_ => new { URL = _.URL, Length = _.URL.Length })
                .OrderByDescending(_ => _.Length).ThenByDescending(_ => _.URL)
                .FirstOrDefault();
            if (maxSlug == null)
                return slug;
            else {
                var lastPart = maxSlug.URL.Split('-').Last();
                if (Int64.TryParse(lastPart, out long result)) {
                    return maxSlug.URL.Substring(0, maxSlug.Length - lastPart.Length - 1) + "-" + (result+1);
                } else {
                    return maxSlug.URL + "-1";
                }
            }
        }
    }
}