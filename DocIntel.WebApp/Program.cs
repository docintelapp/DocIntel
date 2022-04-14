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

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
                var host = CreateWebHostBuilder(args).Build();

                // using (var scope = host.Services.CreateScope())
                // {
                //     var services = scope.ServiceProvider;
                //     try
                //     {
                //         var context = services.GetRequiredService<DocIntelContext>();
                //         var logger = services.GetRequiredService<ILogger<DbInitializer>>();
                //         DbInitializer.Initialize(context, logger);
                //     }
                //     catch (Exception ex)
                //     {
                //         var logger = services.GetRequiredService<ILogger<Program>>();
                //         logger.LogError(ex, "An error occurred while seeding the database.");
                //     }
                // }

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

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var env = hostingContext.HostingEnvironment;

                    config.AddJsonFile("/etc/docintel/appsettings.json", true);
                    config.AddJsonFile("appsettings.json", true);
                    config.AddJsonFile($"appsettings.{env.EnvironmentName}.json", true);

                    config.AddEnvironmentVariables();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Trace);
                })
                .UseNLog();
        }
    }
}