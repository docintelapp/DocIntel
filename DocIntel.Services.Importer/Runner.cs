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
using DocIntel.Core.Authorization;
using DocIntel.Core.Importers;
using DocIntel.Core.Messages;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.EFCore;
using DocIntel.Core.Services;
using DocIntel.Core.Settings;

using MassTransit;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocIntel.Services.Importer
{
    public class Runner : DynamicContextConsumer
    {
        private readonly IPublishEndpoint _busClient;
        private readonly ILogger<DocIntelContext> _contextLogger;
        private readonly IIncomingFeedRepository _feedRepository;
        private readonly ILogger<Runner> _logger;
        private readonly UserManager<AppUser> _userManager;

        public Runner(IIncomingFeedRepository feedRepository,
            ILogger<Runner> logger,
            ILogger<DocIntelContext> contextLogger,
            AppUserClaimsPrincipalFactory userClaimsPrincipalFactory,
            IServiceProvider serviceProvider, IPublishEndpoint busClient,
            ApplicationSettings appSettings, UserManager<AppUser> userManager) 
            : base(appSettings, serviceProvider, userClaimsPrincipalFactory, userManager)
        {
            _feedRepository = feedRepository;
            _contextLogger = contextLogger;
            _logger = logger;
            _busClient = busClient;
            _userManager = userManager;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Will collect feeds...");
                    await ImportFeeds();
                    _logger.LogInformation("Sleeping...");
                }
                catch (Exception e)
                {
                    _logger.LogError(e.GetType().FullName);
                    _logger.LogError(e.Message + "\n" + e.StackTrace);
                }
                await Task.Delay(TimeSpan.FromMinutes(_appSettings.Schedule.ImporterFrequencyCheck), cancellationToken);
            }
        }

        private async Task ImportFeeds()
        {
            _logger.LogDebug($"Start import feeds");
            var options =
                (DbContextOptions<DocIntelContext>) _serviceProvider.GetService(
                    typeof(DbContextOptions<DocIntelContext>));
            var documentRepository = (IDocumentRepository) _serviceProvider.GetService(typeof(IDocumentRepository));
            if (documentRepository == null) throw new ArgumentNullException(nameof(documentRepository));

            using var feedScope = _serviceProvider.CreateScope();
            using var feedContext = await GetAmbientContext(feedScope.ServiceProvider);

            var feeds = await _feedRepository.GetAllAsync(feedContext,
                importer => importer.Where(i => i.Status == ImporterStatus.Enabled).Include(i => i.Classification)
                    .Include(i => i.ReleasableTo).Include(i => i.EyesOnly)).ToArrayAsync();

            foreach (var feed in feeds)
            {
                await CollectFeed(feed, feedContext);
            }
        }

        private async Task CollectFeed(Core.Models.Importer feed, AmbientContext feedContext)
        {
            using var importerScope = _serviceProvider.CreateScope();
            using var importerContext = await GetAmbientContext(importerScope.ServiceProvider);

            var now = DateTime.UtcNow;
            if (feed.LastCollection == null || now - feed.LastCollection > feed.CollectionDelay)
            {
                _logger.LogDebug($"Collecting from {feed.Name} importer...");
                var importer = await ImporterFactory.CreateImporter(feed, _serviceProvider, importerContext);
                if (importer != null)
                {
                    await foreach (var message in importer.PullAsync(feed.LastCollection, feed.Limit))
                    {
                        await CollectItem(feed, message);
                    }

                    feed.LastCollection = now;
                    _logger.LogDebug($"Collecting from {feed.Name} importer...done");
                }
                else
                {
                    feed.Status = ImporterStatus.Error;
                    _logger.LogDebug($"Collecting from {feed.Name} importer failed.");
                }
            }
            else
            {
                _logger.LogDebug(
                    $"Skip {feed.Name} importer (next import in {feed.CollectionDelay - (now - feed.LastCollection)})");
            }
            
            await feedContext.DatabaseContext.SaveChangesAsync();
        }

        private async Task CollectItem(Core.Models.Importer feed, SubmittedDocument message)
        {
            using var messageScope = _serviceProvider.CreateScope();
            using var messageContext = await GetAmbientContext(messageScope.ServiceProvider);

            var documentRepository = _serviceProvider.GetRequiredService<IDocumentRepository>();
            
            var previousSubmissions =
                documentRepository.GetSubmittedDocuments(messageContext,
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
                    await documentRepository.SubmitDocument(messageContext, message);

                await messageContext.DatabaseContext.SaveChangesAsync();
                await _busClient.Publish(new URLSubmittedMessage
                    { SubmissionId = submittedDocument.SubmittedDocumentId });
            }
            else
            {
                _logger.LogDebug($"Already known... {message.URL}");
            }
        }
    }
}