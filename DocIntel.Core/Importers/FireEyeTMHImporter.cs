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
using System.Net;

using DocIntel.Core.Models;
using DocIntel.Core.Settings;
using DocIntel.Integrations.FireEye;

using Microsoft.Extensions.Logging;

namespace DocIntel.Core.Importers
{
    [Importer("ac08f3a7-27f6-473e-9cfa-a5274806c476", Name = "FireEye Threat Media Highlight",
        Description = "Importer for FireEye Threat Media Highlight")]
    // ReSharper disable once UnusedType.Global because it is used but with reflexion
    public class FireEyeThreatMediaHighlightImporter : DefaultImporter
    {
        private readonly Importer _importer;

        private readonly ILogger<FireEyeThreatMediaHighlightImporter> _logger;
        private readonly ApplicationSettings _settings;
        public override bool HasSettings => true;

        public FireEyeThreatMediaHighlightImporter(IServiceProvider serviceProvider, Importer importer) : base(
            serviceProvider)
        {
            _importer = importer;
            _logger = (ILogger<FireEyeThreatMediaHighlightImporter>) serviceProvider.GetService(
                typeof(ILogger<FireEyeThreatMediaHighlightImporter>));
            _settings = (ApplicationSettings) serviceProvider.GetService(typeof(ApplicationSettings));
        }
        
        public override async IAsyncEnumerable<SubmittedDocument> PullAsync(DateTime? lastPull, int limit)
        {
            _logger.LogDebug(
                $"Pulling {GetType().FullName} from {lastPull?.ToString() ?? "(not date)"} but max {limit} documents.");
            
            var importSettings = _importer.Settings.ToObject<FireEyeSettings>();
            
            if (!string.IsNullOrEmpty(importSettings.ApiKey) && !string.IsNullOrEmpty(importSettings.SecretKey))
            {
                var client = new FireEyeAPI(importSettings.ApiKey, importSettings.SecretKey,
                    proxy: new WebProxy("http://" + _settings.Proxy + "/", true, new[] {_settings.NoProxy}));
                var reportParameters = new ThreatMediaHighlightParameters();
                reportParameters.limit = limit;
                if (lastPull != null) reportParameters.since = lastPull;

                var reports = client.TMHReportIndex(reportParameters);
                foreach (var report in reports)
                    yield return new SubmittedDocument
                    {
                        Title = report.title,
                        URL = report.webLink
                    };
            }
            else
            {
                _logger.LogDebug("Invalid AccessKey or SecretKey");
            }
        }

        public override Type GetSettingsType()
        {
            return (typeof(FireEyeSettings));
        }

        class FireEyeSettings
        {
            public string ApiKey { get; set; }
            public string SecretKey { get; set; }
        }
    }
}