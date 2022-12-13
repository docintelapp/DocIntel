using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DocIntel.Core.Authentication;
using DocIntel.Core.Authorization;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.ContentExtraction;
using DocIntel.Core.Utils.Features;
using DotLiquid;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DocIntel.AdminConsole.Commands.Tags
{
    public class  AnalyzeTagsCommand : DocIntelCommand<AnalyzeTagsCommand.Settings>
    {
        private readonly ILogger<AnalyzeTagsCommand> _logger;
        private readonly ITagRepository _tagRepository;
        private readonly ITagFacetRepository _facetRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly IContentExtractionUtility _contentExtractionUtility;
        private readonly ILogger<TagFacetFeatureExtractor> _loggerExtractor;

        public AnalyzeTagsCommand(DocIntelContext context,
            AppUserClaimsPrincipalFactory userClaimsPrincipalFactory,
            ApplicationSettings applicationSettings,
            ILogger<AnalyzeTagsCommand> logger,
            ITagRepository tagRepository,
            ITagFacetRepository facetRepository,
            IDocumentRepository documentRepository,
            IContentExtractionUtility contentExtractionUtility, UserManager<AppUser> userManager,
            AppRoleManager roleManager, ILogger<TagFacetFeatureExtractor> loggerExtractor) : base(context,
            userClaimsPrincipalFactory, applicationSettings, userManager, roleManager)
        {
            _logger = logger;
            _tagRepository = tagRepository;
            _facetRepository = facetRepository;
            _documentRepository = documentRepository;
            _contentExtractionUtility = contentExtractionUtility;
            _loggerExtractor = loggerExtractor;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var ambientContext = await TryGetAmbientContext();
            if (ambientContext == null)
                return 1;

            var doc = await _documentRepository.GetAsync(ambientContext, (Guid)settings.DocumentId, new [] {"DocumentTags", "Files"});

            foreach (var f in doc.Files)
            {
                var text = _contentExtractionUtility.ExtractText(doc, f);
                _logger.LogDebug($"Checking '{f.Filename}' from '{doc.Title}' (length: {text.Length})");
                AnsiConsole.Write(new Markup($"[b yellow]Analyzing '{f.Filename}' from '{doc.Title}'[/]\n"));

                var facets = await _facetRepository.GetAllAsync(ambientContext, new FacetQuery(), new string[] { "Tags" }).ToListAsync();

                foreach (var facet in facets)
                {
                    var extractor = new TagFacetFeatureExtractor(facet, _loggerExtractor); 
                    var patternMatches = extractor.Extract(text);

                    if (!string.IsNullOrEmpty(facet.TagNormalization))
                    {
                        var labelTemplate = Template.Parse("{{label | " + facet.TagNormalization + "}}");
                        patternMatches = patternMatches.Select(_ => labelTemplate.Render(Hash.FromAnonymousObject(new { label = _ }))).Distinct();
                    }
                    
                    if (patternMatches.Count() > 0)
                        AnsiConsole.Write(new Markup($"[b]{facet.Title}:[/] " + string.Join(",", patternMatches) + "\n"));    
                }
            }
            
            return 0;
        }

        public class Settings : DocIntelCommandSettings
        {
            [CommandArgument(0, "<DocumentId>")]
            [Description("DocumentId")]
            public Guid DocumentId { get; set; }
        }
    }
}