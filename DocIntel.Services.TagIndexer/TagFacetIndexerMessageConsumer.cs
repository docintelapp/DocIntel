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

public class TagFacetIndexerMessageConsumer :
    DynamicContextConsumer,
    IConsumer<FacetUpdatedMessage>,
    IConsumer<FacetRemovedMessage>,
    IConsumer<FacetCreatedMessage>,
    IConsumer<FacetMergedMessage>
{
    // TODO Move to EventIds
    public static EventId Unauthorized = new(31001, "authorization-fail");
    public static EventId EntityNotFound = new(31002, "tag-not-found");
    private readonly ITagFacetIndexingUtility _facetIndexingUtility;
    private readonly ITagFacetRepository _facetRepository;
    private readonly ILogger<TagFacetIndexerMessageConsumer> _logger;
    private readonly ITagRepository _tagRepository;

    public TagFacetIndexerMessageConsumer(ILogger<TagFacetIndexerMessageConsumer> logger,
        ITagRepository tagRepository,
        ITagFacetIndexingUtility facetIndexingUtility,
        AppUserClaimsPrincipalFactory userClaimsPrincipalFactory, ITagFacetRepository facetRepository,
        ApplicationSettings appSettings,
        IServiceProvider serviceProvider, UserManager<AppUser> userManager)
        : base(appSettings, serviceProvider, userClaimsPrincipalFactory, userManager)
    {
        _logger = logger;
        _tagRepository = tagRepository;
        _facetRepository = facetRepository;
        _facetIndexingUtility = facetIndexingUtility; 
    }

    public async Task Consume(ConsumeContext<FacetMergedMessage> context)
    {
        try
        {
            _logger.LogDebug("FacetMergedMessage: {0} < {1}", context.Message.RetainedFacetId,
                context.Message.RemovedFacetId);
            await UpdateFacetIndex(context.Message.RetainedFacetId);
            await RemoveFromFacetIndex(context.Message.RemovedFacetId);
        }
        catch (Exception e)
        {
            _logger.LogError($"Could not process message: {context.Message.GetType().FullName}");
        }
    }

    public async Task Consume(ConsumeContext<FacetCreatedMessage> context)
    {
        try
        {
            _logger.LogDebug("FacetTagCreatedMessage: {0}", context.Message.FacetTagId);
            await AddToFacetIndex(context.Message.FacetTagId);
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
            await RemoveFromFacetIndex(context.Message.FacetTagId);
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
            _logger.LogDebug("FacetTagUpdatedMessage: {0}", context.Message.FacetTagId);
            var ambientContext = await GetAmbientContext();
            var tags = await _tagRepository
                .GetAllAsync(ambientContext, new TagQuery { FacetId = context.Message.FacetTagId })
                .Select(_ => _.TagId).ToListAsync();
            ambientContext.Dispose();
            await UpdateFacetIndex(context.Message.FacetTagId);
        }
        catch (Exception e)
        {
            _logger.LogError($"Could not process message: {context.Message.GetType().FullName}");
        }
    }

    private async Task RemoveFromFacetIndex(Guid facetId)
    {
        using var ambientContext = await GetAmbientContext();
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
        finally
        {
            ambientContext.Dispose();
        }
    }

    private async Task AddToFacetIndex(Guid facetId)
    {
        using var ambientContext = await GetAmbientContext();
        try
        {
            var facet = await _facetRepository.GetAsync(ambientContext,
                facetId);
            _facetIndexingUtility.Add(facet);
            facet.LastIndexDate = DateTime.UtcNow;
            await ambientContext.DatabaseContext.SaveChangesAsync();
            _logger.LogInformation("Index updated for the facet {0}", facet.FacetId);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                Unauthorized,
                new LogEvent(
                        $"User '{ambientContext.CurrentUser.UserName}' attempted to retrieve facet without legitimate rights.")
                    .AddUser(ambientContext.CurrentUser)
                    .AddProperty("facet.id", facetId),
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
                    .AddProperty("facet.id", facetId),
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
                    .AddProperty("facet.id", facetId)
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

    private async Task UpdateFacetIndex(Guid facetId)
    {
        using var ambientContext = await GetAmbientContext();
        try
        {
            var facet = await _facetRepository.GetAsync(ambientContext,
                facetId);
            _facetIndexingUtility.Update(facet);
            facet.LastIndexDate = DateTime.UtcNow;
            await ambientContext.DatabaseContext.SaveChangesAsync();
            _logger.LogInformation("Index updated for the facet {0}", facet.FacetId);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                Unauthorized,
                new LogEvent(
                        $"User '{ambientContext.CurrentUser.UserName}' attempted to retrieve facet without legitimate rights.")
                    .AddUser(ambientContext.CurrentUser)
                    .AddProperty("facet.id", facetId),
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
                    .AddProperty("facet.id", facetId),
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
                    .AddProperty("facet.id", facetId)
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
}