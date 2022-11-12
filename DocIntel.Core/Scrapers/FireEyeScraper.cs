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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using DocIntel.Core.Exceptions;
using DocIntel.Core.Importers;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Settings;
using DocIntel.Integrations.FireEye;

using Ganss.Xss;

using Microsoft.Extensions.Logging;

namespace DocIntel.Core.Scrapers
{
    [Scraper("799a8266-722e-4ddf-9f72-f35c1a2cf6ea",
        Name = "FireEye",
        Description = "Scraper for FireEye API",
        Patterns = new[]
        {
            @"https://intelligence.fireeye.com/reports/.+",
            @"https://intelligence.fireeye.com/news_analysis/.+"
        })]
    public class FireEyeScraper : DefaultScraper
    {
        private readonly ILogger<FireEyeScraper> _logger;
        private readonly Scraper _scraper;
        private readonly ApplicationSettings _settings;

        public FireEyeScraper(Scraper scraper, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _scraper = scraper;
            _logger = (ILogger<FireEyeScraper>) serviceProvider.GetService(typeof(ILogger<FireEyeScraper>));
            _settings = (ApplicationSettings) serviceProvider.GetService(typeof(ApplicationSettings));
        }

        [ScraperSetting("ApiKey")] public string ApiKey { get; set; }

        [ScraperSetting("SecretKey", Type = AttributeFieldType.Password)]
        public string SecretKey { get; set; }

        public override async Task<bool> Scrape(SubmittedDocument message)
        {
            Init();
            if (!string.IsNullOrEmpty(ApiKey) && !string.IsNullOrEmpty(SecretKey))
            {
                var client = new FireEyeAPI(ApiKey, SecretKey,
                    proxy: new WebProxy("http://" + _settings.Proxy + "/", true, new[] {_settings.NoProxy}));
                var context = GetContext();
                var match = Regex.Match(message.URL, @"https://intelligence.fireeye.com/reports/(.+)");
                if (match.Success)
                {
                    var reportId = match.Groups[1].ToString();
                    var exists = await _documentRepository.ExistsAsync(context, new DocumentQuery
                    {
                        ExternalReference = reportId
                    });
                    if (exists)
                    {
                        _documentRepository.DeleteSubmittedDocument(context, message.SubmittedDocumentId,
                            SubmissionStatus.Duplicate);
                        await context.DatabaseContext.SaveChangesAsync();
                        return false;
                    }

                    var download = await DownloadReport(context, client, reportId, message);
                    _documentRepository.DeleteSubmittedDocument(context, message.SubmittedDocumentId);
                    await context.DatabaseContext.SaveChangesAsync();

                    return download;
                }

                match = Regex.Match(message.URL, @"https://intelligence.fireeye.com/news_analysis/(.+)");
                if (match.Success)
                {
                    var reportId = match.Groups[1].ToString();
                    var exists = await _documentRepository.ExistsAsync(context, new DocumentQuery
                    {
                        ExternalReference = reportId
                    });
                    if (exists)
                    {
                        _documentRepository.DeleteSubmittedDocument(context, message.SubmittedDocumentId,
                            SubmissionStatus.Duplicate);
                        await context.DatabaseContext.SaveChangesAsync();
                        return false;
                    }

                    var download = await DownloadTMH(context, client, reportId, message);
                    _documentRepository.DeleteSubmittedDocument(context, message.SubmittedDocumentId);
                    await context.DatabaseContext.SaveChangesAsync();

                    return download;
                }

                return true;
            }

            _logger.LogDebug("Invalid AccessKey or SecretKey");

            return true;
        }

        private async Task<bool> DownloadReport(AmbientContext context, FireEyeAPI client, string reportId,
            SubmittedDocument url)
        {
            var report = client.Report(reportId, "full");
            if (report == null)
                return true;

            // Build the document
            var document = new Document
            {
                Title = GetTitle(report),
                ExternalReference = GetExternalReference(report),
                ShortDescription = GetSummary(report),
                SourceUrl = url.URL
            };

            var tags = GetFireEyeTags(report).ToArray();
            try
            {
                document = await AddAsync(_scraper, context, document, url, tags);

                await ImportPDF(context, client, reportId, document, report);
                await ImportJSON(context, client, reportId, document, report);
                await ImportSTIX(context, client, reportId, document, report);
                await ImportHTML(context, client, reportId, document, report);
            }
            catch (InvalidArgumentException e)
            {
                foreach (var error in e.Errors)
                {
                    Console.WriteLine(error.Key + " : ");
                    foreach (var ee in error.Value) Console.WriteLine(" - " + ee);
                }

                return true;
            }
            catch (FileAlreadyKnownException e)
            {
                Console.WriteLine("File already known");
                Console.WriteLine(e.Hash);
                Console.WriteLine(e.ExistingReference);
                return false;
            }

            return false;
        }

        private async Task<bool> DownloadTMH(AmbientContext context, FireEyeAPI client, string reportId,
            SubmittedDocument url)
        {
            var report = client.tmh(reportId);
            if (report == null)
                return true;

            // Build the document
            var document = new Document
            {
                Title = report.Title,
                ExternalReference = report.ReportId,
                ShortDescription = report.isightComment,
                SourceUrl = url.URL
            };

            var tags = new string[] { };
            try
            {
                document = await AddAsync(_scraper, context, document, url, tags);
                await ImportPDF(context, client, reportId, document, report);
                await ImportJSON(context, client, reportId, document, report);
                await ImportHTML(context, client, reportId, document, report);
            }
            catch (InvalidArgumentException e)
            {
                foreach (var error in e.Errors)
                {
                    Console.WriteLine(error.Key + " : ");
                    foreach (var ee in error.Value) Console.WriteLine(" - " + ee);
                }

                return true;
            }
            catch (FileAlreadyKnownException e)
            {
                Console.WriteLine("File already known");
                Console.WriteLine(e.Hash);
                Console.WriteLine(e.ExistingReference);
                return false;
            }

            return false;
        }

        private async Task ImportHTML(AmbientContext context, FireEyeAPI client, string reportId, Document document,
            FireEyeReport report)
        {
            var documentFile = new DocumentFile
            {
                Document = document,
                MimeType = "text/html",
                Filename = reportId + ".html",
                Title = "HTML Report",
                DocumentDate = GetPublicationDate(report),
                Preview = true,
                Visible = true
            };

            using var stream = new MemoryStream();
            client.ReportDownload(reportId, stream, "full", format: FireEyeAPI.AcceptedFormat.HTML);
            documentFile = await AddAsync(_scraper, context, documentFile, stream);
        }

        private async Task ImportHTML(AmbientContext context, FireEyeAPI client, string reportId, Document document,
            FireEyeTMHReport report)
        {
            var documentFile = new DocumentFile
            {
                Document = document,
                MimeType = "text/html",
                Filename = reportId + ".html",
                Title = "HTML Report",
                DocumentDate = GetPublicationDate(report),
                Preview = true,
                Visible = true
            };

            using var stream = new MemoryStream();
            client.tmhDownload(reportId, stream, FireEyeAPI.AcceptedFormat.HTML);
            documentFile = await AddAsync(_scraper, context, documentFile, stream);
        }

        private async Task ImportSTIX(AmbientContext context, FireEyeAPI client, string reportId, Document document,
            FireEyeReport report)
        {
            var documentFile = new DocumentFile
            {
                Document = document,
                MimeType = "application/stix",
                Filename = reportId + ".stix",
                Title = "STIX File",
                DocumentDate = GetPublicationDate(report),
                Visible = true
            };

            using var stream = new MemoryStream();
            client.ReportDownload(reportId, stream, "full", format: FireEyeAPI.AcceptedFormat.STIX);
            documentFile = await AddAsync(_scraper, context, documentFile, stream);
        }

        private async Task ImportJSON(AmbientContext context, FireEyeAPI client, string reportId, Document document,
            FireEyeReport report)
        {
            var documentFile = new DocumentFile
            {
                Document = document,
                MimeType = "application/json",
                Filename = reportId + ".json",
                Title = "JSON FireEye File",

                DocumentDate = GetPublicationDate(report)
            };

            using var jsonStream = new MemoryStream();
            client.ReportDownload(reportId, jsonStream, "full", format: FireEyeAPI.AcceptedFormat.JSON);
            documentFile = await AddAsync(_scraper, context, documentFile, jsonStream);
        }

        private async Task ImportJSON(AmbientContext context, FireEyeAPI client, string reportId, Document document,
            FireEyeTMHReport report)
        {
            var documentFile = new DocumentFile
            {
                Document = document,
                MimeType = "application/json",
                Filename = reportId + ".json",
                Title = "JSON FireEye File",

                DocumentDate = GetPublicationDate(report)
            };

            using var jsonStream = new MemoryStream();
            client.tmhDownload(reportId, jsonStream, FireEyeAPI.AcceptedFormat.JSON);
            documentFile = await AddAsync(_scraper, context, documentFile, jsonStream);
        }

        private async Task<MemoryStream> ImportPDF(AmbientContext context, FireEyeAPI client, string reportId,
            Document document,
            FireEyeReport report)
        {
            var documentFile = new DocumentFile
            {
                Document = document,
                MimeType = "application/pdf",
                Filename = reportId + ".pdf",
                Title = "PDF Report",

                DocumentDate = GetPublicationDate(report),
                Preview = true,
                Visible = true
            };

            await using var pdfStream = new MemoryStream();
            client.ReportDownload(reportId, pdfStream, "full", format: FireEyeAPI.AcceptedFormat.PDF);
            documentFile = await AddAsync(_scraper, context, documentFile, pdfStream);
            return pdfStream;
        }

        private async Task<MemoryStream> ImportPDF(AmbientContext context, FireEyeAPI client, string reportId,
            Document document,
            FireEyeTMHReport report)
        {
            var documentFile = new DocumentFile
            {
                Document = document,
                MimeType = "application/pdf",
                Filename = reportId + ".pdf",
                Title = "PDF Report",

                DocumentDate = GetPublicationDate(report),
                Preview = true,
                Visible = true
            };

            await using var pdfStream = new MemoryStream();
            client.tmhDownload(reportId, pdfStream, FireEyeAPI.AcceptedFormat.PDF);
            documentFile = await AddAsync(_scraper, context, documentFile, pdfStream);
            return pdfStream;
        }

        protected IEnumerable<string> GetFireEyeTags(FireEyeReport jsonResult)
        {
            var tags = new HashSet<string>();
            GetAudienceTags(jsonResult, tags);
            GetProductTags(jsonResult, tags);
            GetReportTypeTags(jsonResult, tags);
            GetMainTags(jsonResult, tags);
            GetRelationTags(jsonResult, tags);
            return tags;
        }

        protected DateTime GetPublicationDate(FireEyeReport jsonResult)
        {
            return GetPublicationDate(jsonResult.publishDate);
        }

        protected DateTime GetPublicationDate(FireEyeTMHReport jsonResult)
        {
            return GetPublicationDate(jsonResult.publishDate);
        }

        protected static string GetExternalReference(FireEyeReport jsonResult)
        {
            return jsonResult.ReportId;
        }

        protected static string GetTitle(FireEyeReport jsonResult)
        {
            return jsonResult.Title;
        }

        protected static string GetSummary(FireEyeReport jsonResult)
        {
            var summary = jsonResult.ExecSummary;

            if (string.IsNullOrEmpty(summary))
                summary = jsonResult.threatDescription;

            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedTags.Clear();
            sanitizer.KeepChildNodes = true;
            summary = sanitizer.Sanitize(summary);
            return summary;
        }

        protected DateTime GetPublicationDate(string value)
        {
            var enUS = new CultureInfo("en-US");
            var publishedDateElement = value;
            DateTime dateValue;
            if (!DateTime.TryParseExact(publishedDateElement, "MMMM dd, yyyy hh:mm:ss tt", enUS, DateTimeStyles.None,
                out dateValue))
                dateValue = DateTime.UtcNow;

            return dateValue;
        }

        protected void GetRelationTags(FireEyeReport jsonResult, HashSet<string> tags)
        {
            var relationSections = jsonResult?.Relations;
            if (relationSections != null)
            {
                if (relationSections.MalwareFamilies != null)
                    foreach (var t in relationSections.MalwareFamilies)
                        tags.Add("malwareFamily:" + t);
                if (relationSections.Actors != null)
                    foreach (var t in relationSections.Actors)
                        tags.Add("actor:" + t);
            }
        }

        protected void GetMainTags(FireEyeReport jsonResult, HashSet<string> tags)
        {
            var tagSections = jsonResult?.TagSection?.Main;
            if (tagSections != null)
            {
                if (tagSections.affectedIndustries?.affectedIndustry != null)
                    foreach (var t in tagSections.affectedIndustries.affectedIndustry)
                        tags.Add("affectedIndustry:" + t);
                if (tagSections.operatingSystems?.operatingSystem != null)
                    foreach (var t in tagSections.operatingSystems.operatingSystem)
                        tags.Add("operatingSystem:" + t);
                if (tagSections.roles?.role != null)
                    foreach (var t in tagSections.roles.role)
                        tags.Add("role:" + t);
                if (tagSections.malwareCapabilities?.malwareCapability != null)
                    foreach (var t in tagSections.malwareCapabilities.malwareCapability)
                        tags.Add("malwareCapability:" + t);
                if (tagSections.detectionNames?.detectionName != null)
                    foreach (var t in tagSections.detectionNames.detectionName)
                        tags.Add("detectionName:" + t.vendor + ":" + t.name);
                if (tagSections.malwareFamilies?.malwareFamily != null)
                    foreach (var t in tagSections.malwareFamilies.malwareFamily)
                        tags.Add("malwareFamily:" + t.name);
            }
        }

        protected static void GetReportTypeTags(FireEyeReport jsonResult, HashSet<string> tags)
        {
            var reportType = jsonResult.ReportType;
            if (!string.IsNullOrEmpty(reportType)) tags.Add("reportType:" + reportType);
        }

        protected static void GetProductTags(FireEyeReport jsonResult, HashSet<string> tags)
        {
            var products = jsonResult.ThreatScape.Product;
            foreach (var product in products) tags.Add("product:" + product);
        }

        protected static void GetAudienceTags(FireEyeReport jsonResult, HashSet<string> tags)
        {
            var audiences = jsonResult.Audience;
            foreach (var audience in audiences) tags.Add("audience:" + audience);
        }
    }
}