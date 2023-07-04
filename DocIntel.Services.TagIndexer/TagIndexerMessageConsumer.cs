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
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Logging;
using DocIntel.Core.Messages;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Services;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.Indexation;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocIntel.Services.TagIndexer;

public class TagIndexerMessageConsumer :
    DynamicContextConsumer,
    IConsumer<TagCreatedMessage>,
    IConsumer<TagUpdatedMessage>,
    IConsumer<TagRemovedMessage>,
    IConsumer<TagMergedMessage>,
    IConsumer<FacetUpdatedMessage>,
    IConsumer<FacetRemovedMessage>,
    IConsumer<FacetMergedMessage>
{
    // TODO Move to EventIds
    public static EventId Unauthorized = new(31001, "authorization-fail");
    public static EventId EntityNotFound = new(31002, "tag-not-found");
    private readonly ITagFacetIndexingUtility _facetIndexingUtility;
    private readonly ITagFacetRepository _facetRepository;
    private readonly ITagIndexingUtility _indexingUtility;
    private readonly ILogger<TagIndexerMessageConsumer> _logger;
    private readonly ITagRepository _tagRepository;

    public TagIndexerMessageConsumer(ILogger<TagIndexerMessageConsumer> logger,
        ITagRepository tagRepository,
        ITagIndexingUtility indexingUtility,
        ITagFacetIndexingUtility facetIndexingUtility,
        AppUserClaimsPrincipalFactory userClaimsPrincipalFactory, ITagFacetRepository facetRepository,
        ApplicationSettings appSettings,
        IServiceProvider serviceProvider, UserManager<AppUser> userManager)
        : base(appSettings, serviceProvider, userClaimsPrincipalFactory, userManager)
    {
        _logger = logger;
        _tagRepository = tagRepository;
        _facetRepository = facetRepository;
        _indexingUtility = indexingUtility;
        _facetIndexingUtility = facetIndexingUtility; 
    }

    public async Task Consume(ConsumeContext<FacetMergedMessage> context)
    {
        try
        {
            _logger.LogDebug("FacetMergedMessage: {0} < {1}", context.Message.RetainedFacetId,
                context.Message.RemovedFacetId);
            foreach (var tagId in context.Message.Tags) await UpdateIndex(tagId);
        }
        catch (Exception e)
        {
            _logger.LogError($"Could not process message: {context.Message.GetType().FullName}");
        }
    }

    public async Task Consume(ConsumeContext<FacetRemovedMessage> context)
    {
        try
        {
            _logger.LogDebug("FacetTagRemovedMessage: {0}", context.Message.FacetTagId);
            foreach (var tagId in context.Message.Tags) await RemoveFromIndex(tagId);
        }
        catch (Exception e)
        {
            _logger.LogError($"Could not process message: {context.Message.GetType().FullName}");
        }
    }

    public async Task Consume(ConsumeContext<FacetUpdatedMessage> context)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            _logger.LogDebug("FacetTagUpdatedMessage: {0}", context.Message.FacetTagId);
            var ambientContext = await GetAmbientContext(scope.ServiceProvider);
            var tags = await _tagRepository
                .GetAllAsync(ambientContext, new TagQuery { FacetId = context.Message.FacetTagId })
                .Select(_ => _.TagId).ToListAsync();
            foreach (var tag in tags)
            {
                // Parallelizing would have been great, but it caused "Too Many Clients" errors
                await UpdateIndex(tag);
            }
            await ambientContext.DatabaseContext.SaveChangesAsyncWithoutNotification();
        }
        catch (Exception e)
        {
            _logger.LogError($"Could not process message: {context.Message.GetType().FullName}");
        }
    }

    public async Task Consume(ConsumeContext<TagCreatedMessage> context)
    {
        try
        {
            _logger.LogDebug("TagCreatedMessage: {0}", context.Message.TagId);
            await AddToIndex(context.Message.TagId);
        }
        catch (Exception e)
        {
            _logger.LogError($"Could not process message: {context.Message.GetType().FullName}");
        }
    }

    public async Task Consume(ConsumeContext<TagMergedMessage> context)
    {
        try
        {
            _logger.LogDebug("TagMergedMessage: {0} < {1}", context.Message.RetainedTagId,
                context.Message.RemovedTagId);
            await UpdateIndex(context.Message.RetainedTagId);
            await RemoveFromIndex(context.Message.RemovedTagId);
        }
        catch (Exception e)
        {
            _logger.LogError($"Could not process message: {context.Message.GetType().FullName}");
        }
    }

    public async Task Consume(ConsumeContext<TagRemovedMessage> context)
    {
        try
        {
            _logger.LogDebug("TagRemovedMessage: {0}", context.Message.TagId);
            await RemoveFromIndex(context.Message.TagId);
        }
        catch (Exception e)
        {
            _logger.LogError($"Could not process message: {context.Message.GetType().FullName}");
        }
    }

    public async Task Consume(ConsumeContext<TagUpdatedMessage> context)
    {
        try
        {
            _logger.LogDebug("TagUpdatedMessage: {0}", context.Message.TagId);
            await UpdateIndex(context.Message.TagId);
        }
        catch (Exception e)
        {
            _logger.LogError($"Could not process message: {context.Message.GetType().FullName}");
        }
    }

    private async Task RemoveFromIndex(Guid tagId)
    {
        using var scope = _serviceProvider.CreateScope();
        using var ambientContext = await GetAmbientContext(scope.ServiceProvider);
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
        finally
        {
            ambientContext.Dispose();
        }
    }

    private async Task AddToIndex(Guid tagId)
    {
        using var scope = _serviceProvider.CreateScope();
        using var ambientContext = await GetAmbientContext(scope.ServiceProvider);
        try
        {
            var tag = await _tagRepository.GetAsync(ambientContext,
                tagId,
                new[]
                {
                    nameof(Tag.Facet),
                    "Documents", "Documents.Document"
                });
            _indexingUtility.Add(tag);
            
            await ambientContext.DatabaseContext.Tags.Where(t => t.TagId == tag.TagId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.LastIndexDate, DateTime.UtcNow));
            //tag.LastIndexDate = DateTime.UtcNow;
            //await ambientContext.DatabaseContext.SaveChangesAsyncWithoutNotification();
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
        finally
        {
            ambientContext.Dispose();
        }
    }
   
    private async Task UpdateIndex(Guid tagId)
    {   
        using var scope = _serviceProvider.CreateScope();
        using var ambientContext = await GetAmbientContext(scope.ServiceProvider);
        try
        {
            var tag = await _tagRepository.GetAsync(ambientContext,
                tagId,
                new[]
                {
                    nameof(Tag.Facet),
                    "Documents", "Documents.Document"
                });
            await UpdateIndex(tag, ambientContext);
            // await ambientContext.DatabaseContext.SaveChangesAsyncWithoutNotification();
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
        finally
        {
            ambientContext.Dispose();
        }
    }

    private async Task UpdateIndex(Tag tag, AmbientContext ambientContext)
    {
            _indexingUtility.Update(tag);
            
            await ambientContext.DatabaseContext.Tags.Where(t => t.TagId == tag.TagId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.LastIndexDate, DateTime.UtcNow));
            _logger.LogInformation("Index updated for the tag {0} ({1})", tag.TagId, tag.FriendlyName);
        
    }

}