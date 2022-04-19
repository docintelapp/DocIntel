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
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DocIntel.Core.Authentication;
using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Helpers;
using DocIntel.Core.Models;
using DocIntel.Core.Services;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils;
using DocIntel.Core.Utils.Indexation.SolR;
using DocIntel.Core.Utils.Observables;
using DocIntel.WebApp.Areas.API;
using DocIntel.WebApp.Helpers;

using MassTransit;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using Newtonsoft.Json;

using Npgsql;

using RunMethodsSequentially;

namespace DocIntel.WebApp
{
    public class Startup
    {
        private readonly IWebHostEnvironment _environment;
        protected ApplicationSettings appSettings = new();
        protected LdapSettings ldapSettings = new();

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Configuration.GetSection("LDAP").Bind(ldapSettings);
            Configuration.Bind(appSettings);
            _environment = environment;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public virtual void ConfigureServices(IServiceCollection services)
        {
            Console.WriteLine("--- Configuring service");

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings {
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };
            
            services.AddMvc();

            NpgsqlConnection.GlobalTypeMapper.UseJsonNet();
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            var policyBuilder = new AuthorizationPolicyBuilder(CookieAuthenticationDefaults.AuthenticationScheme,
                JwtBearerDefaults.AuthenticationScheme);
            policyBuilder.RequireAuthenticatedUser();
            var policy = policyBuilder.Build();

            services.AddMvc(options => { options.Filters.Add(new AuthorizeFilter(policy)); })
                .AddNewtonsoftJson(t =>
                {
                    t.SerializerSettings.Formatting = Formatting.Indented;
                    t.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                });

            services.AddControllersWithViews()
                .AddNewtonsoftJson(t =>
                {
                    t.SerializerSettings.Formatting = Formatting.Indented;
                    t.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                })
                .AddRazorRuntimeCompilation();

            services.AddSingleton(Configuration);
            services.AddSingleton(appSettings);
            

            var authenticationMethod = Configuration.GetValue<string>("AuthenticationMethod");
            if (authenticationMethod != null && authenticationMethod.ToUpper() == "LDAP")
            {
                Console.WriteLine("Uses LDAP authentication");
                services.AddScoped<ILdapService, ActiveDirectoryLdapService>();
                services.AddScoped<LdapUserManager, LdapUserManager>();
                services.AddScoped<SignInManager<AppUser>, LdapSignInManager>();

                services.AddSingleton(ldapSettings);

                services.AddIdentity<AppUser, AppRole>()
                    .AddUserManager<LdapUserManager>()
                    .AddSignInManager<LdapSignInManager>()
                    .AddEntityFrameworkStores<DocIntelContext>();
            }
            else
            {
                Console.WriteLine("Uses native authentication");
                services.AddTransient<SignInManager<AppUser>, AppSignInManager>();
                services.AddTransient<UserManager<AppUser>, AppUserManager>();

                services.AddIdentity<AppUser, AppRole>()
                    .AddSignInManager<AppSignInManager>()
                    .AddUserManager<AppUserManager>()
                    .AddEntityFrameworkStores<DocIntelContext>()
                    .AddDefaultTokenProviders();
            }

            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = Configuration["Jwt:Issuer"],
                        ValidAudience = Configuration["Jwt:Issuer"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"])),
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.SlidingExpiration = true;
                    options.Cookie.HttpOnly = true;
                    options.ExpireTimeSpan = TimeSpan.FromDays(1);
                    options.SlidingExpiration = true;
                    options.ForwardAuthenticate = "Identity.Application";
                });

            services.AddAuthorization(options => { options.DefaultPolicy = policy; });

            services.AddScoped<IUserClaimsPrincipalFactory<AppUser>, AppUserClaimsPrincipalFactory>();

            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<DocIntelContext>(options =>
            {
                options.EnableSensitiveDataLogging();
                options.UseNpgsql(connectionString,
                    x =>
                    {
                        x.SetPostgresVersion(9, 5);
                        x.MigrationsAssembly("DocIntel.Core");
                        x.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    });
            });
            
            var lockFolder = appSettings.LockFolder;
            if (string.IsNullOrEmpty(lockFolder))
                lockFolder = "./wwwroot";
            
            services.RegisterRunMethodsSequentially(options =>
            {
                options.AddPostgreSqlLockAndRunMethods(connectionString);
                options.AddFileSystemLockAndRunMethods(lockFolder);
            })
                .RegisterServiceToRunInJob<MigrateDbContextService>()
                .RegisterServiceToRunInJob<BaseDataDbService>()
                .RegisterServiceToRunInJob<InstallSynapseCustomObjects>();

            StartupHelpers.RegisterServices(services);
            StartupHelpers.RegisterSolR(services, appSettings);
            StartupHelpers.RegisterSynapse(services, appSettings);

            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<SolRProfile>();
                cfg.AddProfile<APIProfile>();
            });

            services.AddTransient<ProblemDetailsFactory, CustomProblemDetailsFactory>();
            
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v0.1",
                    Title = "DocIntel API",
                    Description = "API for DocIntel"
                });
                c.CustomOperationIds(e =>
                    $"{e.ActionDescriptor.RouteValues["controller"]}{e.ActionDescriptor.RouteValues["action"]}{e.HttpMethod.FirstCharToUpper()}");

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme."
                });

                c.OperationFilter<BearerAuthOperationsFilter>();
                /*c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                          new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference 
                                { 
                                    Type = ReferenceType.SecurityScheme, 
                                    Id = "Bearer" 
                                }
                            },
                            new string[] {} 
                    }
                });*/

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            services.AddHealthChecks();

            services.Configure<HealthCheckPublisherOptions>(options =>
            {
                options.Delay = TimeSpan.FromSeconds(2);
                options.Predicate = check => check.Tags.Contains("ready");
            });

            services.AddMassTransit(x =>
            {
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
            
            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseDeveloperExceptionPage();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "DocIntel API v0.1"); });

            app.UseRouting();

            // Serve static files directly
            var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            if (!string.IsNullOrEmpty(appSettings.StaticFiles))
            {
                if (appSettings.StaticFiles.StartsWith("/"))
                    root = Path.Combine(appSettings.StaticFiles);
                else
                    root = Path.Combine(Directory.GetCurrentDirectory(), appSettings.StaticFiles);
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    root
                ),
                RequestPath = new PathString()
            });

            // Get the correct information forwarded by proxies
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            // Must be before UseAuthentication
            app.UseStatusCodePages(context =>
            {
                var request = context.HttpContext.Request;
                var response = context.HttpContext.Response;

                // Required to redirect the user to the login page when not requesting JWT authentication scheme or
                // when querying from XMLHttpRequest
                if (response.StatusCode == (int) HttpStatusCode.Unauthorized
                    && (
                        request.Headers.All(_ => _.Key != "Authentication")
                        || request.Headers.Contains(
                            new KeyValuePair<string, StringValues>("X-Requested-With", "XMLHttpRequest"))
                    ))
                    response.Redirect("/Account/Login");
                return Task.CompletedTask;
            });

            // Must be after UseRouting but before UseEndpoints
            app.UseAuthentication();
            app.UseAuthorization();
            /*
            app.Use(async (context, next) =>
            {
                // Use this if there are multiple authentication schemes
                var authResult = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
                if (authResult.Succeeded && authResult.Principal.Identity.IsAuthenticated)
                {
                    await next();
                }
                else if (authResult.Failure != null)
                {
                    authResult = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    if (authResult.Succeeded && authResult.Principal.Identity.IsAuthenticated)
                    {
                        await next();
                    }
                    else if (authResult.Failure != null)
                    {
                        // Rethrow, let the exception page handle it.
                        ExceptionDispatchInfo.Capture(authResult.Failure).Throw();
                    }
                    else
                    {
                        await context.ChallengeAsync();
                    }
                }
                else
                {
                    await context.ChallengeAsync();
                }
            });*/

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapAreaControllerRoute(
                    "APIArea",
                    "API",
                    "API/{controller=Home}/{action=Index}/{id?}");

                endpoints.MapAreaControllerRoute(
                    "SynapseArea",
                    "Synapse",
                    "Synapse/{controller=Home}/{action=Index}/{id?}");

                endpoints.MapControllerRoute(
                    "default",
                    "{controller}/{action}/{id?}",
                    new {controller = "Home", action = "Index"});

                endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains("ready")
                });

                endpoints.MapHealthChecks("/health/live", new HealthCheckOptions());
            });
        }
    }
}
