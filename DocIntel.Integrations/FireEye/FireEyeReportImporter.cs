namespace DocIntel.Integrations.FireEye
{

    /*public class FireEyeReportImporter : FireEyeImporter {
        private FireEyeAPI client;
        private readonly FireEyePluginSettings _settings;

        public FireEyeReportImporter(Core.Models.Importer feed, IServiceProvider serviceProvider) : base(feed, serviceProvider)
        {
            _settings = feed.Settings.ToObject<FireEyePluginSettings>();

            if (!string.IsNullOrEmpty(_settings.Proxy)) {
                var proxy = new WebProxy("proxyString");
                client = new FireEyeAPI(_settings.ApiKey, _settings.Secret, proxy: proxy);

            } else {
                client = new FireEyeAPI(_settings.ApiKey, _settings.Secret);

            }
        }

        public override async Task PullLatest(int limit = 1) {
            // Get the index
            
            var reports = client.ReportIndex(since: DateTime.UtcNow.AddDays(-14), limit: 10);
            // Download all the reports
            foreach(var report in reports) {
                if (!await this.DocumentExistAsync(report.reportId)) {
                    await Download(report);
                }
            }
        }

        private async Task<Document> Download(FireEyeReportIndexMessage message) {
            // Get the JSON
            var report = client.Report(message.reportId, detail: "full");
            // Build the document
            var d = new Document {
                Title = GetTitle(report),
                ExternalReference = GetExternalReference(report),
                ShortDescription = GetSummary(report),
                Status = DocumentStatus.Registered,
                // TODO
                // Note = await GetNotes(ambientContext, jsonResult, source, _tagRepository, _documentRepository)
            };
            var tags = await GetFireEyeTags(report);
            try {
            d = await this.ImportDocument(d, tags);
            // Import PDF File

            var f = new DocumentFile() {
                DocumentId = d.DocumentId,
                MimeType = "application/pdf",
                Filename = message.reportId + ".pdf",
                Title = "PDF Report", 
                //Classification = Classification.Restricted,
                DocumentDate = GetPublicationDate(report)
            };

            using MemoryStream pdfStream = new MemoryStream();
            client.ReportDownload(message.reportId, stream: pdfStream, detail: "full", format: FireEyeAPI.AcceptedFormat.PDF);
            f = await this.ImportFile(d, f, pdfStream);

            // Import JSON File

            f = new DocumentFile() {
                DocumentId = d.DocumentId,
                MimeType = "application/json",
                Filename = message.reportId + ".json",
                Title = "JSON FireEye File", 
                // Classification = Classification.Restricted,
                DocumentDate = GetPublicationDate(report)
            };

            using MemoryStream jsonStream = new MemoryStream();
            client.ReportDownload(message.reportId, stream: pdfStream, detail: "full", format: FireEyeAPI.AcceptedFormat.JSON);
            f = await this.ImportFile(d, f, jsonStream);



            // Import STIX File

            f = new DocumentFile() {
                DocumentId = d.DocumentId,
                MimeType = "application/stix",
                Filename = message.reportId + ".stix",
                Title = "STIX File",
                // Classification = Classification.Restricted,
                DocumentDate = GetPublicationDate(report)
            };

            using MemoryStream stixStream = new MemoryStream();
            client.ReportDownload(message.reportId, stream: stixStream, detail: "full", format: FireEyeAPI.AcceptedFormat.STIX);
            f = await this.ImportFile(d, f, stixStream);

            // HTML

            f = new DocumentFile() {
                DocumentId = d.DocumentId,
                MimeType = "text/html",
                Filename = message.reportId + ".html",
                Title = "HTML Report",
                // Classification = Classification.Restricted,
                DocumentDate = GetPublicationDate(report)
            };

            using MemoryStream htmlStream = new MemoryStream();
            client.ReportDownload(message.reportId, stream: htmlStream, detail: "full", format: FireEyeAPI.AcceptedFormat.HTML);
            f = await this.ImportFile(d, f, htmlStream);

            // CSV

            // f = new DocumentFile() {
            //     DocumentId = d.DocumentId,
            //     MimeType = "text/html",
            //     Filename = message.reportId + ".csv",
            //     Title = "CSV Indicators Report",
            //     Classification = Classification.Restricted,
            //     DocumentDate = GetPublicationDate(report)
            // };

            // using MemoryStream csvStream = new MemoryStream();
            // client.ReportDownload(message.reportId, stream: csvStream, detail: "full", format: FireEyeAPI.AcceptedFormat.CSV);
            // f = await this.ImportFile(d, f, pdfStream);

            // SNORT

            // f = new DocumentFile() {
            //     DocumentId = d.DocumentId,
            //     MimeType = "text/html",
            //     Filename = message.reportId + ".html",
            //     Title = "Snort rules",
            //     Classification = Classification.Restricted,
            //     DocumentDate = GetPublicationDate(report)
            // };

            // using MemoryStream snortStream = new MemoryStream();
            // client.ReportDownload(message.reportId, stream: snortStream, detail: "full", format: FireEyeAPI.AcceptedFormat.SNORT);
            // f = await this.ImportFile(d, f, pdfStream);

            // Get PDF, XML, HTML, STIX, CSV, SNORT
            // Add the files
            // return
            
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

        protected async Task<ISet<string>> GetFireEyeTags(FireEyeReport jsonResult)
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
    }
    */
}