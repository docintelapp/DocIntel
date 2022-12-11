using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DocIntel.AdminConsole.Commands.Observables;
using DocIntel.Core.Helpers;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils;
using Markdig;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DocIntel.AdminConsole.Commands.Tags
{
    public class ImportTagsCommand : DocIntelCommand<ImportTagsCommand.Settings>
    {
        private readonly ILogger<ExtractObservableCommand> _logger;
        private readonly ITagRepository _tagRepository;
        private readonly ITagFacetRepository _facetRepository;

        public ImportTagsCommand(DocIntelContext context,
            AppUserClaimsPrincipalFactory userClaimsPrincipalFactory, ApplicationSettings applicationSettings, ILogger<ExtractObservableCommand> logger, ITagRepository tagRepository, ITagFacetRepository facetRepository) : base(context,
            userClaimsPrincipalFactory, applicationSettings, userManager, roleManager)
        {
            _logger = logger;
            _tagRepository = tagRepository;
            _facetRepository = facetRepository;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var ambientContext = await TryGetAmbientContext();
            if (ambientContext == null)
                return 1;
            var tu = new TagUtility(_tagRepository, _facetRepository);

            if (settings.Source == Settings.TagSource.Attack)
            {
                await ImportAttack(tu, ambientContext);
            } else if (settings.Source == Settings.TagSource.MispTaxonomy)
            {
                await ImportMISPTaxonomy(settings, tu, ambientContext);
            } else if (settings.Source == Settings.TagSource.MispGalaxy)
            {
                await ImportMISPGalaxy(settings, tu, ambientContext);
            }
            else
            {
                AnsiConsole.WriteLine($"Source '{settings.Source}' unknown.");
                return 1;
            }

            await ambientContext.DatabaseContext.SaveChangesAsync();
            AnsiConsole.WriteLine("Tags imported");
            return 0;
        }

        private async Task ImportMISPTaxonomy(Settings settings, TagUtility tu, AmbientContext ambientContext)
        {
            var url = $"https://raw.githubusercontent.com/MISP/misp-taxonomies/main/{settings.Taxonomy}/machinetag.json";
            try
            {
                var tags = new HashSet<string>();

                using var client = new WebClient();
                if (!string.IsNullOrEmpty(_applicationSettings.Proxy))
                    client.Proxy = new WebProxy(_applicationSettings.Proxy);

                using var stream = new MemoryStream(client.DownloadData(url));
                AnsiConsole.WriteLine("Downloaded");
                using (StreamReader sr = new StreamReader(stream))
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    var p = (JObject)serializer.Deserialize(reader);
                    var ns = p["namespace"].Value<string>().ToLower();
                    
                    /*
                    var facet = await tu.GetOrCreateFacet(ambientContext, ns);
                    facet.Description = p["description"].Value<string>();
                    if (facet.MetaData == null)
                        facet.MetaData = new JObject();
                    facet.MetaData.Add("version", p["version"].Value<int>());
                    */
                    
                    foreach (var item in p["predicates"].ToArray())
                    {
                        tags.Add(ns + ":" + item["value"].Value<string>());
                    }

                    tu.GetOrCreateTags(ambientContext, tags);
                }
            }
            catch (WebException)
            {
                _logger.LogError("Could not import taxonomy '" + settings.Taxonomy + "' from MISP Taxonomies.");
                //return 1;
            }
        }

        private class TagDescription
        {
            public string Label { get; set; }
            public string Description { get; set; }
            public string Keywords { get; set; }
            public string ExtractionKeywords { get; set; }

            protected bool Equals(TagDescription other)
            {
                return Label == other.Label;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((TagDescription)obj);
            }

            public override int GetHashCode()
            {
                return (Label != null ? Label.GetHashCode() : 0);
            }
        }

        private async Task ImportMISPGalaxy(Settings settings, TagUtility tu, AmbientContext ambientContext)
        {
            var url = $"https://raw.githubusercontent.com/MISP/misp-galaxy/main/clusters/{settings.Galaxy}.json";
            try
            {
                var tags = new HashSet<TagDescription>();
                var cache = new HashSet<Tag>();

                using var client = new WebClient();
                if (!string.IsNullOrEmpty(_applicationSettings.Proxy))
                    client.Proxy = new WebProxy(_applicationSettings.Proxy);

                using var stream = new MemoryStream(client.DownloadData(url));
                AnsiConsole.WriteLine("Downloaded");
                using (StreamReader sr = new StreamReader(stream))
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    var p = (JObject)serializer.Deserialize(reader);
                    // var facet = await tu.GetOrCreateFacet(ambientContext, settings.Facet);
                    // facet.Title = p["name"].Value<string>();
                    // facet.Description = p["description"].Value<string>();
                    /*
                    if (facet.MetaData == null)
                        facet.MetaData = new JObject();
                    if (facet.MetaData.ContainsKey("misp-uuid"))
                        facet.MetaData["misp-uuid"] = p["uuid"].Value<string>();
                    else
                        facet.MetaData.Add("misp-uuid", p["uuid"].Value<string>());
                    */
                    
                    /*
                    foreach (var item in p["values"].ToArray())
                    {
                        var tagDescription = new TagDescription
                        {  
                            Label = item["value"]?.Value<string>() ?? "",
                            Description =  item["description"]?.Value<string>() ?? "",
                            Keywords = string.Join(",", item["synonyms"]?.Select(_ => _.Value<string>()) ?? Enumerable.Empty<string>())
                        };
                        tags.Add(tagDescription);
                        _logger.LogDebug($"TagDescription '{tagDescription.Label}'");
                    }
                    */

                    tu.GetOrCreateTags(ambientContext, tags.Select(_ => settings.Facet + ":" + _.Label));
                    
                    /*
                    foreach (var t in tags)
                    {
                        var tt = await tu.GetOrCreateTag(ambientContext, facet, t.Label, cache);
                        // tt.Description = t.Description;
                        // tt.Keywords = t.Keyword;
                        
                        _logger.LogDebug($"TagDescription '{tt.TagId}'");
                    }
                    */
                }
            }
            catch (WebException)
            {
                _logger.LogError("Could not import taxonomy '" + settings.Taxonomy + "' from MISP Taxonomies.");
                //return 1;
            }
        }

        private async Task ImportAttack(TagUtility tu, AmbientContext ambientContext)
        {
            var tags = new Dictionary<string, HashSet<TagDescription>>();

            var facets = new Dictionary<string, TagFacet>();

            AnsiConsole.WriteLine("Import MITRE ATT&CK");
            var url = "https://raw.githubusercontent.com/mitre/cti/master/enterprise-attack/enterprise-attack.json";

            using var client = new WebClient();
            if (!string.IsNullOrEmpty(_applicationSettings.Proxy))
                client.Proxy = new WebProxy(_applicationSettings.Proxy);

            using var stream = new MemoryStream(client.DownloadData(url));
            System.Console.WriteLine("JSON Downloaded");
            using (StreamReader sr = new StreamReader(stream))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                JsonSerializer serializer = new JsonSerializer();
                var p = (JObject)serializer.Deserialize(reader);

                foreach (var o in p["objects"])
                {
                    if (o["type"]?.ToString() == "x-mitre-tactic")
                    {
                        var tacticId = o["x_mitre_shortname"]?.Value<string>()
                            ?.FromDashToLowerCamelCase() ?? "";
                        var name = o["name"]?.Value<string>() ?? tacticId;
                        var description = Markdown.ToHtml(o["description"]?.Value<string>() ?? "");
                        if (!string.IsNullOrEmpty(tacticId) && !facets.ContainsKey(tacticId))
                        {
                            var facet = await tu.GetOrCreateFacet(ambientContext, $"technique.{tacticId}",
                                $"Technique ({name})");
                            facet.Title = $"Technique ({name})";
                            facet.Description = description;
                            facet.AutoExtract = true;
                            facets.Add(tacticId, facet);
                        }
                    }
                    else if (o["type"]?.ToString() == "attack-pattern")
                    {
                        if (o["revoked"]?.Value<bool>() ?? false)
                            continue;

                        var phases = o["kill_chain_phases"]?
                            .Where(oo => oo["kill_chain_name"]?.Value<string>() == "mitre-attack")
                            .Select(oo => oo["phase_name"]?.Value<string>().FromDashToLowerCamelCase());

                        if (o["external_references"]
                                ?.Any(oo => oo["source_name"].ToString() == "mitre-attack") ?? false)
                        {
                            var mitreID =
                                o["external_references"]?.First(oo =>
                                    oo["source_name"].ToString() == "mitre-attack")["external_id"];

                            var tagName = mitreID?.ToString() + " - " + o["name"];
                            var description = Markdown.ToHtml(o["description"]?.Value<string>() ?? "");
                            foreach (var phase in phases)
                            {
                                if (!tags.ContainsKey(phase))
                                    tags.Add(phase, new HashSet<TagDescription>());

                                tags[phase].Add(new TagDescription
                                {
                                    Label = tagName,
                                    Description = description,
                                    Keywords = mitreID?.ToString() ?? "",
                                    ExtractionKeywords = mitreID?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
            }

            var cache = new HashSet<Tag>();
            foreach (var t in tags)
            {
                foreach (var tagDescription in t.Value)
                {
                    // TODO There is very likely a way to improve this two-step approach :vomit:
                    var efTag = await tu.GetOrCreateTag(ambientContext, facets[t.Key], tagDescription.Label, cache);
                    efTag.Description = tagDescription.Description;
                    efTag.Keywords = tagDescription.Keywords;
                    efTag.ExtractionKeywords = tagDescription.ExtractionKeywords;
                }
            }
        }

        public class Settings : DocIntelCommandSettings
        {
            public enum TagSource { Attack, MispTaxonomy, MispGalaxy }
            
            [CommandArgument(0, "<Source>")]
            [Description("Source")]
            public TagSource Source { get; set; }
            
            [CommandOption("-t|--taxonomy <Taxonomy>")]
            [Description("The misp taxonomy to import (only with source MispTaxonomy)")]
            public string Taxonomy { get; set; }
            
            [CommandOption("-g|--galaxy <Galaxy>")]
            [Description("The misp galaxy to import (only with source MispGalaxy)")]
            public string Galaxy { get; set; }
            
            [CommandOption("-f|--facet <Facet>")]
            [Description("The facet prefix (only alphanumeric characteurs, dashes or dots) to use for import (only with source MispGalaxy)")]
            public string Facet { get; set; }
        }
    }
}