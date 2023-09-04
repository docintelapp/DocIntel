using System;
using System.Threading.Tasks;
using DocIntel.Core.Authentication;
using DocIntel.Core.Authorization;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.Indexation;
using DocIntel.Core.Utils.Search.Tags;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Spectre.Console;
using Spectre.Console.Cli;

namespace DocIntel.AdminConsole.Commands.Index
{
    public class IndexFacetsCommand : DocIntelCommand<DocIntelCommandSettings>
    {
        private readonly ITagFacetIndexingUtility _facetIndexingUtility;
        private readonly ITagFacetRepository _facetRepository;
        private readonly ILogger<IndexFacetsCommand> _logger;

        public IndexFacetsCommand(DocIntelContext context,
            AppUserClaimsPrincipalFactory userClaimsPrincipalFactory,
            ApplicationSettings applicationSettings,
            ITagFacetIndexingUtility facetIndexingUtility,
            ITagFacetRepository facetRepository, ILogger<IndexFacetsCommand> logger, UserManager<AppUser> userManager,
            AppRoleManager roleManager) : base(context,
            userClaimsPrincipalFactory, applicationSettings, userManager, roleManager)
        {
            _facetIndexingUtility = facetIndexingUtility;
            _facetRepository = facetRepository;
            _logger = logger;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, DocIntelCommandSettings settings)
        {
            await base.ExecuteAsync(context, settings);
            
            var ambientContext = await TryGetAmbientContext();
            if (ambientContext == null)
                return 1;

            AnsiConsole.Render(new Markup("[grey]Will remove all facets from the index...[/]"));
            _facetIndexingUtility.RemoveAll();
            AnsiConsole.Render(new Markup("[green]Done.[/]\n"));

            // TODO Use tag repository
            var facets = _context.Facets.AsQueryable()
                .Include(_ => _.LastModifiedBy)
                .Include(_ => _.CreatedBy);

            AnsiConsole.Render(new Markup("[grey]Will index all facets...[/]"));
            foreach (var facet in facets)
                try
                {
                    facet.LastIndexDate = DateTime.UtcNow;
                    _facetIndexingUtility.Update(facet);
                }
                catch (Exception e)
                {
                    // TODO Use structured logging
                    _logger.LogError($"Could not index facet '{facet.Title}' ({facet.FacetId}) ({e.Message}).");
                }

            await _context.SaveChangesAsyncWithoutNotification();
            
            AnsiConsole.Render(new Markup("[green]Done.[/]\n"));

            return 0;
        }
    }
}