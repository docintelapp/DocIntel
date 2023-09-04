using System;
using System.ComponentModel;
using System.Threading.Tasks;
using DocIntel.Core.Authentication;
using DocIntel.Core.Authorization;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.Indexation;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Spectre.Console;
using Spectre.Console.Cli;

namespace DocIntel.AdminConsole.Commands.Index
{
    public class IndexTagsCommand : DocIntelCommand<IndexTagsCommand.Settings>
    {
        private readonly ITagIndexingUtility _tagIndexingUtility;
        private readonly ITagRepository _tagRepository;
        private readonly ILogger<IndexTagsCommand> _logger;

        public IndexTagsCommand(DocIntelContext context,
            AppUserClaimsPrincipalFactory userClaimsPrincipalFactory,
            ApplicationSettings applicationSettings,
            ITagIndexingUtility tagIndexingUtility,
            ITagRepository tagRepository, ILogger<IndexTagsCommand> logger, UserManager<AppUser> userManager,
            AppRoleManager roleManager) : base(context,
            userClaimsPrincipalFactory, applicationSettings, userManager, roleManager)
        {
            _tagIndexingUtility = tagIndexingUtility;
            _tagRepository = tagRepository;
            _logger = logger;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            await base.ExecuteAsync(context, settings);
            
            var ambientContext = await TryGetAmbientContext();
            if (ambientContext == null)
                return 1;

            if (settings.Clear)
            {
                AnsiConsole.Render(new Markup("[grey]Will remove all tags from the index...[/]"));
                _tagIndexingUtility.RemoveAll();
                AnsiConsole.Render(new Markup("[green]Done.[/]\n"));
            }

            // TODO Use tag repository
            var tags = _context.Tags.Include(_ => _.Facet)
                .Include(_ => _.LastModifiedBy)
                .Include(_ => _.CreatedBy)
                .Include(_ => _.Documents).ThenInclude(_ => _.Document);

            AnsiConsole.Render(new Markup("[grey]Will index all tags...[/]"));
            foreach (var tag in tags)
                try
                {
                    _tagIndexingUtility.Update(tag);
                    tag.LastIndexDate = DateTime.UtcNow;
                }
                catch (Exception e)
                {
                    // TODO Use structured logging
                    _logger.LogError($"Could not index tag '{tag.TagId}' ({e.Message}).");
                }

            await _context.SaveChangesAsyncWithoutNotification();

            if (settings.ForceCommit)
            {
                AnsiConsole.Render(new Markup("[green]Committing...[/]\n"));
                _tagIndexingUtility.Commit();
            }

            AnsiConsole.Render(new Markup("[green]Done.[/]\n"));

            return 0;
        }

        public class Settings : DocIntelCommandSettings
        {
            [CommandOption("-c|--clear")]
            [DefaultValue(false)]
            public bool Clear { get; set; }
        
            [CommandOption("-f|--force-commit")]
            [DefaultValue(false)]
            public bool ForceCommit { get; set; }
        }
    }
}