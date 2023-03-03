/* DocIntel
 * Copyright (C) 2018-2023 Belgian Defense, Antoine Cailliau
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

using System.Reflection;
using System.Threading.Tasks;
using DocIntel.Core.Helpers;
using DocIntel.Core.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NLog.Web;

namespace DocIntel.Services.Scraper
{
    internal class Program : DocIntelServiceProgram
    {
        public static async Task<int> Main(string[] args)
        {
            int ret = 0;
            if ((ret = await FlightChecks.PreFlightChecks()) > 0)
            {
                return ret;
            }
            
            await CreateHostBuilder(args).Build().RunAsync();
            return 0;
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .ConfigureAppConfiguration(ConfigureAppConfiguration)
                .ConfigureLogging(ConfigureLogging)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddScoped<ScraperConsumer>();
                    services.AddHostedService<ScraperHostedService>();
                    ConfigureService(hostContext, services, new Assembly[] { typeof(Program).Assembly }, true);
                })
                .UseNLog();
    }
}