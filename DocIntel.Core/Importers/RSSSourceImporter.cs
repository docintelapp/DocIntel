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
using System.ServiceModel.Syndication;
using System.Xml;

using AngleSharp.Text;

using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;

using Microsoft.Extensions.Logging;

namespace DocIntel.Core.Importers
{
    [Importer("0ade9c2e-165c-408e-93db-06483af6d07a", Name = "RSS Source Importer",
        Description = "Import news feed from sources")]
    // ReSharper disable once UnusedType.Global because it is used, but with reflexion
    public class RSSSourceImporter : DefaultImporter
    {
        private readonly Importer _importer;
        private readonly ILogger<RSSSourceImporter> _logger;
        private readonly ApplicationSettings _settings;
        private readonly ISourceRepository _sourceRepository;

        public RSSSourceImporter(IServiceProvider serviceProvider, Importer importer) : base(serviceProvider)
        {
            _importer = importer;
            _logger = (ILogger<RSSSourceImporter>) serviceProvider.GetService(typeof(ILogger<RSSSourceImporter>));
            _sourceRepository = (ISourceRepository) serviceProvider.GetService(typeof(ISourceRepository));
            _settings = (ApplicationSettings) serviceProvider.GetService(typeof(ApplicationSettings));
        }

        public override async IAsyncEnumerable<SubmittedDocument> PullAsync(DateTime? lastPull, int limit)
        {
            _logger.LogDebug(
                $"Pulling {GetType().FullName} from {lastPull?.ToString() ?? "(not date)"} but max {limit} documents.");

            var context = GetContext();
            var sources = await _sourceRepository
                .GetAllAsync(context, _ => _.Where(s => !string.IsNullOrEmpty(s.RSSFeed))).ToListAsync();

            foreach (var source in sources)
            {
                _logger.LogInformation("Source : " + source.Title);
                if (source.MetaData != null && source.MetaData.ContainsKey("rss_enabled") && source.MetaData.Value<bool>("rss_enabled"))
                {
                    var lastSourcePull = source.MetaData.Value<DateTime>("rss_last_pull");

                    var httpWebRequest = (HttpWebRequest) WebRequest.Create(source.RSSFeed);
                    if (!string.IsNullOrEmpty(_settings.Proxy))
                        httpWebRequest.Proxy =
                            // new WebProxy(_settings.Proxy, true);
                            new WebProxy(_settings.Proxy, true, _settings.NoProxy.Split(new char[] {',',';'}, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

                    SyndicationFeed feed = null;
                    try
                    {
                        using var httpWebResponse = (HttpWebResponse) httpWebRequest.GetResponse();
                        await using var responseStream = httpWebResponse.GetResponseStream();
                        using var reader = XmlReader.Create(responseStream);
                        reader.Settings.DtdProcessing = _settings.Security?.DtdProcessing ?? DtdProcessing.Prohibit;
                    
                        feed = SyndicationFeed.Load(reader);
                    }
                    catch (System.Net.WebException e)
                    {
                        _logger.LogError(e.Message);
                        _logger.LogError(e.StackTrace);
                        _logger.LogError(e.Response?.ToString());
                    }

                    if (feed != null)
                    {
                        foreach (var item in feed.Items)
                        {
                            var subject = item.Title.Text;
                            var summary = item.Summary.Text;
                            var link = item.Links.FirstOrDefault()?.Uri;
                            var date = item.PublishDate;

                            if ((!source.MetaData.ContainsKey("rss_last_pull") || date > lastSourcePull) && link != default)
                            {
                                var keywords = source.MetaData.Value<string>("rss_keywords")?.SplitSpaces() ??
                                               new string[] { };
                                if (keywords.Length > 0 && !(subject + summary).SplitSpaces()
                                    .Any(_ => keywords.Contains(_)))
                                {
                                    _logger.LogTrace("Keyword not found, skip...");
                                    continue;
                                }

                                _logger.LogTrace("Importing " + subject);

                                yield return new SubmittedDocument
                                {
                                    Title = subject,
                                    Description = summary,
                                    URL = link.ToString()
                                };
                            }
                        }

                        source.MetaData["rss_last_pull"] = feed.Items.Max(_ => _.PublishDate);
                        _logger.LogTrace(source.MetaData.ToString());
                        await _sourceRepository.UpdateAsync(context, source);
                        await context.DatabaseContext.SaveChangesAsync();
                    }
                }
                else
                {
                    _logger.LogTrace("Source has no rss_enabled");
                }
                await context.DatabaseContext.SaveChangesAsync();
            }
        }
    }
}
