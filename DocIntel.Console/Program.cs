using System;
using System.Linq;
using System.Threading.Tasks;

using DocIntel.Console.Commands.Observables;
using DocIntel.Console.Commands.Tags;
using DocIntel.Core.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NLog.Web;

using Spectre.Cli.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DocIntel.Console
{
    internal class Program : DocIntelServiceProgram
    {
        public static async Task Main(string[] args)
        {
            using var host = CreateHostBuilder(args).Build();
            
            // If we start the host as it should be, the application crashes with a System.NullReferenceException
            // Spectre.Console.Cli.CommandRuntimeException: Could not resolve type 'DocIntel.AdminConsole.Commands.XXX'.
            // ---> MassTransit.ConfigurationException: An exception occurred during bus creation
            // ---> System.NullReferenceException: Object reference not set to an instance of an object.
            // I have no idea how to fix the issue properly, and I wonder if this is not due to the weird dependency
            // injection system bound to Spectre.Console. I'm commenting the code for now. 
            
            // await host.StartAsync();
            // var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

            var app = host.Services.GetRequiredService<CommandApp>();
            AnsiConsole.Write(new Markup($"[bold yellow]DocIntel Administrative Console[/]\n" +
                                         "*** For more information on DocIntel see <https://gitlab01.low.cert.mil.be/cailliaua/DocIntel> ***\n" +
                                         "*** Please report bugs to <https://gitlab01.low.cert.mil.be/cailliaua/DocIntel/issues> ***\n"));
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
            });
        }
    }
}