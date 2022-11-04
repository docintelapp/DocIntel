using System;
using System.ComponentModel;
using System.Threading.Tasks;

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
            IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory,
            ApplicationSettings applicationSettings,
            ITagIndexingUtility tagIndexingUtility,
            ITagRepository tagRepository, ILogger<IndexTagsCommand> logger) : base(context,
            userClaimsPrincipalFactory, applicationSettings)
        {
            _tagIndexingUtility = tagIndexingUtility;
            _tagRepository = tagRepository;
            _logger = logger;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            await base.ExecuteAsync(context, settings);
            
            if (!TryGetAmbientContext(out var ambientContext))
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
                }
                catch (Exception e)
                {
                    // TODO Use structured logging
                    _logger.LogError($"Could not index tag '{tag.TagId}' ({e.Message}).");
                }

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