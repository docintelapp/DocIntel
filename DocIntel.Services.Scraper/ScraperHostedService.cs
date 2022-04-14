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
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using DocIntel.Core.Services;
using DocIntel.Core.Settings;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using PuppeteerSharp;

namespace DocIntel.Services.Scraper
{
    class ScraperHostedService : DocIntelHostedService
    {
        protected override string WorkerName => "Scraper";

        public ScraperHostedService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        protected override async Task Run(CancellationToken cancellationToken)
        {
            var _logger = _serviceProvider.GetService<ILogger<ScraperHostedService>>();
            var _settings = _serviceProvider.GetService<ApplicationSettings>();
            try
            {
                var browserFetcher = new BrowserFetcher();
                _logger.LogDebug("Browser Fetcher Initialized");
                if (!string.IsNullOrEmpty(_settings.Proxy))
                {
                    browserFetcher.WebProxy = new WebProxy(_settings.Proxy);
                    _logger.LogDebug("Configuring proxy " + _settings.Proxy);
                }
                _logger.LogDebug("Browser Fetcher Proxy");
                await browserFetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);
                _logger.LogDebug("Browser Downloaded");
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                _logger.LogError(e.StackTrace);
            }
            
            var runner = _serviceProvider.GetService<ScraperConsumer>();
            if (runner != null) await runner.ConsumeBacklogAsync();
            else throw new InvalidOperationException("Could not create instance of consumer");
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
    }
}