using System;
using System.Linq;
using System.Threading.Tasks;
using DocIntel.Console.Commands.Documents;
using DocIntel.Console.Commands.Observables;
using DocIntel.Console.Commands.Tags;
using DocIntel.Console.Commands.Thumbnails;
using DocIntel.Core.Helpers;
using DocIntel.Core.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NLog.Web;
using RunMethodsSequentially.LockAndRunCode;
using Spectre.Console;
using Spectre.Console.Cli;
using Synsharp;

namespace DocIntel.Console
{
    internal class Program : DocIntelServiceProgram
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

            var settings = host.Services.GetRequiredService<SynapseSettings>();
            System.Console.WriteLine(settings.URL);
            
            var client = host.Services.GetRequiredService<SynapseClient>();
            await client.LoginAsync();
            
            var app = host.Services.GetRequiredService<CommandApp>();
            AnsiConsole.Write(new Markup($"[bold yellow]DocIntel Administrative Console[/]\n" +
                                         "*** For more information on DocIntel see <https://docintel.org/> ***\n" +
                                         "*** Please report bugs to <https://github.com/docintelapp> ***\n"));
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
        
        private static void ConfigureApp(IConfigurator config)
        {
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
                add.AddCommand<ImportTagsCommand>("import");
                add.AddCommand<AnalyzeTagsCommand>("analyze");
            });
            config.AddBranch("thumbnails", add =>
            {
                add.AddCommand<GenerateThumbnailCommand>("generate");
            });
        }
    }
}