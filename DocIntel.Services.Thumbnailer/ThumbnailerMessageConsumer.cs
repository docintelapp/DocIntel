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
using System.Threading.Tasks;
using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Logging;
using DocIntel.Core.Messages;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Services;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.Thumbnail;

using MassTransit;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocIntel.Services.Thumbnailer
{
    public class ThumbnailerMessageConsumer :
        DynamicContextConsumer,
        IConsumer<DocumentCreatedMessage>,
        IConsumer<DocumentUpdatedMessage>
    {
        // TODO Move to EventIDs (currently conflicting)
        public static EventId Unauthorized = new(30001, "authorization-fail");
        public static EventId EntityNotFound = new(30002, "document-not-found");
        
        private readonly IDocumentRepository _documentRepository;
        private readonly ILogger<ThumbnailerMessageConsumer> _logger;
        private readonly IThumbnailUtility _utility;

        public ThumbnailerMessageConsumer(ILogger<ThumbnailerMessageConsumer> logger,
            IDocumentRepository documentRepository,
            AppUserClaimsPrincipalFactory userClaimsPrincipalFactory,
            IThumbnailUtility utility, ApplicationSettings appSettings, IServiceProvider serviceProvider,
            UserManager<AppUser> userManager)
            : base(appSettings, serviceProvider, userClaimsPrincipalFactory, userManager)
        {
            _logger = logger;
            _documentRepository = documentRepository;
            _utility = utility;
        }

        public async Task Consume(ConsumeContext<DocumentCreatedMessage> context)
        {
            _logger.LogDebug("DocumentCreatedMessage: {0}", context.Message.DocumentId);
            await Thumbnail(context.Message.DocumentId);
        }

        public async Task Consume(ConsumeContext<DocumentUpdatedMessage> context)
        {
            _logger.LogDebug("DocumentUpdatedMessage: {0}", context.Message.DocumentId);
            await Thumbnail(context.Message.DocumentId);
        }

        private async Task Thumbnail(Guid documentId)
        {
            using var scope = _serviceProvider.CreateScope();
            using var ambientContext = await GetAmbientContext(scope.ServiceProvider);
            try
            {
                var document =
                    await _documentRepository.GetAsync(ambientContext, documentId, new[] {"Files"});
                if (document.Status == DocumentStatus.Registered) {
                    await _utility.GenerateThumbnail(ambientContext, document);
                    await ambientContext.DatabaseContext.SaveChangesAsync();
                }
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    Unauthorized,
                    new LogEvent(
                            $"User '{ambientContext.CurrentUser.UserName}' attempted to retreive document without legitimate rights.")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("document.id", documentId),
                    null,
                    LogEvent.Formatter);
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EntityNotFound,
                    new LogEvent(
                            $"User '{ambientContext.CurrentUser.UserName}' attempted to retreive a non-existing document.")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("document.id", documentId),
                    null,
                    LogEvent.Formatter);
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Warning,
                    EntityNotFound,
                    new LogEvent($"User '{ambientContext.CurrentUser.UserName}' could not thumbnail the document: " +
                                 e.GetType() + " (" + e.Message + ")")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("document.id", documentId)
                        .AddException(e),
                    null,
                    LogEvent.Formatter);
                _logger.LogDebug(e.StackTrace);
            }
        }
    }
}