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
    [Importer("c7427a98-a5e5-47f3-ae62-ae2acf726fec", Name = "FireEye", Description = "Importer for FireEye")]
    // ReSharper disable once UnusedType.Global because it is used, but with reflexion
    public class FireEyeImporter : DefaultImporter
    {
        private readonly Importer _importer;

        private readonly ILogger<FireEyeImporter> _logger;
        private readonly ApplicationSettings _settings;
        public override bool HasSettings => true;
        public FireEyeImporter(IServiceProvider serviceProvider, Importer importer) : base(serviceProvider)
        {
            _importer = importer;
            _logger = (ILogger<FireEyeImporter>) serviceProvider.GetService(typeof(ILogger<FireEyeImporter>));
            _settings = (ApplicationSettings) serviceProvider.GetService(typeof(ApplicationSettings));
        }

        public override async IAsyncEnumerable<SubmittedDocument> PullAsync(DateTime? lastPull, int limit)
        {
            _logger.LogDebug(
                $"Pulling {GetType().FullName} from {lastPull?.ToString() ?? "(not date)"} but max {limit} documents.");
            
            var importSettings = _importer.Settings.ToObject<FireEyeSettings>();
            
            if (!string.IsNullOrEmpty(importSettings.ApiKey) && !string.IsNullOrEmpty(importSettings.SecretKey))
            {
                WebProxy webProxy = null;
                if (!string.IsNullOrEmpty(_settings.Proxy))
                    webProxy = new WebProxy("http://" + _settings.Proxy + "/", true, new[] {_settings.NoProxy});

                var client = webProxy != null
                    ? new FireEyeAPI(importSettings.ApiKey, importSettings.SecretKey, proxy: webProxy)
                    : new FireEyeAPI(importSettings.ApiKey, importSettings.SecretKey);

                var reportParameters = new ReportParameters {limit = limit, since = DateTime.UtcNow.AddDays(-7)};

                if (lastPull != null) reportParameters.since = lastPull;

                var reports = client.ReportIndex(reportParameters);
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
        
        public override string GetSettingsView()
        {
            return "_FireEyeSettings";
        }

        public class FireEyeSettings
        {
            public string ApiKey { get; set; }
            public string SecretKey { get; set; }
        }
    }
}