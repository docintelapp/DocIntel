using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DocIntel.Core.Authentication;
using DocIntel.Core.Authorization;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using Spectre.Console.Cli;
using Synsharp.Telepath;
using Synsharp.Telepath.Helpers;
using Synsharp.Telepath.Messages;

namespace DocIntel.AdminConsole.Commands.Observables
{
    public class ImportWhitelistCommand : DocIntelCommand<ImportWhitelistCommand.Settings>
    {
        private readonly ILogger<ExtractObservableCommand> _logger;
        protected readonly TelepathClient _synapseClient;
        private readonly NodeHelper _nodeHelper;

        public ImportWhitelistCommand(DocIntelContext context,
            AppUserClaimsPrincipalFactory userClaimsPrincipalFactory,
            ApplicationSettings applicationSettings,
            LoggerFactory loggerFactory, TelepathClient synapseClient, UserManager<AppUser> userManager,
            AppRoleManager roleManager) : base(context,
            userClaimsPrincipalFactory, applicationSettings, userManager, roleManager)
        {
            _logger = loggerFactory.CreateLogger<ExtractObservableCommand>();
            _synapseClient = synapseClient;
            _nodeHelper = new NodeHelper(_synapseClient, loggerFactory.CreateLogger<NodeHelper>());
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            AmbientContext ambientContext = await TryGetAmbientContext();
            if (ambientContext == null)
                return 1;


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

                        var objects = new List<SynapseNode>();
                        foreach (var item in p["list"].ToArray())
                        {
                            SynapseNode o = null;
                            if (valueType == "cidr")
                            {
                                IPNetwork network;
                                if (IPNetwork.TryParse(item.Value<string>(), out network))
                                {
                                    if (network.Total > 1)
                                    {
                                        if (network.AddressFamily == AddressFamily.InterNetwork)
                                            o = new SynapseNode() { Form = "inet:cidr4", Valu = network.ToString() };
                                        
                                        if (network.AddressFamily == AddressFamily.InterNetworkV6)
                                            o = new SynapseNode() { Form = "inet:cidr6", Valu = network.ToString() };
                                    }
                                    else
                                    {
                                        if (network.AddressFamily == AddressFamily.InterNetwork)
                                            o = new SynapseNode() { Form = "inet:ipv4", Valu = network.Network.ToString() };
                                        
                                        if (network.AddressFamily == AddressFamily.InterNetworkV6)
                                            o = new SynapseNode() { Form = "inet:ipv6", Valu = network.Network.ToString() };
                                    }
                                }
                            }
                            else if (valueType == "hostname")
                            {
                                o = new SynapseNode() { Form = "inet:fqdn", Valu = item.Value<string>() };
                            }
                            else if (valueType == "string" & p["matching_attributes"]?.ToArray().Select(_ => _.Value<string>()).Contains("hostname") ?? false)
                            {
                                o = new SynapseNode() { Form = "inet:fqdn", Valu = item.Value<string>() };
                            }

                            if (o != null)
                            {
                                o.Tags.Add(tag, new long?[] {});
                                if (settings.Ignore)
                                    o.Tags.Add("_di.workflow.ignore", System.Array.Empty<long?>());
                                await _nodeHelper.AddAsync(o);
                            }
                        }
                    }
                }
                catch (WebException)
                {
                    _logger.LogError("Could not import list " + settings.List + " from MISP Warning lists.");
                    return 1;
                }

                var user = await GetAutomationUserAsync();
                if (user == null)
                    return 1;
                
            }
            
            return 0;
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
            
            [CommandOption("--ignore")]
            [Description("Whether the values should be ignored while importing")]
            public bool Ignore { get; set; }

        }
    }
}