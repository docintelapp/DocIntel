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
using System.Threading;
using System.Threading.Tasks;

using DocIntel.Core.Importers;
using DocIntel.Core.Messages;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;

using MassTransit;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocIntel.Services.Importer
{
    public class Runner
    {
        private readonly ApplicationSettings _appSettings;
        private readonly IPublishEndpoint _busClient;
        private readonly ILogger<DocIntelContext> _contextLogger;
        private readonly IIncomingFeedRepository _feedRepository;
        private readonly ILogger<Runner> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IUserClaimsPrincipalFactory<AppUser> _userClaimsPrincipalFactory;

        public Runner(IIncomingFeedRepository feedRepository,
            ILogger<Runner> logger,
            ILogger<DocIntelContext> contextLogger,
            IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory,
            IServiceProvider serviceProvider, IPublishEndpoint busClient,
            ApplicationSettings appSettings)
        {
            _feedRepository = feedRepository;
            _contextLogger = contextLogger;
            _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _busClient = busClient;
            _appSettings = appSettings;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await ImportFeeds();
                _logger.LogInformation("Sleeping...");
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
        }

        private async Task ImportFeeds()
        {
            var options =
                (DbContextOptions<DocIntelContext>) _serviceProvider.GetService(
                    typeof(DbContextOptions<DocIntelContext>));
            var documentRepository = (IDocumentRepository) _serviceProvider.GetService(typeof(IDocumentRepository));
            if (documentRepository == null) throw new ArgumentNullException(nameof(documentRepository));
            var context = new DocIntelContext(options, _contextLogger);

            var automationUser = context.Users.AsNoTracking().FirstOrDefault(_ => _.UserName == _appSettings.AutomationAccount);
            if (automationUser == null)
                return;

            var claims = _userClaimsPrincipalFactory.CreateAsync(automationUser).Result;
            var ambientContext = new AmbientContext
            {
                DatabaseContext = context,
                Claims = claims,
                CurrentUser = automationUser
            };

            var feeds = await _feedRepository.GetAllAsync(ambientContext,
                importer => importer.Where(i => i.Status == ImporterStatus.Enabled).Include(i => i.Classification)
                    .Include(i => i.ReleasableTo).Include(i => i.EyesOnly)).ToArrayAsync();

            foreach (var feed in feeds)
            {
                var now = DateTime.UtcNow;
                if (feed.LastCollection == null || now - feed.LastCollection > feed.CollectionDelay)
                {
                    _logger.LogDebug($"Collecting from {feed.Name} importer...");
                    var importer = await ImporterFactory.CreateImporter(feed, _serviceProvider, ambientContext);
                    if (importer != null)
                    {
                        await foreach (var message in importer.PullAsync(feed.LastCollection, feed.Limit))
                        {
                            var previousSubmissions =
                                documentRepository.GetSubmittedDocuments(ambientContext,
                                    _ => _.Where(__ => __.URL == message.URL));

                            if (!await previousSubmissions.AnyAsync())
                            {
                                _logger.LogDebug($"Sending message for URL: {message.URL}");
                                message.SubmitterId = feed.FetchingUserId;
                                message.SubmissionDate = DateTime.UtcNow;
                                message.ImporterId = feed.ImporterId;
                                message.Priority = feed.Priority;
                                message.OverrideClassification = feed.OverrideClassification;
                                message.OverrideReleasableTo = feed.OverrideReleasableTo;
                                message.OverrideEyesOnly = feed.OverrideEyesOnly;

                                if (feed.OverrideClassification)
                                {
                                    _logger.LogDebug("ReleasableTo: " +
                                                     string.Join(", ", feed.ReleasableTo.Select(_ => _.Name)));
                                    _logger.LogDebug(
                                        "EyesOnly: " + string.Join(", ", feed.EyesOnly.Select(_ => _.Name)));
                                    message.Classification = feed.Classification;
                                }

                                if (feed.OverrideReleasableTo) message.ReleasableTo = feed.ReleasableTo;

                                if (feed.OverrideEyesOnly) message.EyesOnly = feed.EyesOnly;

                                var submittedDocument =
                                    await documentRepository.SubmitDocument(ambientContext, message);
                                await _busClient.Publish(new URLSubmittedMessage
                                    {SubmissionId = submittedDocument.SubmittedDocumentId});
                            }
                            else
                            {
                                _logger.LogDebug($"Already known... {message.URL}");
                            }
                        }

                        feed.LastCollection = now;
                        await ambientContext.DatabaseContext.SaveChangesAsync();
                    }
                    else
                    {
                        feed.Status = ImporterStatus.Error;
                    }
                }
                else
                {
                    _logger.LogDebug(
                        $"Skip {feed.Name} importer (next import in {feed.CollectionDelay - (now - feed.LastCollection)})");
                }
            }

            await context.SaveChangesAsync();
        }
    }
}