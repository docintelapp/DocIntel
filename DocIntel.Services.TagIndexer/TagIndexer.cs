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
using System.Linq;
using System.Threading.Tasks;

using DocIntel.Core.Exceptions;
using DocIntel.Core.Logging;
using DocIntel.Core.Messages;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.Indexation;

using MassTransit;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocIntel.Services.TagIndexer
{
    public class TagIndexer :
        IConsumer<TagCreatedMessage>,
        IConsumer<TagUpdatedMessage>,
        IConsumer<TagRemovedMessage>,
        IConsumer<TagMergedMessage>,
        IConsumer<FacetTagUpdatedMessage>,
        IConsumer<FacetTagRemovedMessage>,
        IConsumer<FacetTagCreatedMessage>,
        IConsumer<FacetMergedMessage>
    {
        // TODO Move to EventIds
        public static EventId Unauthorized = new(31001, "authorization-fail");
        public static EventId EntityNotFound = new(31002, "tag-not-found");
        private readonly ApplicationSettings _appSettings;
        private readonly ITagFacetIndexingUtility _facetIndexingUtility;
        private readonly ITagFacetRepository _facetRepository;
        private readonly ITagIndexingUtility _indexingUtility;
        private readonly ILogger<TagIndexer> _logger;
        private readonly ITagRepository _tagRepository;
        private readonly IUserClaimsPrincipalFactory<AppUser> _userClaimsPrincipalFactory;
        private readonly IServiceProvider _serviceProvider;

        public TagIndexer(ILogger<TagIndexer> logger,
            ITagRepository tagRepository,
            ITagIndexingUtility indexingUtility,
            ITagFacetIndexingUtility facetIndexingUtility,
            IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory, ITagFacetRepository facetRepository,
            ApplicationSettings appSettings, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _tagRepository = tagRepository;
            _indexingUtility = indexingUtility;
            _facetIndexingUtility = facetIndexingUtility;

            _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
            _facetRepository = facetRepository;
            _appSettings = appSettings;
            _serviceProvider = serviceProvider;
        }

        public async Task Consume(ConsumeContext<FacetMergedMessage> context)
        {
            var ambientContext = GetAmbientContext();
            foreach (var tagId in context.Message.Tags) await UpdateIndex(tagId, ambientContext);
            await UpdateFacetIndex(context.Message.RetainedFacetId, ambientContext);
            RemoveFromFacetIndex(context.Message.RemovedFacetId, ambientContext);
        }

        public async Task Consume(ConsumeContext<FacetTagCreatedMessage> context)
        {
            var ambientContext = GetAmbientContext();
            await AddToFacetIndex(context.Message.FacetTagId, ambientContext);
        }

        public Task Consume(ConsumeContext<FacetTagRemovedMessage> context)
        {
            var ambientContext = GetAmbientContext();
            foreach (var tagId in context.Message.Tags) RemoveFromIndex(tagId, ambientContext);
            RemoveFromFacetIndex(context.Message.FacetTagId, ambientContext);
            return Task.CompletedTask;
        }

        public async Task Consume(ConsumeContext<FacetTagUpdatedMessage> context)
        {
            var ambientContext = GetAmbientContext();
            var tags = _tagRepository.GetAllAsync(ambientContext, new TagQuery {FacetId = context.Message.FacetTagId})
                .Select(_ => _.TagId);
            foreach (var tagId in tags.ToEnumerable()) await UpdateIndex(tagId, ambientContext);
            await UpdateFacetIndex(context.Message.FacetTagId, ambientContext);
        }

        public async Task Consume(ConsumeContext<TagCreatedMessage> context)
        {
            _logger.LogDebug("TagCreatedMessage: {0}", context.Message.TagId);

            var ambientContext = GetAmbientContext();
            await AddToIndex(context.Message.TagId, ambientContext);
        }

        public async Task Consume(ConsumeContext<TagMergedMessage> context)
        {
            var ambientContext = GetAmbientContext();
            await UpdateIndex(context.Message.RetainedTagId, ambientContext);
            RemoveFromIndex(context.Message.RemovedTagId, ambientContext);
        }

        public Task Consume(ConsumeContext<TagRemovedMessage> context)
        {
            _logger.LogDebug("TagRemovedMessage: {0}", context.Message.TagId);

            var ambientContext = GetAmbientContext();
            RemoveFromIndex(context.Message.TagId, ambientContext);
            return Task.CompletedTask;
        }

        public async Task Consume(ConsumeContext<TagUpdatedMessage> context)
        {
            _logger.LogDebug("TagUpdatedMessage: {0}", context.Message.TagId);

            var ambientContext = GetAmbientContext();
            await UpdateIndex(context.Message.TagId, ambientContext);
        }

        private void RemoveFromIndex(Guid tagId, AmbientContext ambientContext)
        {
            try
            {
                _indexingUtility.Remove(tagId);
                _logger.LogInformation($"Tag '{tagId}' removed from index.");
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Warning,
                    EntityNotFound,
                    new LogEvent(
                            $"User '{ambientContext.CurrentUser.UserName}' could not remove the tag '{tagId}' from index: {e.GetType()} ({e.Message})")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("tag.id", tagId)
                        .AddException(e),
                    null,
                    LogEvent.Formatter);
                _logger.LogDebug(e.StackTrace);
            }
        }

        private void RemoveFromFacetIndex(Guid facetId, AmbientContext ambientContext)
        {
            try
            {
                _facetIndexingUtility.Remove(facetId);
                _logger.LogInformation($"Tag '{facetId}' removed from index.");
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Warning,
                    EntityNotFound,
                    new LogEvent(
                            $"User '{ambientContext.CurrentUser.UserName}' could not remove the facet '{facetId}' from index: {e.GetType()} ({e.Message})")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("tag.id", facetId)
                        .AddException(e),
                    null,
                    LogEvent.Formatter);
                _logger.LogDebug(e.StackTrace);
            }
        }

        private async Task AddToIndex(Guid tagId, AmbientContext ambientContext)
        {
            try
            {
                var tag = await _tagRepository.GetAsync(ambientContext,
                    tagId,
                    new[]
                    {
                        nameof(Tag.Facet)
                    });
                _indexingUtility.Add(tag);
                _logger.LogInformation("Index updated for the tag {0}", tag.TagId);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    Unauthorized,
                    new LogEvent(
                            $"User '{ambientContext.CurrentUser.UserName}' attempted to retreive tag without legitimate rights.")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("tag.id", tagId),
                    null,
                    LogEvent.Formatter);
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EntityNotFound,
                    new LogEvent(
                            $"User '{ambientContext.CurrentUser.UserName}' attempted to retreive a non-existing tag.")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("tag.id", tagId),
                    null,
                    LogEvent.Formatter);
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Warning,
                    EntityNotFound,
                    new LogEvent(
                            $"User '{ambientContext.CurrentUser.UserName}' could not add the tag '{tagId}' to the index: {e.GetType()} ({e.Message})")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("tag.id", tagId)
                        .AddException(e),
                    null,
                    LogEvent.Formatter);
                _logger.LogDebug(e.StackTrace);
            }
        }

        private async Task AddToFacetIndex(Guid facetId, AmbientContext ambientContext)
        {
            try
            {
                var facet = await _facetRepository.GetAsync(ambientContext,
                    facetId);
                _facetIndexingUtility.Add(facet);
                _logger.LogInformation("Index updated for the facet {0}", facet.Id);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    Unauthorized,
                    new LogEvent(
                            $"User '{ambientContext.CurrentUser.UserName}' attempted to retrieve facet without legitimate rights.")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("tag.id", facetId),
                    null,
                    LogEvent.Formatter);
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EntityNotFound,
                    new LogEvent(
                            $"User '{ambientContext.CurrentUser.UserName}' attempted to retrieve a non-existing facet.")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("tag.id", facetId),
                    null,
                    LogEvent.Formatter);
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Warning,
                    EntityNotFound,
                    new LogEvent(
                            $"User '{ambientContext.CurrentUser.UserName}' could not add the facet '{facetId}' to the index: {e.GetType()} ({e.Message})")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("tag.id", facetId)
                        .AddException(e),
                    null,
                    LogEvent.Formatter);
                _logger.LogDebug(e.StackTrace);
            }
        }

        private async Task UpdateIndex(Guid tagId, AmbientContext ambientContext)
        {
            try
            {
                var tag = await _tagRepository.GetAsync(ambientContext,
                    tagId,
                    new[]
                    {
                        nameof(Tag.Facet)
                    });
                _indexingUtility.Update(tag);
                _logger.LogInformation("Index updated for the tag {0}", tag.TagId);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    Unauthorized,
                    new LogEvent(
                            $"User '{ambientContext.CurrentUser.UserName}' attempted to retrieve tag without legitimate rights.")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("tag.id", tagId),
                    null,
                    LogEvent.Formatter);
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EntityNotFound,
                    new LogEvent(
                            $"User '{ambientContext.CurrentUser.UserName}' attempted to retrieve a non-existing tag.")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("tag.id", tagId),
                    null,
                    LogEvent.Formatter);
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Warning,
                    EntityNotFound,
                    new LogEvent($"User '{ambientContext.CurrentUser.UserName}' could not index the tag: " +
                                 e.GetType() + " (" + e.Message + ")")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("tag.id", tagId)
                        .AddException(e),
                    null,
                    LogEvent.Formatter);
                _logger.LogDebug(e.StackTrace);
            }
        }

        private async Task UpdateFacetIndex(Guid facetId, AmbientContext ambientContext)
        {
            try
            {
                var facet = await _facetRepository.GetAsync(ambientContext,
                    facetId);
                _facetIndexingUtility.Update(facet);
                _logger.LogInformation("Index updated for the facet {0}", facet.Id);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    Unauthorized,
                    new LogEvent(
                            $"User '{ambientContext.CurrentUser.UserName}' attempted to retrieve facet without legitimate rights.")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("tag.id", facetId),
                    null,
                    LogEvent.Formatter);
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EntityNotFound,
                    new LogEvent(
                            $"User '{ambientContext.CurrentUser.UserName}' attempted to retrieve a non-existing facet.")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("tag.id", facetId),
                    null,
                    LogEvent.Formatter);
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Warning,
                    EntityNotFound,
                    new LogEvent($"User '{ambientContext.CurrentUser.UserName}' could not index the facet: " +
                                 e.GetType() + " (" + e.Message + ")")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("tag.id", facetId)
                        .AddException(e),
                    null,
                    LogEvent.Formatter);
                _logger.LogDebug(e.StackTrace);
            }
        }

        // TODO Refactor, code duplication
        private AmbientContext GetAmbientContext()
        {
            var dbContextOptions = _serviceProvider.GetRequiredService<DbContextOptions<DocIntelContext>>();
            var dbContextLogger = _serviceProvider.GetRequiredService<ILogger<DocIntelContext>>();
            var _dbContext = new DocIntelContext(dbContextOptions, dbContextLogger);
            var automationUser = _dbContext.Users.FirstOrDefault(_ => _.UserName == _appSettings.AutomationAccount);
            if (automationUser == null)
                throw new ArgumentNullException($"User '{_appSettings.AutomationAccount}' was not found.");

            var claims = _userClaimsPrincipalFactory.CreateAsync(automationUser).Result;
            var ambientContext = new AmbientContext
            {
                DatabaseContext = _dbContext,
                Claims = claims,
                CurrentUser = automationUser
            };
            return ambientContext;
        }
    }
}