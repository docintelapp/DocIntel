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
using System.Linq;
using System.Reflection;

using DocIntel.Core.Authentication;
using DocIntel.Core.Authorization;
using DocIntel.Core.Helpers;
using DocIntel.Core.Models;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils;
using DocIntel.Core.Utils.Indexation.SolR;
using MassTransit;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NLog.Web;

using Npgsql;

using RunMethodsSequentially;

namespace DocIntel.Core.Services
{
    public abstract class DocIntelServiceProgram
    {
        public static void ConfigureLogging(HostBuilderContext hostingContext, ILoggingBuilder logging)
        {
            var env = hostingContext.HostingEnvironment;
            
            var configFiles = new string[]
            {
                "/etc/docintel/nlog.config",
                $"/etc/docintel/nlog.{env.EnvironmentName}.config",
                $"/etc/docintel/nlog.{env.ApplicationName}.config",
                "/config/nlog.config",
                $"/config/nlog.{env.EnvironmentName}.config",
                $"/config/nlog.{env.ApplicationName}.config",
                "nlog.config",
                $"nlog.{env.EnvironmentName}.config",
                $"nlog.{env.ApplicationName}.config"
            };
            
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Trace);

            bool success = false;
            foreach (var configFile in configFiles.Reverse())
            {
                if (File.Exists(configFile))
                {
                    logging.AddNLog(configFile);
                    success = true;
                    break;
                }
            }

            if (!success)
            {
                // TODO Raise custom exception
                throw new FileNotFoundException("Could not find logging configuration file 'nlog.config'.");
            }

        }

        public static void ConfigureAppConfiguration(HostBuilderContext hostingContext, IConfigurationBuilder config)
        {
            var env = hostingContext.HostingEnvironment;
            
            var configFiles = new string[]
            {
                "/etc/docintel/appsettings.json",
                $"/etc/docintel/appsettings.{env.EnvironmentName}.json",
                $"/etc/docintel/appsettings.{env.ApplicationName}.json",
                "/config/appsettings.json",
                $"/config/appsettings.{env.EnvironmentName}.json",
                $"/config/appsettings.{env.ApplicationName}.json",
                "appsettings.json",
                $"appsettings.{env.EnvironmentName}.json",
                $"appsettings.{env.ApplicationName}.json"
            };
            foreach (var configFile in configFiles)
            {
                if (File.Exists(configFile))
                {
                    config.AddJsonFile(configFile, optional: true);
                }
            }
            config.AddEnvironmentVariables();
        }

        public static void ConfigureService(HostBuilderContext hostContext, IServiceCollection serviceCollection,
            Assembly[] consumerAssemblies = null, bool runHostedServices = false)
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
            
            serviceCollection.AddScoped<AppRoleManager, AppRoleManager>();
                
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
                serviceCollection.AddScoped<UserManager<AppUser>, AppUserManager>();

                serviceCollection.AddIdentity<AppUser, AppRole>()
                    .AddSignInManager<AppSignInManager>()
                    .AddUserManager<AppUserManager>()
                    .AddEntityFrameworkStores<DocIntelContext>()
                    .AddDefaultTokenProviders();
            }
            
            StartupHelpers.RegisterAuthorizationHandlers(serviceCollection);
            StartupHelpers.RegisterIndexingServices(serviceCollection);
            StartupHelpers.RegisterSearchServices(serviceCollection);
            StartupHelpers.RegisterRepositories(serviceCollection);
            StartupHelpers.RegisterSolR(serviceCollection, appSettings);
            StartupHelpers.RegisterSynapse(serviceCollection, appSettings);
            
            /*
            serviceCollection.AddIdentity<AppUser, AppRole>()
                .AddEntityFrameworkStores<DocIntelContext>()
                .AddDefaultTokenProviders();
            */
            
            serviceCollection.AddAuthorization();
            serviceCollection.AddTransient<IUserClaimsPrincipalFactory<AppUser>, AppUserClaimsPrincipalFactory>();
            serviceCollection.AddTransient<AppUserClaimsPrincipalFactory, AppUserClaimsPrincipalFactory>();

            serviceCollection.AddMassTransit(x =>
            {
                if (consumerAssemblies != null)
                    foreach (var assembly in consumerAssemblies)
                        x.AddConsumers(assembly);

                x.SetKebabCaseEndpointNameFormatter();
                x.UsingRabbitMq((context, cfg) => {
                    cfg.Host(appSettings.RabbitMQ.Host, appSettings.RabbitMQ.VirtualHost, h =>
                    {
                        h.Username(appSettings.RabbitMQ.Username);
                        h.Password(appSettings.RabbitMQ.Password);
                    });
                    cfg.ConfigureEndpoints(context);
                });
            });
            
            serviceCollection.AddAutoMapper(cfg => {
                cfg.AddProfile<SolRProfile>();
            });

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            serviceCollection
                .AddDbContext<DocIntelContext>(options =>
                {
                    options.UseNpgsql(connectionString,
                        x => x.MigrationsAssembly("DocIntel.Core"));
                }, contextLifetime: ServiceLifetime.Transient);

            var lockFolder = appSettings.LockFolder;
            if (string.IsNullOrEmpty(lockFolder))
                lockFolder = ".";
            
            serviceCollection.RegisterRunMethodsSequentially(options =>
            {
                options.RegisterAsHostedService = runHostedServices;
                options.AddPostgreSqlLockAndRunMethods(connectionString);
                options.AddFileSystemLockAndRunMethods(lockFolder);
            }).RegisterServiceToRunInJob<MigrateDbContextService>()
                .RegisterServiceToRunInJob<BaseDataDbService>()
                .RegisterServiceToRunInJob<InstallSynapseCustomObjects>();
            
        }
    }
}