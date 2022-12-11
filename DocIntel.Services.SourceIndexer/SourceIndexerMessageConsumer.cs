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
using System.Threading.Tasks;
using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Logging;
using DocIntel.Core.Messages;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Services;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.Indexation;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace DocIntel.Services.SourceIndexer;

public class SourceIndexerMessageConsumer :
    DynamicContextConsumer,
    IConsumer<SourceCreatedMessage>,
    IConsumer<SourceUpdatedMessage>,
    IConsumer<SourceRemovedMessage>,
    IConsumer<SourceMergedMessage>
{
    // TODO Move to EventIds (names are currently conflicting)
    public static EventId Unauthorized = new(30001, "authorization-fail");
    public static EventId EntityNotFound = new(30002, "source-not-found");
    private readonly ISourceIndexingUtility _indexingUtility;
    private readonly ILogger<SourceIndexerMessageConsumer> _logger;
    private readonly ISourceRepository _sourceRepository;

    public SourceIndexerMessageConsumer(ILogger<SourceIndexerMessageConsumer> logger,
        ISourceRepository sourceRepository,
        ISourceIndexingUtility indexingUtility,
        ApplicationSettings appSettings, IServiceProvider serviceProvider,
        AppUserClaimsPrincipalFactory userClaimsPrincipalFactory, UserManager<AppUser> userManager)
        : base(appSettings, serviceProvider, userClaimsPrincipalFactory, userManager)
    {
        _logger = logger;
        _sourceRepository = sourceRepository;
        _indexingUtility = indexingUtility;
    }

    public async Task Consume(ConsumeContext<SourceCreatedMessage> context)
    {
        using var ambientContext = await GetAmbientContext();
        await AddToIndex(context.Message.SourceId, ambientContext);
    }

    public async Task Consume(ConsumeContext<SourceMergedMessage> context)
    {
        using var ambientContext = await GetAmbientContext();
        await UpdateIndex(context.Message.PrimarySourceId, ambientContext);
        RemoveFromIndex(context.Message.SecondarySourceId, ambientContext);
    }

    public async Task Consume(ConsumeContext<SourceRemovedMessage> context)
    {
        var ambientContext = await GetAmbientContext();
        RemoveFromIndex(context.Message.SourceId, ambientContext);
    }

    public async Task Consume(ConsumeContext<SourceUpdatedMessage> context)
    {
        using var ambientContext = await GetAmbientContext();
        await UpdateIndex(context.Message.SourceId, ambientContext);
    }

    private void RemoveFromIndex(Guid sourceId, AmbientContext ambientContext)
    {
        try
        {
            _indexingUtility.Remove(sourceId);
            _logger.LogInformation($"Document '{sourceId}' removed from index.");
        }
        catch (Exception e)
        {
            _logger.Log(LogLevel.Warning,
                EntityNotFound,
                new LogEvent($"User '{ambientContext.CurrentUser.UserName}' could not remove the source '" +
                             sourceId + "' from index: " + e.GetType() + " (" + e.Message + ")")
                    .AddUser(ambientContext.CurrentUser)
                    .AddProperty("source.id", sourceId)
                    .AddException(e),
                null,
                LogEvent.Formatter);
            _logger.LogDebug(e.StackTrace);
        }
    }

    private async Task AddToIndex(Guid sourceId, AmbientContext ambientContext)
    {
        try
        {
            var source = await _sourceRepository.GetAsync(ambientContext,
                sourceId,
                new string[] { });

            _indexingUtility.Add(source);
            source.LastIndexDate = DateTime.UtcNow;
            await ambientContext.DatabaseContext.SaveChangesAsync();
            _logger.LogInformation("Index updated for the source '{0}'", source.SourceId);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                Unauthorized,
                new LogEvent(
                        $"User '{ambientContext.CurrentUser.UserName}' attempted to retreive source without legitimate rights.")
                    .AddUser(ambientContext.CurrentUser)
                    .AddProperty("source.id", sourceId),
                null,
                LogEvent.Formatter);
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EntityNotFound,
                new LogEvent(
                        $"User '{ambientContext.CurrentUser.UserName}' attempted to retreive a non-existing source.")
                    .AddUser(ambientContext.CurrentUser)
                    .AddProperty("source.id", sourceId),
                null,
                LogEvent.Formatter);
        }
        catch (Exception e)
        {
            _logger.Log(LogLevel.Warning,
                EntityNotFound,
                new LogEvent(
                        $"User '{ambientContext.CurrentUser.UserName}' could not add the source '{sourceId}' to the index: {e.GetType()} ({e.Message})")
                    .AddUser(ambientContext.CurrentUser)
                    .AddProperty("source.id", sourceId)
                    .AddException(e),
                null,
                LogEvent.Formatter);
            _logger.LogDebug(e.StackTrace);
        }
    }

    private async Task UpdateIndex(Guid sourceId, AmbientContext ambientContext)
    {
        try
        {
            var source = await _sourceRepository.GetAsync(ambientContext,
                sourceId,
                new string[] { });

            _indexingUtility.Update(source);
            source.LastIndexDate = DateTime.UtcNow;
            await ambientContext.DatabaseContext.SaveChangesAsync();
            _logger.LogInformation("Index updated for the source '{0}'", source.SourceId);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                Unauthorized,
                new LogEvent(
                        $"User '{ambientContext.CurrentUser.UserName}' attempted to retreive source without legitimate rights.")
                    .AddUser(ambientContext.CurrentUser)
                    .AddProperty("source.id", sourceId),
                null,
                LogEvent.Formatter);
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EntityNotFound,
                new LogEvent(
                        $"User '{ambientContext.CurrentUser.UserName}' attempted to retreive a non-existing source.")
                    .AddUser(ambientContext.CurrentUser)
                    .AddProperty("source.id", sourceId),
                null,
                LogEvent.Formatter);
        }
        catch (Exception e)
        {
            _logger.Log(LogLevel.Warning,
                EntityNotFound,
                new LogEvent($"User '{ambientContext.CurrentUser.UserName}' could not index the source: " +
                             e.GetType() + " (" + e.Message + ")")
                    .AddUser(ambientContext.CurrentUser)
                    .AddProperty("source.id", sourceId)
                    .AddException(e),
                null,
                LogEvent.Formatter);
            _logger.LogDebug(e.StackTrace);
        }
    }
}