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
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.Indexation;

using MassTransit;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocIntel.RabbitMQSourceIndexer
{
    public class SourceIndexer :
        IConsumer<SourceCreatedMessage>,
        IConsumer<SourceUpdatedMessage>,
        IConsumer<SourceRemovedMessage>,
        IConsumer<SourceMergedMessage>
    {
        // TODO Move to EventIds (names are currently conflicting)
        public static EventId Unauthorized = new(30001, "authorization-fail");
        public static EventId EntityNotFound = new(30002, "source-not-found");
        private readonly ApplicationSettings _appSettings;
        private readonly ISourceIndexingUtility _indexingUtility;
        private readonly ILogger<SourceIndexer> _logger;
        private readonly ISourceRepository _sourceRepository;
        private readonly IUserClaimsPrincipalFactory<AppUser> _userClaimsPrincipalFactory;
        private readonly IServiceProvider _serviceProvider;

        public SourceIndexer(ILogger<SourceIndexer> logger,
            ISourceRepository sourceRepository,
            ISourceIndexingUtility indexingUtility,
            IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory, ApplicationSettings appSettings, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _sourceRepository = sourceRepository;
            _indexingUtility = indexingUtility;

            _logger.LogDebug("EventConsumer created");
            _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
            _appSettings = appSettings;
            _serviceProvider = serviceProvider;
        }

        public async Task Consume(ConsumeContext<SourceCreatedMessage> context)
        {
            var ambientContext = GetAmbientContext();
            await AddToIndex(context.Message.SourceId, ambientContext);
        }

        public async Task Consume(ConsumeContext<SourceMergedMessage> context)
        {
            var ambientContext = GetAmbientContext();
            await UpdateIndex(context.Message.PrimarySourceId, ambientContext);
            RemoveFromIndex(context.Message.SecondarySourceId, ambientContext);
        }

        public Task Consume(ConsumeContext<SourceRemovedMessage> context)
        {
            var ambientContext = GetAmbientContext();
            RemoveFromIndex(context.Message.SourceId, ambientContext);
            return Task.CompletedTask;
        }

        public async Task Consume(ConsumeContext<SourceUpdatedMessage> context)
        {
            var ambientContext = GetAmbientContext();
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