using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Threading.Tasks;

using DocIntel.Core.Models;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.Observables;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Spectre.Console;
using Spectre.Console.Cli;

namespace DocIntel.Console.Commands.Observables
{
    public class ImportWhitelistCommand : DocIntelCommand<ImportWhitelistCommand.Settings>
    {
        private readonly ILogger<ExtractObservableCommand> _logger;
        private IObservableWhitelistUtility _whitelistUtility;

        public ImportWhitelistCommand(DocIntelContext context,
            IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory, ApplicationSettings applicationSettings, ILogger<ExtractObservableCommand> logger, IObservableWhitelistUtility whitelistUtility) : base(context,
            userClaimsPrincipalFactory, applicationSettings)
        {
            _logger = logger;
            _whitelistUtility = whitelistUtility;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            if (!TryGetAmbientContext(out var ambientContext))
                return 1;

            if (settings.Source == Settings.WhitelistSource.MISP)
            {
                var domains = new HashSet<string>();
                var ip = new HashSet<string>();
                AnsiConsole.WriteLine("Import '" + settings.List + "' domains");
                var url = "https://raw.githubusercontent.com/MISP/misp-warninglists/main/lists/" + settings.List + "/list.json";

                try
                {
                    using (var client = new WebClient() { Proxy = new WebProxy(_applicationSettings.Proxy)})
                    {
                        using var stream = new MemoryStream(client.DownloadData(url));
                        System.Console.WriteLine("Downloaded");
                        using (StreamReader sr = new StreamReader(stream))
                        using (JsonReader reader = new JsonTextReader(sr))
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            var p = (JObject) serializer.Deserialize(reader);
                            if (p["matching_attributes"].Children().Any(_ => _.ToString() == "domain"))
                            {
                                foreach (var o in p["list"])
                                {
                                    domains.Add(o.ToString());
                                }
                            }
                            
                            if (p["matching_attributes"].Children().Any(_ => _.ToString() == "ip-src" | _.ToString() == "ip-dst"))
                            {
                                foreach (var o in p["list"])
                                {
                                    if (IPAddress.TryParse(o.ToString(), out var parsedId))
                                    {
                                        if (parsedId.AddressFamily == AddressFamily.InterNetwork)
                                        {
                                            _logger.LogDebug(parsedId.ToString());
                                            ip.Add(parsedId.ToString());
                                        }   
                                    }
                                }
                            }
                        }
                    }
                }
                catch (WebException e)
                {
                    _logger.LogError("Could not import list " + settings.List + " from MISP Warning lists.");
                    return 1;
                }

                var user = GetAutomationUser();
                if (user == null)
                    return 1;
                
                foreach (var t in domains)
                {
                    await _whitelistUtility.AddWhitelistedObservable(new Observable()
                    {
                        Type = ObservableType.FQDN,
                        Value = t,
                        Id = Guid.NewGuid(),
                        RegisteredById = user.Id,
                        LastModifiedById = user.Id,
                        Status = ObservableStatus.Whitelisted
                    });
                }
                
                foreach (var t in ip)
                {
                    await _whitelistUtility.AddWhitelistedObservable(new Observable()
                    {
                        Type = ObservableType.IPv4,
                        Value = t,
                        Id = Guid.NewGuid(),
                        RegisteredById = user.Id,
                        LastModifiedById = user.Id,
                        Status = ObservableStatus.Whitelisted
                    });
                }
            }
            
            return 0;
        }
        
        private AppUser GetAutomationUser()
        {
            var automationUser =
                _context.Users.FirstOrDefault(_ => _.UserName == _applicationSettings.AutomationAccount);
            if (automationUser == null)
                AnsiConsole.Render(
                    new Markup($"[red]The user '{_applicationSettings.AutomationAccount}' was not found.[/]"));

            return automationUser;
        }

        public class Settings : DocIntelCommandSettings
        {
            public enum WhitelistSource { MISP }
            
            [CommandArgument(0, "<Source>")]
            [Description("Source")]
            public WhitelistSource Source { get; set; }
            
            [CommandArgument(1, "<List>")]
            [Description("List")]
            public string List { get; set; }
        }
    }
}