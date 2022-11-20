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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DocIntel.Core.Authorization;
using DocIntel.Core.Messages;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;

using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

using DocIntel.Core.Scrapers;
using DocIntel.Core.Services;
using DocIntel.Core.Settings;

namespace DocIntel.Services.Scraper
{
    public class ScraperConsumer :
        DynamicContextConsumer,
        IConsumer<URLSubmittedMessage>
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IScraperRepository _scraperRepository;
        private readonly ILogger _logger;

        public ScraperConsumer(IDocumentRepository documentRepository,
            IServiceProvider serviceProvider,
            ILogger<ScraperConsumer> logger,
            IScraperRepository scraperRepository, 
            ApplicationSettings appSettings,
            AppUserClaimsPrincipalFactory userClaimsPrincipalFactory)
            : base(appSettings, serviceProvider, userClaimsPrincipalFactory)
        {
            _documentRepository = documentRepository;
            _logger = logger;
            _scraperRepository = scraperRepository;
        }

        public async Task ConsumeBacklogAsync()
        {
            
            while (await _documentRepository.GetSubmittedDocuments(GetContext(),
                    _ => _.Include(__ => __.Classification)
                        .Include(__ => __.ReleasableTo)
                        .Include(__ => __.EyesOnly)
                        .Where(__ => __.Status == SubmissionStatus.Submitted))
                .CountAsync() > 0)
            {
                var submitted = await _documentRepository.GetSubmittedDocuments(GetContext(),
                        _ => _.Include(__ => __.Classification)
                            .Include(__ => __.ReleasableTo)
                            .Include(__ => __.EyesOnly)
                            .Where(__ => __.Status == SubmissionStatus.Submitted)
                            .OrderByDescending(__ => __.SubmissionDate))
                    .FirstOrDefaultAsync();
                var result = await Scrape(submitted);
            }
        }

        private AmbientContext GetContext(string registeredBy = null)
        {
            var userClaimsPrincipalFactory =
                (IUserClaimsPrincipalFactory<AppUser>) _serviceProvider.GetService(
                    typeof(IUserClaimsPrincipalFactory<AppUser>));
            var options =
                (DbContextOptions<DocIntelContext>) _serviceProvider.GetService(
                    typeof(DbContextOptions<DocIntelContext>));
            var context = new DocIntelContext(options,
                (ILogger<DocIntelContext>) _serviceProvider.GetService(typeof(ILogger<DocIntelContext>)));

            var automationUser = !string.IsNullOrEmpty(registeredBy)
                ? context.Users.AsNoTracking().FirstOrDefault(_ => _.Id == registeredBy)
                : context.Users.AsNoTracking().FirstOrDefault(_ => _.UserName == _appSettings.AutomationAccount);
            
            if (automationUser == null)
                throw new InvalidOperationException("Could not find user to run consumer");

            if (userClaimsPrincipalFactory != null)
            {
                var claims = userClaimsPrincipalFactory.CreateAsync(automationUser).Result;
                return new AmbientContext {
                    DatabaseContext = context,
                    Claims = claims,
                    CurrentUser = automationUser
                };
            }

            throw new InvalidOperationException(
                "Could not create instance of `IUserClaimsPrincipalFactory<AppUser>`");
        }

        public Task Consume(ConsumeContext<URLSubmittedMessage> c)
        {
            _logger.LogDebug($"Received a new message: {c.Message.SubmissionId}");
            var urlSubmittedMessage = c.Message;
            Scrape(urlSubmittedMessage).Wait();
            return Task.CompletedTask;
        }

        private async Task Scrape(URLSubmittedMessage urlSubmittedMessage)
        {   
            var context = GetContext();
            var submission = await _documentRepository.GetSubmittedDocument(context, 
                urlSubmittedMessage.SubmissionId,
                _ => _.Include(__ => __.Classification).Include(__ => __.ReleasableTo).Include(__ => __.EyesOnly));
            await Scrape(submission);
        }

        private async Task<bool> Scrape(SubmittedDocument submission)
        {  
            submission.IngestionDate = DateTime.UtcNow;
            
            var context = GetContext();
            _logger.LogDebug($"Scrape: {submission.URL}");
            var exists = await _documentRepository.GetAllAsync(context,
                    _ => _.Where(__ =>
                        __.SourceUrl == submission.URL | __.Files.Any(___ => ___.SourceUrl == submission.URL)))
                .ToArrayAsync();
            
            // If we have an existing document with a priority higher, we can skip the scraper.
            if (exists.Any(_ => _.MetaData.Value<int>("ScraperPriority") >= submission.Priority)) {
                _documentRepository.DeleteSubmittedDocument(context, submission.SubmittedDocumentId, SubmissionStatus.Duplicate);
                await context.DatabaseContext.SaveChangesAsync();
                return true;
            }

            bool scraped = false;
            var scrapers = await _scraperRepository.GetAllAsync(context,
                    _ => _.Where(__ => __.Enabled))
                .ToArrayAsync();
            foreach (var scraper in scrapers.OrderBy(_ => _.Position))
            {
                
                var instance = await ScraperFactory.CreateScraper(scraper, _serviceProvider, context);
                foreach (var pattern in instance.Patterns)
                {
                    if (Regex.IsMatch(submission.URL, pattern))
                    {
                        try
                        {
                            if (!await instance.Scrape(submission))
                            {
                                scraped = true;
                                break;
                            }

                        } catch (Exception e)
                        {
                            do
                            {
                                _logger.LogDebug(
                                    $"Could not scrape content: {e.GetType().FullName}({e.Message})\n{e?.StackTrace}");
                            } while ((e = e.InnerException) != null);
                            
                        }
                    }
                }
            }

            if (scraped)
            {
                _documentRepository.DeleteSubmittedDocument(context, submission.SubmittedDocumentId);
                await context.DatabaseContext.SaveChangesAsync();
            }
            else
            {
                submission.Status = SubmissionStatus.Error;
                await context.DatabaseContext.SaveChangesAsync();
            }
            
            _logger.LogDebug(
                $"No scraper found for {submission.URL}");

            return scraped;
        }
    }
}