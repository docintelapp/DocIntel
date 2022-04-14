namespace DocIntel.Integrations.FireEye
{
    /*
    public class FireEyeThreatMediaHighlightImporter : FireEyeImporter {
        private FireEyeAPI client;
        private readonly FireEyePluginSettings _settings;
        private readonly IPublishEndpoint _busClient;

        public FireEyeThreatMediaHighlightImporter(Core.Models.Importer feed, IServiceProvider serviceProvider) : base(feed, serviceProvider)
        {
            _settings = feed.Settings.ToObject<FireEyePluginSettings>();
            _busClient = (IPublishEndpoint) serviceProvider.GetService(typeof(IPublishEndpoint));

            if (!string.IsNullOrEmpty(_settings.Proxy)) {
                var proxy = new WebProxy("proxyString");
                client = new FireEyeAPI(_settings.ApiKey, _settings.Secret, proxy: proxy);

            } else {
                client = new FireEyeAPI(_settings.ApiKey, _settings.Secret);

            }
        }

        public override async Task PullLatest(int limit = 1) {            
            var reports = client.TMHReportIndex(since: DateTime.UtcNow.AddDays(-14), limit: 10);
            // Download all the reports
            foreach(var report in reports) {
                if (!await this.DocumentExistAsync(report.reportId)) {
                    await Download(report);
                }
            }
        }

        private async Task<Document> Download(FireEyeTMHIndexMessage message) {
            // Get the JSON
            var report = client.tmh(message.reportId);
            // Build the document
            var d = new Document {
                Title = GetTitle(report),
                ExternalReference = GetExternalReference(report),
                ShortDescription = GetSummary(report),
                Status = DocumentStatus.Registered,
                // TODO
                // Note = await GetNotes(ambientContext, jsonResult, source, _tagRepository, _documentRepository)
            };
            var tags = GetFireEyeTags(report);
            try {
            d = await this.ImportDocument(d, tags);

            // Submit the story link
            await _busClient.Publish(new URLSubmittedMessage {
                URL = report.storyLink
            });

            // Import PDF File
            var f = new DocumentFile() {
                DocumentId = d.DocumentId,
                MimeType = "application/pdf",
                Filename = report.ReportId + ".pdf",
                Title = "PDF Report", 
                // Classification = Classification.Restricted,
                DocumentDate = GetPublicationDate(report)
            };

            using MemoryStream pdfStream = new MemoryStream();
            client.tmhDownload(report.ReportId, stream: pdfStream, format: FireEyeAPI.AcceptedFormat.PDF);
            f = await this.ImportFile(d, f, pdfStream);

            // Import JSON File
            f = new DocumentFile() {
                DocumentId = d.DocumentId,
                MimeType = "application/json",
                Filename = report.ReportId + ".json",
                Title = "JSON FireEye File", 
                // Classification = Classification.Restricted,
                DocumentDate = GetPublicationDate(report)
            };

            using MemoryStream jsonStream = new MemoryStream();
            client.tmhDownload(report.ReportId, stream: pdfStream, format: FireEyeAPI.AcceptedFormat.JSON);
            f = await this.ImportFile(d, f, jsonStream);

            // HTML
            f = new DocumentFile() {
                DocumentId = d.DocumentId,
                MimeType = "text/html",
                Filename = report.ReportId + ".html",
                Title = "HTML Report",
                // Classification = Classification.Restricted,
                DocumentDate = GetPublicationDate(report)
            };

            using MemoryStream htmlStream = new MemoryStream();
            client.tmhDownload(report.ReportId, stream: htmlStream, format: FireEyeAPI.AcceptedFormat.HTML);
            f = await this.ImportFile(d, f, htmlStream);

            
            } catch (DocIntel.Core.Exceptions.InvalidArgumentException e) {
                foreach (var error in e.Errors) {
                    System.Console.WriteLine(error.Key + " : ");
                    foreach (var ee in error.Value) {
                        System.Console.WriteLine(" - " + ee);
                    }
                }
                return null;
            } catch (DocIntel.Core.Exceptions.FileAlreadyKnownException e) {
                System.Console.WriteLine("File already known");
                System.Console.WriteLine(e.Hash);
                System.Console.WriteLine(e.ExistingReference);
                
                return null;
            }

            await this.SaveChangesAsync();
            return d;
        }

        private DateTime GetPublicationDate(FireEyeTMHReport report)
        {
            return base.GetPublicationDate(report.publishDate);
        }

        protected static string GetExternalReference(FireEyeTMHReport jsonResult)
        {
            return jsonResult.ReportId;
        }

        protected static string GetTitle(FireEyeTMHReport jsonResult)
        {
            return jsonResult.Title;
        }

        protected static string GetSummary(FireEyeTMHReport jsonResult)
        {
            string summary = jsonResult.isightComment;

            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedTags.Clear();
            sanitizer.KeepChildNodes = true;
            summary = sanitizer.Sanitize(summary);
            return summary;
        }

        protected ISet<string> GetFireEyeTags(FireEyeTMHReport jsonResult)
        {
            var tags = new HashSet<string> { "accuracy:" + jsonResult.tmhAccuracyRanking };
            GetProductTags(jsonResult, tags);
            return tags;
        }

        protected void GetProductTags(FireEyeTMHReport jsonResult, HashSet<string> tags)
        {
            string[] products = jsonResult.ThreatScape.Product;
            foreach (var product in products)
            {
                tags.Add("product:" + product);
            }
        }
    }
    */
}