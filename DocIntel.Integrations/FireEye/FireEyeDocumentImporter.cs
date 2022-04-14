namespace DocIntel.Integrations.FireEye
{
    /*public abstract class FireEyeImporter : Importer {
        protected FireEyeImporter(Core.Models.Importer feed, IServiceProvider serviceProvider) : base(feed, serviceProvider)
        {
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
            string summary = jsonResult.ExecSummary;

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
            CultureInfo enUS = new CultureInfo("en-US");
            var publishedDateElement = value;
            DateTime dateValue;
            if (DateTime.TryParseExact(publishedDateElement, "MMMM dd, yyyy hh:mm:ss tt", enUS, DateTimeStyles.None, out dateValue))
            {
                // _logger.LogInformation(dateValue.ToString());
            }
            else
            {
                // _logger.LogError("Could not parse date " + publishedDateElement);
                dateValue = DateTime.Now;
            }

            return dateValue;
        }

        protected void GetRelationTags(FireEyeReport jsonResult, HashSet<string> tags)
        {
            var relationSections = jsonResult?.Relations;
            if (relationSections != null)
            {
                if (relationSections.MalwareFamilies != null)
                    foreach (var t in relationSections.MalwareFamilies) {
                        tags.Add("malwareFamily:" + t);
                    }
                if (relationSections.Actors != null)
                    foreach (var t in relationSections.Actors) {
                        tags.Add("actor:" + t);
                    }
            }
        }

        protected void GetMainTags(FireEyeReport jsonResult, HashSet<string> tags)
        {
            var tagSections = jsonResult?.TagSection?.Main;
            if (tagSections != null)
            {
                if (tagSections.affectedIndustries?.affectedIndustry != null)
                    foreach (var t in tagSections.affectedIndustries.affectedIndustry) {
                        tags.Add("affectedIndustry:" + t);
                    }
                if (tagSections.operatingSystems?.operatingSystem != null)
                    foreach (var t in tagSections.operatingSystems.operatingSystem) {
                        tags.Add("operatingSystem:" + t);
                    }
                if (tagSections.roles?.role != null)
                    foreach (var t in tagSections.roles.role) {
                        tags.Add("role:" + t);
                    }
                if (tagSections.malwareCapabilities?.malwareCapability != null)
                    foreach (var t in tagSections.malwareCapabilities.malwareCapability) {
                        tags.Add("malwareCapability:" + t);
                    }
                if (tagSections.detectionNames?.detectionName != null)
                    foreach (var t in tagSections.detectionNames.detectionName) {
                        tags.Add("detectionName:" + t.vendor + ":" + t.name);
                    }
                if (tagSections.malwareFamilies?.malwareFamily != null)
                    foreach (var t in tagSections.malwareFamilies.malwareFamily) {
                        tags.Add("malwareFamily:" + t.name);
                    }
            }
        }

        protected static void GetReportTypeTags(FireEyeReport jsonResult, HashSet<string> tags)
        {
            string reportType = jsonResult.ReportType;
            if (!string.IsNullOrEmpty(reportType))
            {
                tags.Add("reportType:" + reportType);
            }
        }

        protected static void GetProductTags(FireEyeReport jsonResult, HashSet<string> tags)
        {
            string[] products = jsonResult.ThreatScape.Product;
            foreach (var product in products)
            {
                tags.Add("product:" + product);
            }
        }

        protected static void GetAudienceTags(FireEyeReport jsonResult, HashSet<string> tags)
        {
            string[] audiences = jsonResult.Audience;
            foreach (var audience in audiences)
            {
                tags.Add("audience:" + audience);
            }
        }
    }*/
}