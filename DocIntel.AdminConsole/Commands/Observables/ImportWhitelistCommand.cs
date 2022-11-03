using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DocIntel.Core.Models;
using DocIntel.Core.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using Spectre.Console.Cli;
using Synsharp;
using Synsharp.Forms;

namespace DocIntel.AdminConsole.Commands.Observables
{
    public class ImportWhitelistCommand : DocIntelCommand<ImportWhitelistCommand.Settings>
    {
        private readonly ILogger<ExtractObservableCommand> _logger;
        protected readonly SynapseClient _synapseClient;

        public ImportWhitelistCommand(DocIntelContext context,
            IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory, 
            ApplicationSettings applicationSettings, 
            ILogger<ExtractObservableCommand> logger, SynapseClient synapseClient) : base(context,
            userClaimsPrincipalFactory, applicationSettings)
        {
            _logger = logger;
            _synapseClient = synapseClient;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            if (!TryGetAmbientContext(out var ambientContext))
                return 1;

            await _synapseClient.LoginAsync();

            if (settings.Source == Settings.WhitelistSource.MISP)
            {
                AnsiConsole.WriteLine("Import '" + settings.List + "' list");
                var url = "https://raw.githubusercontent.com/MISP/misp-warninglists/main/lists/" + settings.List + "/list.json";
                
                var tag = (!string.IsNullOrEmpty(settings.Tag) ? settings.Tag : $"{settings.Source}.{settings.List}").ToLower();
                tag = Regex.Replace(tag, @"[^a-z0-9\.]+", "_");
                
                try
                {
                    using var client = new WebClient();
                    if(!string.IsNullOrEmpty(_applicationSettings.Proxy))
                        client.Proxy = new WebProxy(_applicationSettings.Proxy);
                    
                    using var stream = new MemoryStream(client.DownloadData(url));
                    System.Console.WriteLine("Downloaded");
                    using (StreamReader sr = new StreamReader(stream))
                    using (JsonReader reader = new JsonTextReader(sr))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        var p = (JObject) serializer.Deserialize(reader);

                        var valueType = p["type"].Value<string>().ToLower();

                        var objects = new List<SynapseObject>();
                        foreach (var item in p["list"].ToArray())
                        {
                            SynapseObject o = null;
                            if (valueType == "cidr")
                            {
                                IPNetwork network;
                                if (IPNetwork.TryParse(item.Value<string>(), out network))
                                {
                                    if (network.Total > 1)
                                    {
                                        if (network.AddressFamily == AddressFamily.InterNetwork)
                                            o = new InetCidr4(network);
                                        
                                        if (network.AddressFamily == AddressFamily.InterNetworkV6)
                                            o = new InetCidr6(network);
                                    }
                                    else
                                    {
                                        if (network.AddressFamily == AddressFamily.InterNetwork)
                                            o = new InetIPv4(network.Network);
                                        
                                        if (network.AddressFamily == AddressFamily.InterNetworkV6)
                                            o = new InetIPv6(network.Network);
                                    }
                                }
                            }
                            else if (valueType == "hostname")
                            {
                                o = new InetFqdn(item.Value<string>());
                            }
                            else if (valueType == "string" & p["matching_attributes"]?.ToArray().Select(_ => _.Value<string>()).Contains("hostname") ?? false)
                            {
                                o = new InetFqdn(item.Value<string>());
                            }

                            if (o != null)
                            {
                                o.Tags.Add(tag);
                                if (settings.Ignore)
                                    o.Tags.Add("_di.workflow.ignore");
                                await _synapseClient.Nodes.Add(o);
                            }
                        }
                    }
                }
                catch (WebException)
                {
                    _logger.LogError("Could not import list " + settings.List + " from MISP Warning lists.");
                    return 1;
                }

                var user = GetAutomationUser();
                if (user == null)
                    return 1;
                
            }
            
            return 0;
        }
        
        private AppUser GetAutomationUser()
        {
            var automationUser =
                _context.Users.AsNoTracking().FirstOrDefault(_ => _.UserName == _applicationSettings.AutomationAccount);
            if (automationUser == null)
                AnsiConsole.Render(
                    new Markup($"[red]The user '{_applicationSettings.AutomationAccount}' was not found.[/]"));

            return automationUser;
        }

        public class Settings : DocIntelCommandSettings
        {
            public enum WhitelistSource { MISP }
            
            [CommandArgument(0, "<Source>")]
            [Description("Source (one of MISP)")]
            public WhitelistSource Source { get; set; }
            
            [CommandArgument(1, "<List>")]
            [Description("List, e.g. cisco_top1000")]
            public string List { get; set; }
            
            [CommandOption("-t|--tag <Tag>")]
            [Description("The tag to apply (default: <source>.<list>, e.g. misp.cisco_top1000)")]
            public string Tag { get; set; }
            
            [CommandOption("-i|--ignore")]
            [Description("Whether the values should be ignored while importing")]
            public bool Ignore { get; set; }

        }
    }
}