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
    public class SourceEFRepository : DefaultEFRepository<Source>, ISourceRepository
    {
        private readonly ILogger<SourceEFRepository> _logger;

        public SourceEFRepository(IPublishEndpoint busClient,
            IAppAuthorizationService appAuthorizationService, ILogger<SourceEFRepository> logger) 
            : base(_ => _.DatabaseContext.Sources, busClient, appAuthorizationService)
        {
            _logger = logger;
        }

        public async Task<Source> CreateAsync(AmbientContext ambientContext, Source source)
        {
            if (!await _appAuthorizationService.CanCreateSource(ambientContext.Claims, source))
                throw new UnauthorizedOperationException();

            var modelErrors = new List<ValidationResult>();
            if (IsValid(source, modelErrors))
            {
                var utcNow = DateTime.UtcNow;
                source.CreationDate = utcNow;
                source.ModificationDate = utcNow;
                source.Description = _sanitizer.Sanitize(source.Description);

                if (ambientContext.CurrentUser != null)
                {
                    source.RegisteredById = ambientContext.CurrentUser.Id;
                    source.LastModifiedById = ambientContext.CurrentUser.Id;
                }

                source.URL = UpdateSourceURL(ambientContext, source, _ => _.Title, url => data => data.URL == url);

                var trackingEntity = await _tableSelector(ambientContext).AddAsync(source);
                PublishMessage(ambientContext, new SourceCreatedMessage
                {
                    SourceId = trackingEntity.Entity.SourceId,
                    UserId = ambientContext.CurrentUser.Id
                });
                return trackingEntity.Entity;
            }

            throw new InvalidArgumentException(modelErrors);
        }

        public async Task<Source> UpdateAsync(AmbientContext ambientContext, Source source)
        {
            var retrievedSource = _tableSelector(ambientContext).SingleOrDefault(_ => _.SourceId == source.SourceId);
            if (retrievedSource == null)
                throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanEditSource(ambientContext.Claims, retrievedSource))
                throw new UnauthorizedOperationException();

            var modelErrors = new List<ValidationResult>();
            if (IsValid(source, modelErrors))
            {
                source.ModificationDate = DateTime.UtcNow;

                if (ambientContext.CurrentUser != null) source.LastModifiedById = ambientContext.CurrentUser.Id;

                source.Description = _sanitizer.Sanitize(source.Description);
                source.URL = UpdateSourceURL(ambientContext, source, _ => _.Title, url => data => data.SourceId != source.SourceId & data.URL == url);

                var trackingEntity = ambientContext.DatabaseContext.Update(source);
                PublishMessage(ambientContext, new SourceUpdatedMessage
                    {
                        SourceId = trackingEntity.Entity.SourceId,
                        UserId = ambientContext.CurrentUser.Id
                    }
                );

                return trackingEntity.Entity;
            }

            throw new InvalidArgumentException(modelErrors);
        }

        public async Task<Source> RemoveAsync(AmbientContext ambientContext, Guid sourceId)
        {
            var source = await _tableSelector(ambientContext)
                .Include(_ => _.Documents)
                .AsQueryable()
                .SingleOrDefaultAsync(_ => _.SourceId == sourceId);

            if (source == null)
                throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanDeleteSource(ambientContext.Claims, source))
                throw new UnauthorizedOperationException();

            var documents = source.Documents.Select(_ => _.DocumentId).ToArray();
            var trackingEntity = _tableSelector(ambientContext).Remove(source);
            PublishMessage(ambientContext, new SourceRemovedMessage
                {
                    SourceId = trackingEntity.Entity.SourceId,
                    Documents = documents,
                    UserId = ambientContext.CurrentUser.Id
                }
            );

            return trackingEntity.Entity;
        }

        public async Task MergeAsync(AmbientContext ambientContext, Guid sourceId1, Guid sourceId2)
        {
            var source1 = await _tableSelector(ambientContext).FindAsync(sourceId1);
            if (source1 == null)
                throw new NotFoundEntityException();

            var source2 = await _tableSelector(ambientContext).FindAsync(sourceId2);
            if (source2 == null)
                throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanMergeSource(ambientContext.Claims, new[] {source1, source2}))
                throw new UnauthorizedOperationException();

            // All documents from Source2 to Source1
            var docIdToIndexAgain = new List<Guid>();
            foreach (var d in ambientContext.DatabaseContext.Documents.AsQueryable()
                .Where(_ => _.SourceId == sourceId2))
            {
                d.SourceId = sourceId1;
                docIdToIndexAgain.Add(d.DocumentId);
            }

            _tableSelector(ambientContext).Remove(source2);

            PublishMessage(ambientContext, new SourceMergedMessage
            {
                PrimarySourceId = source1.SourceId,
                SecondarySourceId = source2.SourceId,
                Documents = docIdToIndexAgain,
                UserId = ambientContext.CurrentUser.Id
            });
        }

        public async Task<bool> ExistsAsync(AmbientContext ambientContext, Guid sourceId)
        {
            var feed = await _tableSelector(ambientContext).FindAsync(sourceId);
            if (feed != null) return await _appAuthorizationService.CanViewSource(ambientContext.Claims, feed);

            return false;
        }

        public async IAsyncEnumerable<Source> GetAllAsync(AmbientContext ambientContext,
            object relatedData1,
            string[] includeRelatedData = null,
            int page = 0,
            int limit = 10)
        {
            IQueryable<Source> enumerable = _tableSelector(ambientContext);

            if (includeRelatedData != null)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            var filteredSources = enumerable.Skip((page - 1) * limit).Take(limit);

            foreach (var source in filteredSources)
                if (await _appAuthorizationService.CanViewSource(ambientContext.Claims, source))
                    yield return source;
        }

        public async IAsyncEnumerable<Source> GetAllAsync(AmbientContext ambientContext, SourceQuery query,
            string[] includeRelatedData = null)
        {
            IQueryable<Source> enumerable = _tableSelector(ambientContext);

            if (includeRelatedData != null)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            var sources = BuildQuery(enumerable, query);

            foreach (var source in sources)
                if (await _appAuthorizationService.CanViewSource(ambientContext.Claims, source))
                    yield return source;
        }

        public async IAsyncEnumerable<Source> GetAllAsync(AmbientContext ambientContext, Func<IQueryable<Source>, IQueryable<Source>> relatedData)
        {
            IQueryable<Source> enumerable = _tableSelector(ambientContext);

            if (relatedData != null)
                enumerable = relatedData(enumerable);

            foreach (var source in enumerable)
                if (await _appAuthorizationService.CanViewSource(ambientContext.Claims, source))
                    yield return source;
        }

        public async Task<Source> GetAsync(AmbientContext ambientContext, Guid sourceId,
            string[] includeRelatedData = null)
        {
            IQueryable<Source> enumerable = _tableSelector(ambientContext);

            if (includeRelatedData != null)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            var source = enumerable.SingleOrDefault(_ => _.SourceId == sourceId);

            if (source == null)
                throw new NotFoundEntityException();

            if (await _appAuthorizationService.CanViewSource(ambientContext.Claims, source))
                return source;
            throw new UnauthorizedOperationException();
        }

        public async Task<Source> GetAsync(AmbientContext ambientContext, SourceQuery query,
            string[] includeRelatedData = null)
        {
            IQueryable<Source> enumerable = _tableSelector(ambientContext);

            if (includeRelatedData != null)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            var source = BuildQuery(enumerable, query).SingleOrDefault();

            if (source == null)
                throw new NotFoundEntityException();

            if (await _appAuthorizationService.CanViewSource(ambientContext.Claims, source))
                return source;
            throw new UnauthorizedOperationException();
        }

        public async Task<Source> GetAsync(AmbientContext ambientContext, string title,
            string[] includeRelatedData = null)
        {
            IQueryable<Source> enumerable = _tableSelector(ambientContext);

            if (includeRelatedData != null)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            var source = enumerable.SingleOrDefault(_ => _.Title == title);

            if (source == null)
                throw new NotFoundEntityException();

            if (await _appAuthorizationService.CanViewSource(ambientContext.Claims, source))
                return source;
            throw new UnauthorizedOperationException();
        }

        public Task<int> CountAsync(AmbientContext ambientContext)
        {
            // TODO How to efficiently check each item for CanViewSource
            return _tableSelector(ambientContext).AsQueryable().CountAsync();
        }

        public IAsyncEnumerable<Source> GetMutedSourcesAsync(
            AmbientContext ambientContext, AppUser user)
        {
            var queryable = ambientContext.DatabaseContext.UserSourceSubscription
                .AsQueryable()
                .Where(_ => _.UserId == user.Id && _.Muted)
                .Select(_ => _.Source);

            return queryable.AsAsyncEnumerable();
        }

        public async Task SubscribeAsync(AmbientContext ambientContext, AppUser user, Guid sourceId, bool notification)
        {
            var retrievedSource = await _tableSelector(ambientContext).FindAsync(sourceId);
            if (retrievedSource == null)
                throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanSubscribeToSource(ambientContext.Claims, user, retrievedSource))
                throw new UnauthorizedOperationException();

            var subscription = ambientContext.DatabaseContext.UserSourceSubscription
                .FirstOrDefault(_ => (_.SourceId == sourceId) & (_.UserId == user.Id));

            if (subscription != null)
            {
                subscription.Subscribed = true;
                subscription.Notify = notification;
                ambientContext.DatabaseContext.UserSourceSubscription.Update(subscription);
            }
            else
            {
                subscription = new UserSourceSubscription
                {
                    SourceId = sourceId,
                    UserId = user.Id,
                    Notify = notification,
                    Subscribed = true,
                    Muted = false
                };
                await ambientContext.DatabaseContext.UserSourceSubscription.AddAsync(subscription);
            }
        }

        public async Task UnsubscribeAsync(AmbientContext ambientContext, AppUser user, Guid sourceId)
        {
            var retrievedSource = await _tableSelector(ambientContext).FindAsync(sourceId);
            if (retrievedSource == null)
                throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanSubscribeToSource(ambientContext.Claims, user, retrievedSource))
                throw new UnauthorizedOperationException();

            var subscription = ambientContext.DatabaseContext.UserSourceSubscription
                .FirstOrDefault(_ => (_.SourceId == sourceId) & (_.UserId == user.Id));
            
            if (subscription != null)
            {
                if (subscription.Muted)
                {
                    subscription.Subscribed = false;
                    subscription.Notify = false;
                    ambientContext.DatabaseContext.UserSourceSubscription.Update(subscription);
                }
                else
                {
                    ambientContext.DatabaseContext.UserSourceSubscription.RemoveRange(
                        ambientContext.DatabaseContext.UserSourceSubscription.AsQueryable()
                            .Where(_ => (_.SourceId == sourceId) & (_.UserId == user.Id))
                    );
                }
            }
        }

        public async Task<SubscriptionStatus> IsSubscribedAsync(AmbientContext ambientContext, AppUser user,
            Guid sourceId)
        {
            var retrievedSource = await _tableSelector(ambientContext).FindAsync(sourceId);
            if (retrievedSource == null)
                throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanSubscribeToSource(ambientContext.Claims, user, retrievedSource))
                return new SubscriptionStatus {Subscribed = false};
            var subscription =
                ambientContext.DatabaseContext.UserSourceSubscription.FirstOrDefault(_ =>
                    (_.SourceId == sourceId) & (_.UserId == user.Id));
            if (subscription == null)
                return new SubscriptionStatus {Subscribed = false};
            return new SubscriptionStatus {Subscribed = subscription.Subscribed, Notification = subscription.Notify};
        }
        
        public async Task MuteAsync(AmbientContext ambientContext, AppUser user, Guid sourceId)
        {
            var retrievedSource = await _tableSelector(ambientContext).FindAsync(sourceId);
            if (retrievedSource == null)
                throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanSubscribeToSource(ambientContext.Claims, user, retrievedSource))
                throw new UnauthorizedOperationException();

            var subscription = ambientContext.DatabaseContext.UserSourceSubscription
                .FirstOrDefault(_ => (_.SourceId == sourceId) & (_.UserId == user.Id));

            if (subscription != null)
            {
                subscription.Muted = true;
                ambientContext.DatabaseContext.UserSourceSubscription.Update(subscription);
            }
            else
            {
                subscription = new UserSourceSubscription
                {
                    SourceId = sourceId,
                    UserId = user.Id,
                    Notify = false,
                    Subscribed = false,
                    Muted = true
                };
                await ambientContext.DatabaseContext.UserSourceSubscription.AddAsync(subscription);
            }
        }

        public async Task UnmuteAsync(AmbientContext ambientContext, AppUser user, Guid sourceId)
        {
            var retrievedSource = await _tableSelector(ambientContext).FindAsync(sourceId);
            if (retrievedSource == null)
                throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanSubscribeToSource(ambientContext.Claims, user, retrievedSource))
                throw new UnauthorizedOperationException();

            var subscription = ambientContext.DatabaseContext.UserSourceSubscription
                .FirstOrDefault(_ => (_.SourceId == sourceId) & (_.UserId == user.Id));
            
            if (subscription != null)
            {
                if (subscription.Subscribed)
                {
                    subscription.Muted = false;
                    ambientContext.DatabaseContext.UserSourceSubscription.Update(subscription);
                }
                else // not subscribed and not muted, we can remove the item from the database
                {
                    ambientContext.DatabaseContext.UserSourceSubscription.RemoveRange(
                        ambientContext.DatabaseContext.UserSourceSubscription.AsQueryable()
                            .Where(_ => (_.SourceId == sourceId) & (_.UserId == user.Id))
                    );
                }
            }
        }

        public async Task<MuteStatus> IsMutedAsync(AmbientContext ambientContext, AppUser user,
            Guid sourceId)
        {
            var retrievedSource = await _tableSelector(ambientContext).FindAsync(sourceId);
            if (retrievedSource == null)
                throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanSubscribeToSource(ambientContext.Claims, user, retrievedSource))
                return new MuteStatus {Muted = false};

            var subscription =
                ambientContext.DatabaseContext.UserSourceSubscription.FirstOrDefault(_ =>
                    (_.SourceId == sourceId) & (_.UserId == user.Id));
            if (subscription == null)
                return new MuteStatus {Muted = false};
            return new MuteStatus {Muted = subscription.Muted};
        }

        public IAsyncEnumerable<(Source source, SubscriptionStatus status)> GetSubscriptionsAsync(
            AmbientContext ambientContext, AppUser user,
            int page = 0,
            int limit = 10)
        {
            var queryable = ambientContext.DatabaseContext.UserSourceSubscription.Include(_ => _.Source)
                .Where(_ => _.UserId == user.Id & _.Subscribed);

            if ((page > 0) & (limit > 0))
                queryable = queryable.Skip((page - 1) * limit).Take(limit);

            return queryable
                .AsAsyncEnumerable()
                .Select(_ => (source: _.Source, status: new SubscriptionStatus
                {
                    Subscribed = true,
                    Notification = _.Notify
                }));
        }

        public async IAsyncEnumerable<Source> GetAsync(AmbientContext ambientContext, IEnumerable<Guid> sourceIds,
            string[] includeRelatedData = null)
        {
            IQueryable<Source> enumerable = _tableSelector(ambientContext);

            if (includeRelatedData != null)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            var sources = enumerable.Where(_ => sourceIds.Contains(_.SourceId));

            foreach (var source in sources)
                if (await _appAuthorizationService.CanViewSource(ambientContext.Claims, source))
                    yield return source;
        }

        private static IQueryable<Source> BuildQuery(IQueryable<Source> documents,
            SourceQuery query)
        {
            if (query.SourceId != default)
                documents = documents.Where(_ => _.SourceId == query.SourceId);

            if (!string.IsNullOrEmpty(query.Title))
                documents = documents.Where(_ => _.Title == query.Title);

            if (!string.IsNullOrEmpty(query.URL))
                documents = documents.Where(_ => _.URL == query.URL);

            if (!string.IsNullOrEmpty(query.HomePage))
                documents = documents.Where(_ => _.HomePage == query.HomePage);

            if ((query.Page > 0) & (query.Limit > 0))
                documents = documents.Skip((query.Page - 1) * query.Limit).Take(query.Limit);

            return documents;
        }
    }
}