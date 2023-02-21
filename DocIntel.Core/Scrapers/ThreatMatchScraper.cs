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
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using DocIntel.Core.Importers;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Settings;
using DocIntel.Integrations.ThreatMatch;

using Microsoft.Extensions.Logging;

namespace DocIntel.Core.Scrapers
{
    [Scraper("87c53591-7afe-47d8-b8f3-2f5e49a08136",
        Name = "ThreatMatch",
        Description = "Scraper for ThreatMatch URI",
        Patterns = new[]
        {
            @"https?://eu.threatmatch.com/app/reports/view/[0-9]+"
        })]
    public class ThreatMatchScraper : DefaultScraper
    {
        private readonly ILogger<ThreatConnectScraper> _logger;
        private readonly Scraper _scraper;
        private readonly ApplicationSettings _settings;
        public override bool HasSettings => true;
        public ThreatMatchScraper(Scraper scraper, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _logger = (ILogger<ThreatConnectScraper>) serviceProvider.GetService(typeof(ILogger<ThreatConnectScraper>));
            _settings = (ApplicationSettings) serviceProvider.GetService(typeof(ApplicationSettings));
            _scraper = scraper;
        }

        public override async Task<bool> Scrape(SubmittedDocument message)
        {
            var scraperSettings = _scraper.Settings.ToObject<ThreatMatchSettings>();
            Init();
            var context = GetContextAsync();
            var match = Regex.Match(message.URL, @"https?://eu.threatmatch.com/app/reports/view/([0-9]+)");
            var reportId = match.Groups[1].ToString();

            var client = new APIClient(scraperSettings.Username, scraperSettings.APIKey, proxy: new WebProxy("http://" + _settings.Proxy + "/", true, new string[] { _settings.NoProxy }));
            await ImportReport(message, client, int.Parse(reportId), await context);
            return false;
        }

        private async Task ImportReport(SubmittedDocument message, APIClient client, int articleGUID,
            AmbientContext context)
        {
            var report = client.Reports.GetReport(articleGUID);

            if (report.Published)
            {
                Document document;
                var exists = await _documentRepository.ExistsAsync(context, new DocumentQuery
                {
                    SourceUrl = message.URL
                });

                if (exists)
                {
                    _logger.LogInformation($"Document for {message.URL} already exists, updating the document.");
                    _documentRepository.DeleteSubmittedDocument(context, message.SubmittedDocumentId, SubmissionStatus.Duplicate);
                    await context.DatabaseContext.SaveChangesAsync();
                    return;
                }

                document = new Document
                {
                    Title = report.Title,
                    DocumentDate = report.PublishedAt
                };

                document = await AddAsync(_scraper, context, document, message);
                _logger.LogDebug("Document created... " + document.DocumentId);

                var documentFile = new DocumentFile
                {
                    Document = document,
                    MimeType = "application/pdf",
                    Filename = report.Slug + ".pdf",
                    Title = "Report",
                    ClassificationId = _classificationRepository.GetDefault(context)?.ClassificationId,
                    DocumentDate = report.PublishedAt,
                    SourceUrl = message.URL,
                    Visible = true,
                    Preview = true
                };

                var stream = client.Reports.DownloadReport(articleGUID);
                stream.Position = 0;
                documentFile = await AddAsync(_scraper, context, documentFile, stream);
            }

            _documentRepository.DeleteSubmittedDocument(context, message.SubmittedDocumentId);
            await context.DatabaseContext.SaveChangesAsync();
        }

        public override Type GetSettingsType()
        {
            return (typeof(ThreatMatchSettings));
        }
        
        class ThreatMatchSettings
        {
            public string Username { get; set; }
            public string APIKey { get; set; }
        }
    }
}