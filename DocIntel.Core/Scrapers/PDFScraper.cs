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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DocIntel.Core.Helpers;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Settings;
using Microsoft.Extensions.Logging;

namespace DocIntel.Core.Scrapers
{
    [Scraper("8726e5d8-e39f-4917-9e8e-cf95abbe677e",
        Name = "PDF Scraper",
        Description = "PDF Downloader",
        Patterns = new[]
        {
            @".*"
        })]
    public class PdfScraper : DefaultScraper
    {
        private readonly ILogger<PdfScraper> _logger;
        private readonly Scraper _scraper;
        private readonly ApplicationSettings _settings;

        public PdfScraper(Scraper scraper, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _scraper = scraper;
            _settings = (ApplicationSettings) serviceProvider.GetService(typeof(ApplicationSettings));
            _logger = (ILogger<PdfScraper>) serviceProvider.GetService(typeof(ILogger<PdfScraper>));
        }

        public override async Task<bool> Scrape(SubmittedDocument message)
        {
            Init();
            var context = await GetContextAsync(message.SubmitterId);
            var uri = new Uri(message.URL);
            var host = uri.Host;

            var exists = await _documentRepository.ExistsAsync(context, new DocumentQuery
            {
                SourceUrl = uri.ToString()
            });

            if (exists)
            {
                _logger.LogDebug($"Document for {uri} already exists, updating the document.");
                _documentRepository.DeleteSubmittedDocument(context, message.SubmittedDocumentId,
                    SubmissionStatus.Duplicate);
                await context.DatabaseContext.SaveChangesAsync();
                return false;
            }

            var httpClientHandler = new HttpClientHandler();
            if (!string.IsNullOrEmpty(_settings.Proxy))
            {
                httpClientHandler.Proxy = new WebProxy("http://" + _settings.Proxy + "/", true, new[] { _settings.NoProxy });
            }

            HttpClient httpClient = new HttpClient(httpClientHandler);

            var response = await httpClient.GetAsync(message.URL);
            if (response.Content.Headers.ContentType.ToString() == "application/pdf")
            {
                _logger.LogDebug($"Document for {uri} unknown, register the document.");
                var source = _sourceRepository.GetAllAsync(context, new SourceQuery
                {
                    HomePage = host
                }).ToEnumerable().FirstOrDefault();

                if (source == null)
                    source = await _sourceRepository.CreateAsync(context, new Source
                    {
                        Title = host,
                        HomePage = host
                    });

                var d = new Document
                {
                    Title = uri.GetLastPart(),
                    ShortDescription = "",
                    SourceId = source.SourceId,
                    Status = DocumentStatus.Submitted,
                    DocumentDate = message.SubmissionDate
                };
                var trackingD = await AddAsync(_scraper, context, d, message);
                _logger.LogDebug("Document created...");
                
                var webClient = new WebClient();
                var downloadData = webClient.DownloadData(message.URL);
                using (var stream = new MemoryStream(downloadData))
                {
                    var documentFile = new DocumentFile
                    {
                        Document = trackingD,
                        MimeType = "application/pdf",
                        Filename = uri.GetLastPart(),
                        Title = uri.GetLastPart(),
                        ClassificationId = _classificationRepository.GetDefault(context)?.ClassificationId,
                        SourceUrl = message.URL,
                        Visible = true,
                        Preview = true
                    };
                    documentFile = await AddAsync(_scraper, context, documentFile, stream);
                    _logger.LogDebug("PDF Downloaded and added");
                }

                _documentRepository.DeleteSubmittedDocument(context, message.SubmittedDocumentId);
                await context.DatabaseContext.SaveChangesAsync();
                return false;
            }
            
            return true;
        }

        public override Type GetSettingsType()
        {
            return (typeof(PdfSettings));
        }

        class PdfSettings
        {}
    }
}