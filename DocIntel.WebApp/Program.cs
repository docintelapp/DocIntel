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
using System.IO;
using DocIntel.Core.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

using NLog;
using NLog.Web;

using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace DocIntel.WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Logger logger;
            if (File.Exists("nlog.config"))
                logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            else if (File.Exists("/etc/docintel/nlog.config"))
                logger = NLogBuilder.ConfigureNLog("/etc/docintel/nlog.config").GetCurrentClassLogger();
            else
                throw new FileNotFoundException("nlog.config");

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
                throw;
            }
            finally
            {
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                LogManager.Shutdown();
            }
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
