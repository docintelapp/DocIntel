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
using DocIntel.Core.Utils.Thumbnail;

using MassTransit;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocIntel.Services.Thumbnailer
{
    public class Thumbnailer :
        IConsumer<DocumentCreatedMessage>,
        IConsumer<DocumentUpdatedMessage>
    {
        // TODO Move to EventIDs (currently conflicting)
        public static EventId Unauthorized = new(30001, "authorization-fail");
        public static EventId EntityNotFound = new(30002, "document-not-found");
        private readonly ApplicationSettings _appSettings;
        private readonly IDocumentRepository _documentRepository;
        private readonly ILogger<Thumbnailer> _logger;
        private readonly IUserClaimsPrincipalFactory<AppUser> _userClaimsPrincipalFactory;
        private readonly IThumbnailUtility _utility;
        private readonly IServiceProvider _serviceProvider;

        public Thumbnailer(ILogger<Thumbnailer> logger,
            IDocumentRepository documentRepository,
            IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory,
            IThumbnailUtility utility, ApplicationSettings appSettings, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _documentRepository = documentRepository;
            _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
            _utility = utility;
            _appSettings = appSettings;
            _serviceProvider = serviceProvider;
        }

        public async Task Consume(ConsumeContext<DocumentCreatedMessage> context)
        {
            _logger.LogDebug("DocumentCreatedMessage: {0}", context.Message.DocumentId);
            await Thumbnail(context.Message.DocumentId, GetAmbientContext());
        }

        public async Task Consume(ConsumeContext<DocumentUpdatedMessage> context)
        {
            _logger.LogDebug("DocumentUpdatedMessage: {0}", context.Message.DocumentId);
            await Thumbnail(context.Message.DocumentId, GetAmbientContext());
        }

        private async Task Thumbnail(Guid documentId, AmbientContext ambientContext)
        {
            try
            {
                var document =
                    await _documentRepository.GetAsync(ambientContext, documentId, new[] {"Files", "Files.Document"});
                await _utility.GenerateThumbnail(ambientContext, document);
                await ambientContext.DatabaseContext.SaveChangesAsync();
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