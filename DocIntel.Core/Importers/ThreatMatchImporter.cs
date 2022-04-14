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

using DocIntel.Core.Models;
using DocIntel.Core.Settings;
using DocIntel.Integrations.ThreatMatch;

using Microsoft.Extensions.Logging;

namespace DocIntel.Core.Importers
{
    [Importer("b58f59d7-e6a4-4ca5-809f-5667794586e8", Name = "ThreatMatch", Description = "Importer for ThreatMatch")]
    // ReSharper disable once UnusedType.Global because it is used, but with reflexion
    public class ThreatMatchImporter : DefaultImporter
    {
        private readonly ILogger<ThreatMatchImporter> _logger;
        private readonly Importer _importer;
        private readonly ApplicationSettings _settings;

        [ImporterSetting("Username")]
        // ReSharper disable once MemberCanBePrivate.Global TODO Check if necessary to be public
        // ReSharper disable once UnusedAutoPropertyAccessor.Global because it is used for creating the form dynamically.
        public string Username { get; set; }
        
        [ImporterSetting("APIKey", Type = AttributeFieldType.Password)]
        // ReSharper disable once MemberCanBePrivate.Global TODO Check if necessary to be public
        // ReSharper disable once UnusedAutoPropertyAccessor.Global because it is used for creating the form dynamically.
        public string APIKey { get; set; }

        public ThreatMatchImporter(IServiceProvider serviceProvider, Importer importer) : base(serviceProvider)
        {
            _logger = (ILogger<ThreatMatchImporter>) serviceProvider.GetService(typeof(ILogger<ThreatMatchImporter>));
            _settings = (ApplicationSettings) serviceProvider.GetService(typeof(ApplicationSettings));
            _importer = importer;
        }
        public override async IAsyncEnumerable<SubmittedDocument> PullAsync(DateTime? lastPull, int limit)
        {
            _logger.LogDebug(
                $"Pulling {this.GetType().FullName} from {(lastPull?.ToString() ?? "(not date)")} but max {limit} documents.");
            
            if (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(APIKey))
            {
                WebProxy webProxy = null;
                if (!string.IsNullOrEmpty(_settings.Proxy))
                    webProxy = new WebProxy("http://" + _settings.Proxy + "/", true, new string[] {_settings.NoProxy});
                var client = webProxy != null ? new APIClient(Username, APIKey, webProxy) : new APIClient(Username, APIKey);
                var reports = client.Reports.GetReports(lastPull != null
                    ? new ReportFilter()
                    {
                        DateFrom = lastPull
                    }
                    : new ReportFilter()
                    {
                        DateFrom = DateTime.UtcNow.AddDays(-7)
                    });

                foreach (var report in reports.OrderByDescending(_ => _.PublishedAt).Take(limit))
                {
                    yield return new SubmittedDocument()
                    {
                        Title = report.Title,
                        URL = "https://eu.threatmatch.com/app/reports/view/" + report.Id
                    };
                }
            }
        }
    }
}