using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using DocIntel.Console.Commands.Observables;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils;
using DocIntel.Core.Utils.ContentExtraction;
using DocIntel.Core.Utils.Observables;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Spectre.Console;
using Spectre.Console.Cli;

namespace DocIntel.Console.Commands.Tags
{
    public class ImportTagsCommand : DocIntelCommand<ImportTagsCommand.Settings>
    {
        private readonly ILogger<ExtractObservableCommand> _logger;
        private readonly ITagRepository _tagRepository;
        private readonly ITagFacetRepository _facetRepository;

        public ImportTagsCommand(DocIntelContext context,
            IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory, ApplicationSettings applicationSettings, ILogger<ExtractObservableCommand> logger, ITagRepository tagRepository, ITagFacetRepository facetRepository) : base(context,
            userClaimsPrincipalFactory, applicationSettings)
        {
            _logger = logger;
            _tagRepository = tagRepository;
            _facetRepository = facetRepository;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            if (!TryGetAmbientContext(out var ambientContext))
                return 1;

            if (settings.Source == Settings.TagSource.Attack)
            {
                var tags = new HashSet<string>();
                AnsiConsole.WriteLine("Import attack");
                var url = "https://raw.githubusercontent.com/mitre/cti/master/enterprise-attack/enterprise-attack.json";
                using (var client = new WebClient() { Proxy = new WebProxy(_applicationSettings.Proxy)})
                {
                    using var stream = new MemoryStream(client.DownloadData(url));
                    System.Console.WriteLine("Downloaded");
                    using (StreamReader sr = new StreamReader(stream))
                    using (JsonReader reader = new JsonTextReader(sr))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        var p = (JObject) serializer.Deserialize(reader);

                        foreach (var o in p["objects"])
                        {
                            if (o["type"]?.ToString() == "attack-pattern")
                            {
                                if (o["external_references"]?.Any(oo => oo["source_name"].ToString() == "mitre-attack") ?? false)
                                {
                                    var mitreID =
                                        o["external_references"]?.First(oo =>
                                            oo["source_name"].ToString() == "mitre-attack")["external_id"];
                                    tags.Add(mitreID?.ToString() + " - " + o["name"]);
                                }
                            }
                        }
                    }
                }

                var cache = new HashSet<Tag>();
                var tu = new TagUtility(_tagRepository, _facetRepository);
                var facet = await tu.GetOrCreateFacet(ambientContext, "technique", "Technique");
                foreach (var t in tags)
                {
                    await tu.GetOrCreateTag(ambientContext, facet.Id, t, cache);
                }

                await ambientContext.DatabaseContext.SaveChangesAsync();
            }
            
            return 0;
        }

        public class Settings : DocIntelCommandSettings
        {
            public enum TagSource { Attack }
            
            [CommandArgument(0, "<DocumentId>")]
            [Description("Source")]
            public TagSource Source { get; set; }
        }
    }
}