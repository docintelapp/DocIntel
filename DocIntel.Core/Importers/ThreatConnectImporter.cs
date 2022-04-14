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
using DocIntel.Integrations.ThreatConnect;

using Microsoft.Extensions.Logging;

namespace DocIntel.Core.Importers
{
    [Importer("488444a2-2b77-49d0-9a4e-eaa188ba72ba", Name = "ThreatConnect",
        Description = "Importer for ThreatConnect")]
    // ReSharper disable once UnusedType.Global because it is used, but with reflexion
    public class ThreatConnectImporter : DefaultImporter
    {
        private readonly Importer _importer;

        private readonly ILogger<ThreatConnectImporter> _logger;
        private readonly ApplicationSettings _settings;

        public ThreatConnectImporter(IServiceProvider serviceProvider, Importer importer) : base(serviceProvider)
        {
            _logger = (ILogger<ThreatConnectImporter>) serviceProvider.GetService(
                typeof(ILogger<ThreatConnectImporter>));
            _settings = (ApplicationSettings) serviceProvider.GetService(typeof(ApplicationSettings));
            _importer = importer;
        }

        [ImporterSetting("AccessId")] public string AccessId { get; set; }

        [ImporterSetting("SecretKey")] public string SecretKey { get; set; }

        [ImporterSetting("Owner")] public string Owner { get; set; }

        public override async IAsyncEnumerable<SubmittedDocument> PullAsync(DateTime? lastPull, int limit)
        {
            _logger.LogDebug(
                $"Pulling {GetType().FullName} from {lastPull?.ToString() ?? "(not date)"} but max {limit} documents.");

            if (!string.IsNullOrEmpty(AccessId) && !string.IsNullOrEmpty(SecretKey))
            {
                var client = new APIClient(AccessId, SecretKey,
                    new WebProxy("http://" + _settings.Proxy + "/", true, new[] {_settings.NoProxy}));
                var groupParameter = new GroupParameter {Owner = Owner, Limit = limit};
                if (lastPull != null) groupParameter.DateAdded = ">" + lastPull?.ToString("yyyy-MM-dd");

                var groups = await client.Groups.GetGroups(GroupType.Reports, groupParameter);
                foreach (var report in groups.Data.Report)
                    yield return new SubmittedDocument
                    {
                        Title = report.Name,
                        URL = report.WebLink
                    };

                groups = await client.Groups.GetGroups(GroupType.Documents, groupParameter);
                foreach (var report in groups.Data.Document)
                    yield return new SubmittedDocument
                    {
                        Title = report.Name,
                        URL = report.WebLink
                    };
            }
            else
            {
                _logger.LogDebug("Invalid AccessKey or SecretKey");
            }
        }
    }
}