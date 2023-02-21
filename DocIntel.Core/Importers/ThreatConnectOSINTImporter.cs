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

using DocIntel.Core.Models;
using DocIntel.Core.Settings;
using DocIntel.Integrations.ThreatConnect;

using Markdig;

using Microsoft.Extensions.Logging;

namespace DocIntel.Core.Importers
{
    [Importer("8e6f2421-5440-4f26-bec9-057e8365319d", Name = "ThreatConnect OSINT", Description = "Importer for OSINT")]
    // ReSharper disable once UnusedType.Global because it is used, but with reflexion
    public class ThreatConnectOSINTImporter : DefaultImporter
    {
        private readonly Importer _importer;

        private readonly ILogger<ThreatConnectOSINTImporter> _logger;
        private readonly ApplicationSettings _settings;
        public override bool HasSettings => true;

        public ThreatConnectOSINTImporter(IServiceProvider serviceProvider, Importer importer) : base(serviceProvider)
        {
            _logger = (ILogger<ThreatConnectOSINTImporter>) serviceProvider.GetService(
                typeof(ILogger<ThreatConnectOSINTImporter>));
            _settings = (ApplicationSettings) serviceProvider.GetService(typeof(ApplicationSettings));
            _importer = importer;
        }


        public override async IAsyncEnumerable<SubmittedDocument> PullAsync(DateTime? lastPull, int limit)
        {
            _logger.LogDebug(
                $"Pulling {GetType().FullName} from {lastPull?.ToString() ?? "(not date)"} but max {limit} documents.");

            var importSettings = _importer.Settings.ToObject<ThreatConnectSettings>();
            
            if (!string.IsNullOrEmpty(importSettings.AccessId) && !string.IsNullOrEmpty(importSettings.SecretKey))
            {
                WebProxy webProxy = null;
                if (!string.IsNullOrEmpty(_settings.Proxy))
                    webProxy = new WebProxy("http://" + _settings.Proxy + "/", true, new[] {_settings.NoProxy});

                var client = webProxy != null
                    ? new APIClient(importSettings.AccessId, importSettings.SecretKey, proxy: webProxy)
                    : new APIClient(importSettings.AccessId, importSettings.SecretKey);

                var groupParameter = new GroupParameter {Owner = importSettings.Owner, Limit = 100};
                if (lastPull != null)
                    groupParameter.DateAdded = ">" + ((DateTime) lastPull).ToString("yyyy-MM-dd");
                else
                    groupParameter.DateAdded = ">" + DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");

                GroupType groupType;
                if (importSettings.Endpoint == "incidents")
                    groupType = GroupType.Incidents;
                else
                    yield break;

                var groups = await client.Groups.GetGroups(groupType, groupParameter);
                foreach (var report in groups.Data.Incident)
                {
                    var attributeResponse =
                        await client.Groups.GetGroupAttributes(groupType, report.Id, groupParameter);
                    var attributes = attributeResponse.Data.Attribute;
                    var source = attributes.SingleOrDefault(_ => _.Type == "Source")?.Value;
                    var description = attributes.SingleOrDefault(_ => _.Type == "Description")?.Value;

                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        var pipeline = new MarkdownPipelineBuilder().UseBootstrap().Build();
                        description = Markdown.ToHtml(description, pipeline);
                    }

                    var blacklisted = false;
                    foreach (var line in importSettings.Blacklist.Split(new[] {Environment.NewLine},
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        if (Regex.Match(source, line).Success)
                        {
                            _logger.LogDebug("URL is blacklisted.");
                            blacklisted = true;
                            break;
                        }

                    if (!string.IsNullOrEmpty(source) & !blacklisted)
                        yield return new SubmittedDocument
                        {
                            Title = report.Name,
                            Description = description,
                            URL = source
                        };
                }
            }
            else
            {
                _logger.LogDebug("Invalid AccessKey or SecretKey");
            }
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
            public string Endpoint { get; set; }
            public string Blacklist { get; set; }
        }
    }
}