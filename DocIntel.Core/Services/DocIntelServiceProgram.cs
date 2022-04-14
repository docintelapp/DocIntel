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
using System.Reflection;

using DocIntel.Core.Authentication;
using DocIntel.Core.Authorization;
using DocIntel.Core.Helpers;
using DocIntel.Core.Models;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils;
using DocIntel.Core.Utils.Indexation.SolR;
using DocIntel.Core.Utils.Observables;

using MassTransit;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NLog.Web;

using Npgsql;

namespace DocIntel.Core.Services
{
    public abstract class DocIntelServiceProgram
    {
        public static void ConfigureLogging(ILoggingBuilder logging)
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Trace);
            
            if (File.Exists("nlog.config"))
                logging.AddNLog("nlog.config");
            else if (File.Exists("/etc/docintel/nlog.config"))
                logging.AddNLog("/etc/docintel/nlog.config");
            else
                throw new FileNotFoundException("nlog.config");
        }

        public static void ConfigureAppConfiguration(HostBuilderContext hostingContext, IConfigurationBuilder config)
        {
            var env = hostingContext.HostingEnvironment;
            config.AddJsonFile("/etc/docintel/appsettings.json", optional: true);
            config.AddJsonFile("appsettings.json", optional: true);
            config.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);
            config.AddEnvironmentVariables();
        }

        public static void ConfigureService(HostBuilderContext hostContext, IServiceCollection serviceCollection,
            Assembly[] consumerAssemblies = null)
        {
            NpgsqlConnection.GlobalTypeMapper.UseJsonNet();
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            
            var configuration = hostContext.Configuration;
            EmailSettings emailSettings = new EmailSettings();
            LdapSettings ldapSettings = new LdapSettings();
            ApplicationSettings appSettings = new ApplicationSettings();
            
            configuration.GetSection("Email").Bind(emailSettings);
            configuration.GetSection("LDAP").Bind(ldapSettings);
            configuration.Bind(appSettings);
            
            serviceCollection
                .AddSingleton(configuration)
                .AddSingleton(appSettings)
                .AddSingleton(emailSettings);

            serviceCollection.AddTransient<MailKitEmailSender>();
            
            var authenticationMethod = configuration.GetValue<string>("AuthenticationMethod");
            if (authenticationMethod != null && authenticationMethod.ToUpper() == "LDAP")
            {
                Console.WriteLine("Uses LDAP authentication");
                serviceCollection.AddScoped<ILdapService, ActiveDirectoryLdapService>();
                serviceCollection.AddScoped<LdapUserManager, LdapUserManager>();
                serviceCollection.AddScoped<SignInManager<AppUser>, LdapSignInManager>();

                serviceCollection.AddSingleton(ldapSettings);

                serviceCollection.AddIdentity<AppUser, AppRole>()
                    .AddUserManager<LdapUserManager>()
                    .AddSignInManager<LdapSignInManager>()
                    .AddEntityFrameworkStores<DocIntelContext>();
            }
            else
            {
                Console.WriteLine("Uses native authentication");
                serviceCollection.AddScoped<SignInManager<AppUser>, AppSignInManager>();

                serviceCollection.AddIdentity<AppUser, AppRole>()
                    .AddSignInManager<AppSignInManager>()
                    .AddEntityFrameworkStores<DocIntelContext>()
                    .AddDefaultTokenProviders();
            }
            
            StartupHelpers.RegisterAuthorizationHandlers(serviceCollection);
            StartupHelpers.RegisterIndexingServices(serviceCollection);
            StartupHelpers.RegisterSearchServices(serviceCollection);
            StartupHelpers.RegisterRepositories(serviceCollection);
            StartupHelpers.RegisterSolR(serviceCollection);
            
            /*
            serviceCollection.AddIdentity<AppUser, AppRole>()
                .AddEntityFrameworkStores<DocIntelContext>()
                .AddDefaultTokenProviders();
            */
            
            serviceCollection.AddAuthorization();
            
            serviceCollection.AddScoped<IUserClaimsPrincipalFactory<AppUser>, AppUserClaimsPrincipalFactory>();

            serviceCollection.AddMassTransit(x =>
            {
                if (consumerAssemblies != null)
                    foreach (var assembly in consumerAssemblies)
                        x.AddConsumers(assembly);

                x.SetKebabCaseEndpointNameFormatter();
                x.UsingRabbitMq((context, cfg) => {
                    cfg.ConfigureEndpoints(context);
                });
            });
            serviceCollection.AddMassTransitHostedService();
            
            serviceCollection.AddAutoMapper(cfg => {
                cfg.AddProfile<SolRProfile>();
                cfg.AddProfile<ElasticProfile>();
            });

            serviceCollection
                .AddDbContext<DocIntelContext>(options =>
                    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                        x => x.MigrationsAssembly("DocIntel.Core")));
        }
    }
}