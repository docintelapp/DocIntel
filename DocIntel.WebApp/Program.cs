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

using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using DocIntel.Core.Helpers;
using DocIntel.Core.Services;
using DocIntel.Core.Settings;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

using NLog;
using NLog.Web;
using Synsharp.Telepath;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace DocIntel.WebApp
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            int ret = 0;
            if ((ret = await FlightChecks.PreFlightChecks()) > 0)
            {
                return ret;
            }
            
            Logger logger = null;
            if (File.Exists("nlog.config"))
                logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            else if (File.Exists("/config/nlog.config"))
                logger = NLogBuilder.ConfigureNLog("/config/nlog.config").GetCurrentClassLogger();
            else if (File.Exists("/etc/docintel/nlog.config"))
                logger = NLogBuilder.ConfigureNLog("/etc/docintel/nlog.config").GetCurrentClassLogger();
            
            try
            {
                var builder = CreateWebHostBuilder(args);
                var host = builder.Build();

                host.Run();
            }
            catch (Exception ex)
            {
                //NLog: catch setup errors
                logger.Error(ex, "Stopped program because of exception");
                return 1;
            }
            finally
            {
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                LogManager.Shutdown();
            }

            return 0;
        }

        public static IHostBuilder CreateWebHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(DocIntelServiceProgram.ConfigureAppConfiguration)
                .ConfigureLogging(DocIntelServiceProgram.ConfigureLogging)
                .UseSystemd()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
                        .UseSetting(WebHostDefaults.ApplicationKey,
                            typeof(Program).Assembly.FullName);
                })
                .UseNLog();
        }
    }
}
