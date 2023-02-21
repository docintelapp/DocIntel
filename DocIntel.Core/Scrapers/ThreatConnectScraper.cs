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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using DocIntel.Core.Exceptions;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Settings;
using DocIntel.Integrations.ThreatConnect;
using DocIntel.Integrations.ThreatConnect.Model;

using Microsoft.Extensions.Logging;

using Document = DocIntel.Core.Models.Document;
using DocumentStatus = DocIntel.Core.Models.DocumentStatus;

namespace DocIntel.Core.Scrapers
{
    [Scraper("4782e22d-7bf5-47dc-8fd0-8ac84ee6fd90",
        Name = "ThreatConnect",
        Description = "Scraper for ThreatConnect URI",
        Patterns = new[]
        {
            @"https?:\/\/app\.threatconnect\.com\/auth\/report\/report\.xhtml\?report\=.*",
            @"https?:\/\/app\.threatconnect\.com\/auth\/document\/document\.xhtml\?document\=.*"
        })]
    public class ThreatConnectScraper : DefaultScraper
    {
        private readonly ILogger<ThreatConnectScraper> _logger;
        private readonly Scraper _scraper;
        private readonly ApplicationSettings _settings;
        public override bool HasSettings => true;
        public ThreatConnectScraper(Scraper scraper, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _logger = (ILogger<ThreatConnectScraper>) serviceProvider.GetService(typeof(ILogger<ThreatConnectScraper>));
            _settings = (ApplicationSettings) serviceProvider.GetService(typeof(ApplicationSettings));
            _scraper = scraper;
        }

        public override async Task<bool> Scrape(SubmittedDocument message)
        {
            var scraperSettings = _scraper.Settings.ToObject<ThreatConnectSettings>();
            Init();
            var context = await GetContextAsync();
            var match = Regex.Match(message.URL,
                @"https?:\/\/app\.threatconnect\.com\/auth\/(document|report)\/(document|report)\.xhtml\?(document|report)\=(.*)");
            var endpoint = match.Groups[1].ToString();
            var articleGUID = match.Groups[4].ToString();

            var client = new APIClient(scraperSettings.AccessId, scraperSettings.SecretKey,
                new WebProxy("http://" + _settings.Proxy + "/", true, new[] {_settings.NoProxy}));
            var groupParameter = new GroupParameter {Owner = scraperSettings.Owner};

            if (endpoint == "report")
            {
                await ImportReport(message, client, articleGUID, groupParameter, context);
                return false;
            }

            if (endpoint == "document")
            {
                await ImportDocument(message, client, articleGUID, groupParameter, context);
                return false;
            }

            return true;
        }

        private async Task ImportReport(SubmittedDocument message, APIClient client, string articleGUID,
            GroupParameter groupParameter, AmbientContext context)
        {
            SingleResponse response;
            response = await client.Groups.GetGroup(GroupType.Reports, articleGUID, groupParameter);
            var report = response.Data.Report;

            if (report.Status == "Success")
            {
                Document document;
                var exists = await _documentRepository.ExistsAsync(context, new DocumentQuery
                {
                    SourceUrl = message.URL
                });

                if (exists)
                {
                    _logger.LogInformation($"Document for {message.URL} already exists, updating the document.");
                    _documentRepository.DeleteSubmittedDocument(context, message.SubmittedDocumentId,
                        SubmissionStatus.Duplicate);
                    await context.DatabaseContext.SaveChangesAsync();
                    return;
                }

                document = new Document
                {
                    Title = report.Name,
                    Status = DocumentStatus.Submitted,
                    DocumentDate = report.DateAdded
                };

                if (await PopulateWithAttributes(GroupType.Reports, message, client, articleGUID, groupParameter,
                    context, document)) return;

                var tags = (await PopulateWithTags(GroupType.Reports, message, client, articleGUID, groupParameter,
                    context, document)).ToArray();
                document = await AddAsync(_scraper, context, document, message, tags);
                _logger.LogDebug("Document created... " + document.DocumentId);

                if (report.FileName.EndsWith(".pdf"))
                {
                    var documentFile = new DocumentFile
                    {
                        Document = document,
                        MimeType = "application/pdf",
                        Filename = report.FileName,
                        Title = report.Name,
                        ClassificationId = _classificationRepository.GetDefault(context).ClassificationId,
                        DocumentDate = report.DateAdded,
                        SourceUrl = report.WebLink,
                        Visible = true,
                        Preview = true
                    };

                    var stream = await client.Groups.DownloadGroups(GroupType.Reports, articleGUID, groupParameter);
                    stream.Position = 0;
                    documentFile = await AddAsync(_scraper, context, documentFile, stream);
                }
            }

            _documentRepository.DeleteSubmittedDocument(context, message.SubmittedDocumentId);
            await context.DatabaseContext.SaveChangesAsync();
        }

        private async Task<bool> PopulateWithAttributes(GroupType groupType, SubmittedDocument message,
            APIClient client, string articleGUID,
            GroupParameter groupParameter, AmbientContext context, Document document)
        {
            // Check if there is a source specified in the attributes
            var attributeResponse =
                await client.Groups.GetGroupAttributes(groupType, articleGUID, groupParameter);
            var attributes = attributeResponse.Data.Attribute;
            var sourceStr = attributes.SingleOrDefault(_ => _.Type == "Source")?.Value ?? "";
            var description = attributes.SingleOrDefault(_ => _.Type == "Description")?.Value ?? "";

            var threatConnectURL = new Uri(message.URL);
            var threatConnectHost = threatConnectURL.Host;
            Source source = null;
            if (!string.IsNullOrEmpty(sourceStr))
            {
                var sourceUrl = new Uri(sourceStr);
                var sourceHost = sourceUrl.Host;

                var exists = await _documentRepository.ExistsAsync(context, new DocumentQuery
                {
                    SourceUrl = sourceUrl.ToString()
                });
                if (exists)
                {
                    Console.WriteLine("Document already exists");
                    _documentRepository.DeleteSubmittedDocument(context, message.SubmittedDocumentId,
                        SubmissionStatus.Duplicate);
                    await context.DatabaseContext.SaveChangesAsync();
                    return true;
                }

                source = _sourceRepository.GetAllAsync(context, new SourceQuery
                {
                    HomePage = sourceHost
                }).ToEnumerable().FirstOrDefault();

                if (source == null)
                    source = await _sourceRepository.CreateAsync(context, new Source
                    {
                        Title = sourceHost,
                        HomePage = sourceHost
                    });
            }

            if (source == null)
            {
                source = _sourceRepository.GetAllAsync(context, new SourceQuery
                {
                    HomePage = threatConnectHost
                }).ToEnumerable().FirstOrDefault();

                if (source == null)
                    source = await _sourceRepository.CreateAsync(context, new Source
                    {
                        Title = "ThreatConnect",
                        HomePage = threatConnectHost
                    });
            }

            document.Source = source;
            document.ShortDescription = description;
            return false;
        }

        private async Task<IEnumerable<string>> PopulateWithTags(GroupType groupType, SubmittedDocument message,
            APIClient client, string articleGUID,
            GroupParameter groupParameter, AmbientContext context, Document document)
        {
            // Check if there is a source specified in the attributes
            var attributeResponse =
                await client.Groups.GetGroupTags(groupType, articleGUID, groupParameter);
            var attributes = attributeResponse.Data.Tag.Select(_ => _.Name);

            return attributes;
        }

        private async Task ImportDocument(SubmittedDocument message, APIClient client, string articleGUID,
            GroupParameter groupParameter, AmbientContext context)
        {
            _logger.LogDebug("ImportDocument");
            SingleResponse response;
            response = await client.Groups.GetGroup(GroupType.Documents, articleGUID, groupParameter);
            var report = response.Data.Document;

            Document document;
            var exists = await _documentRepository.ExistsAsync(context, new DocumentQuery
            {
                SourceUrl = message.URL
            });

            if (exists)
            {
                _logger.LogInformation($"Document for {message.URL} already exists, updating the document.");
                _documentRepository.DeleteSubmittedDocument(context, message.SubmittedDocumentId,
                    SubmissionStatus.Duplicate);
                await context.DatabaseContext.SaveChangesAsync();
                return;
            }

            document = new Document
            {
                Title = report.Name,
                DocumentDate = report.DateAdded
            };

            document = await AddAsync(_scraper, context, document, message);
            _logger.LogDebug("Document created... " + document.DocumentId);

            try
            {
                var documentFile = new DocumentFile
                {
                    Document = document,
                    MimeType = "application/pdf",
                    Filename = report.FileName,
                    Title = report.Name,
                    ClassificationId = _classificationRepository.GetDefault(context)?.ClassificationId,
                    DocumentDate = report.DateAdded,
                    SourceUrl = report.WebLink,
                    Visible = true,
                    Preview = true
                };

                var stream = await client.Groups.DownloadGroups(GroupType.Documents, articleGUID, groupParameter);
                stream.Position = 0;
                documentFile = await AddAsync(_scraper, context, documentFile, stream);
            }
            catch (FileAlreadyKnownException)
            {
                _logger.LogDebug("File is already known...");
            }

            _documentRepository.DeleteSubmittedDocument(context, message.SubmittedDocumentId);
            await context.DatabaseContext.SaveChangesAsync();
        }

        public override Type GetSettingsType()
        {
            return (typeof(ThreatConnectSettings));
        }
        
        class ThreatConnectSettings
        {
            public string AccessId { get; set; }
            public string SecretKey { get; set; }
            public string Owner { get; set; }
        }
    }
}