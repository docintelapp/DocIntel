using System;
using System.Linq;
using System.Threading.Tasks;
using DocIntel.AdminConsole.Commands.Classifications;
using DocIntel.AdminConsole.Commands.Documents;
using DocIntel.AdminConsole.Commands.Index;
using DocIntel.AdminConsole.Commands.Observables;
using DocIntel.AdminConsole.Commands.Roles;
using DocIntel.AdminConsole.Commands.Tags;
using DocIntel.AdminConsole.Commands.Thumbnails;
using DocIntel.AdminConsole.Commands.Users;
using DocIntel.Core.Helpers;
using DocIntel.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog.Web;
using RunMethodsSequentially.LockAndRunCode;

using Spectre.Console.Cli;

namespace DocIntel.AdminConsole
{
    internal class Program: DocIntelServiceProgram
    {
        public static async Task Main(string[] args)
        {
            using var host = CreateHostBuilder(args).Build();

            // Ensure that pre-flight scripts are run
            var lockAndRun = host.Services.GetRequiredService<IGetLockAndThenRunServices>();
            await lockAndRun.LockAndLoadAsync();
            
            // If we start the host as it should be, the application crashes with a System.NullReferenceException
            // Spectre.Console.Cli.CommandRuntimeException: Could not resolve type 'DocIntel.AdminConsole.Commands.XXX'.
            // ---> MassTransit.ConfigurationException: An exception occurred during bus creation
            // ---> System.NullReferenceException: Object reference not set to an instance of an object.
            // I have no idea how to fix the issue properly, and I wonder if this is not due to the weird dependency
            // injection system bound to Spectre.Console. I'm commenting the code for now. 
            
            // await host.StartAsync();
            // var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

            var app = host.Services.GetRequiredService<CommandApp>();
            await app.RunAsync(Environment.GetCommandLineArgs().Skip(1));

            // lifetime.StopApplication();
            // await host.WaitForShutdownAsync();
        }
        
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .ConfigureAppConfiguration(ConfigureAppConfiguration)
                .ConfigureLogging(ConfigureLogging)
                .ConfigureServices((hostContext, services) =>
                {
                    var typeRegistrar = new DependencyInjectionRegistrar(services);
                    services.AddSingleton(typeRegistrar);
            
                    var app = new CommandApp(typeRegistrar);
                    app.Configure(config =>
                    {
                        config.PropagateExceptions();
                        ConfigureApp(config);
                    });

                    services.AddSingleton(app);
                    
                    ConfigureService(hostContext, services);
                })
                .UseNLog();

        private static  void ConfigureApp(IConfigurator config)
        {
            config.AddBranch("user", add =>
            {
                add.AddCommand<InitUserCommand>("init");
                add.AddCommand<AddUserCommand>("add");
                add.AddCommand<ResetUserCommand>("reset");
                add.AddCommand<RoleUserCommand>("role");
                add.AddCommand<VerifyEmailUserCommand>("verify");
            });
            config.AddBranch("role", add =>
            {
                add.AddCommand<ListRoleCommand>("list");
                add.AddCommand<AddRoleCommand>("add");
                add.AddCommand<PermissionRoleCommand>("permissions");
            });
            config.AddBranch("classification", add =>
            {
                add.AddCommand<AddClassificationCommand>("add");
            });
            config.AddBranch("index", add =>
            {
                add.AddCommand<IndexDocsCommand>("docs");
                add.AddCommand<IndexTagsCommand>("tags");
                add.AddCommand<IndexSourcesCommand>("sources");
                add.AddCommand<IndexFacetsCommand>("facets");
            });
            config.AddBranch("document", add =>
            {
                add.AddCommand<ImportDocumentCommand>("import");
            });
            config.AddBranch("observable", add =>
            {
                add.AddCommand<ExtractObservableCommand>("extract");
            });
            config.AddBranch("whitelist", add =>
            {
                add.AddCommand<ImportWhitelistCommand>("import");
            });
            config.AddBranch("tags", add =>
            {
                // add.AddCommand<ImportTagsCommand>("import");
                add.AddCommand<AnalyzeTagsCommand>("analyze");
            });
            config.AddBranch("thumbnails", add =>
            {
                add.AddCommand<GenerateThumbnailCommand>("generate");
            });
        }
    }
}