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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using DocIntel.Core.Authorization;
using DocIntel.Core.Collectors;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Importers;
using DocIntel.Core.Messages;
using DocIntel.Core.Models;
using DocIntel.Core.Modules;
using DocIntel.Core.Repositories;
using DocIntel.Core.Services;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils;
using DocIntel.Core.Utils.Observables;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Synsharp.Telepath.Messages;

namespace DocIntel.Services.Cron
{
    public class CollectorWorker : DynamicContextConsumer
    {
        private readonly ILogger<CollectorWorker> _logger;

        private readonly IMapper _mapper;
        private readonly ModuleFactory _moduleFactory;
        private readonly TagUtility _tagUtility;
        private readonly IDocumentRepository _documentRepository;
        private readonly ISynapseRepository _observablesRepository;
        private readonly IObservablesUtility _observablesUtility;
        private readonly ICollectorRepository _collectorRepository;

        public CollectorWorker(ILogger<CollectorWorker> logger,
            AppUserClaimsPrincipalFactory userClaimsPrincipalFactory,
            IServiceProvider serviceProvider, 
            ApplicationSettings appSettings,
            UserManager<AppUser> userManager,
            ModuleFactory moduleFactory,
            IMapper mapper,
            TagUtility tagUtility,
            IDocumentRepository documentRepository,
            ISynapseRepository observablesRepository,
            IObservablesUtility observablesUtility, ICollectorRepository collectorRepository) 
            : base(appSettings, serviceProvider, userClaimsPrincipalFactory, userManager)
        {
            _logger = logger;
            _moduleFactory = moduleFactory;
            _mapper = mapper;
            _tagUtility = tagUtility;
            _documentRepository = documentRepository;
            _observablesRepository = observablesRepository;
            _observablesUtility = observablesUtility;
            _collectorRepository = collectorRepository;
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
                await Task.Delay(TimeSpan.FromMinutes(_appSettings.Schedule.CronFrequencyCheck), cancellationToken);
            }
        }

        private async Task ImportFeeds()
        {
            var collectorLastCollection = DateTime.UtcNow;

            using var scope = _serviceProvider.CreateScope();
            using var context = await GetAmbientContext(scope.ServiceProvider);
            var collectors = await _collectorRepository.GetAllAsync(context).ToListAsync();
            
            foreach (var collector in collectors)
            {
                var schedule = NCrontab.CrontabSchedule.Parse(collector.CronExpression);
                // Console.WriteLine($"updating {collector.LastCollection}");
                DateTime nextOccurrence = DateTime.MinValue;
                if (collector.LastCollection != null)
                    nextOccurrence = schedule.GetNextOccurrence((DateTime)collector.LastCollection);
                
                if (nextOccurrence < collectorLastCollection)
                {
                    using var collectorScope = _serviceProvider.CreateScope();
                    using var collectorContext = await GetAmbientContext(collectorScope.ServiceProvider);
                    await CollectFeed(collector, collectorContext);

                    // Console.WriteLine($"updating {collector.LastCollection}");
                    collector.LastCollection = collectorLastCollection;
                    await collectorContext.DatabaseContext.SaveChangesAsync();
                    await context.DatabaseContext.SaveChangesAsync();
                }
                else
                {
                    // No need to execute, wait for the next occurence. 
                }
            }
        }

        private async Task CollectFeed(Collector collector, AmbientContext collectorContext)
        {
            var lastCollection = collector.LastCollection;
            var limit = collector.Limit;

            var client = _moduleFactory.GetCollector(collector.Module, collector.CollectorName);
            // Console.WriteLine(collector.Module);
            // Console.WriteLine(collector.CollectorName);
            var collectorSettings = _moduleFactory.GetCollectorSettings(collector.Module, collector.CollectorName);
            Console.WriteLine(collectorSettings);
            var settings = collector.Settings.Deserialize(
                collectorSettings);

            await foreach (var report in client.Collect(collector, settings))
            {
                var document = _mapper.Map<Document>(report);

                document.SourceId = collector.SourceId;
                document.ClassificationId = collector.ClassificationId;
                document.ReleasableTo = collector.ReleasableTo;
                document.EyesOnly = collector.EyesOnly;
                document.RegisteredById = collector.UserId;
                document.LastModifiedById = collector.UserId;

                // Check if document is already known
                if (document.SourceId != null
                    && !string.IsNullOrEmpty(document.ExternalReference)
                    && collectorContext.DatabaseContext.Documents.Any(_ => _.SourceId == document.SourceId
                                                                           && _.ExternalReference ==
                                                                           document.ExternalReference))
                {
                    _logger.LogDebug(
                        $"Document '{document.ExternalReference}' from '{document.SourceId}' was already imported.");
                    // TODO Include a field version in the files
                    continue;
                }

                try
                {
                    var tags = _tagUtility.GetOrCreateTags(collectorContext, report.Tags);
                    if (collector.Tags?.Any() ?? false)
                        tags = tags.Union(collector.Tags).Distinct();

                    document = await _documentRepository.AddAsync(collectorContext,
                        document,
                        tags.ToArray());
                }
                catch (InvalidArgumentException e)
                {
                    Console.WriteLine(e.Message);
                    foreach (var error in e.Errors)
                    {
                        foreach(var error2 in error.Value)
                            Console.WriteLine(error.Key + ": " + error2);
                    }

                    throw;
                }

                if (collector.SkipInbox)
                {
                    document.MetaData = new Dictionary<string, JsonObject>();
                    var registrationMetadata = new RegistrationMetadata()
                    {
                        Auto = true
                    };
                    var stringJson = JsonSerializer.Serialize(registrationMetadata);
                    document.MetaData.Add("registration", JsonNode.Parse(stringJson) as JsonObject);
                }

                foreach (var file in report.Files)
                {
                    var f = _mapper.Map<DocumentFile>(file);
                    f.Document = document;

                    if (file.Content != null)
                    {
                        await using var stream = file.Content?.Stream();
                        if (stream != null)
                        {
                            using var memoryStream = new MemoryStream();
                            await stream.CopyToAsync(memoryStream);
                            f = await _documentRepository.AddFile(collectorContext, f, memoryStream);
                        }
                    }
                }

                if (report.Nodes != null & collector.ImportStructuredData)
                {
                    try
                    {
                        var view = await _observablesRepository.CreateView(document);
                        var structuredData = _mapper.Map<IEnumerable<SynapseNode>>(report.Nodes);
                        await _observablesUtility.AnnotateAsync(document, null, structuredData);
                        _logger.LogDebug($"Found {structuredData.Count()} observables");
                        await _observablesRepository.Add(structuredData, document, view);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Could not import structured data on document '{document.DocumentId}': {e.Message}");
                    }
                }

                await collectorContext.DatabaseContext.SaveChangesAsync();
            }
        }
    }
}