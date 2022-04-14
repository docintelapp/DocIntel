using System;
using System.Threading.Tasks;

using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.Indexation;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using Spectre.Console;
using Spectre.Console.Cli;

namespace DocIntel.AdminConsole.Commands.Index
{
    public class IndexDocsCommand : DocIntelCommand<DocIntelCommandSettings>
    {
        private readonly IDocumentIndexingUtility _documentIndexingService;
        private readonly IDocumentRepository _documentRepository;
        private readonly ILogger<IndexDocsCommand> _logger;

        public IndexDocsCommand(DocIntelContext context,
            IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory,
            ApplicationSettings applicationSettings,
            IDocumentIndexingUtility documentIndexingService,
            IDocumentRepository documentRepository, ILogger<IndexDocsCommand> logger) : base(context,
            userClaimsPrincipalFactory, applicationSettings)
        {
            _documentIndexingService = documentIndexingService;
            _documentRepository = documentRepository;
            _logger = logger;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, DocIntelCommandSettings settings)
        {
            await base.ExecuteAsync(context, settings);
            
            if (!TryGetAmbientContext(out var ambientContext))
                return 1;

            AnsiConsole.Render(new Markup("[grey]Will remove all documents from the index...[/]"));
            _documentIndexingService.RemoveAll();
            AnsiConsole.Render(new Markup("[green]Done.[/]\n"));

            var documents = _documentRepository.GetAllAsync(ambientContext, new DocumentQuery
                {
                    Limit = -1
                },
                new[]
                {
                    nameof(Document.DocumentTags),
                    nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag),
                    nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag) + "." + nameof(Tag.Facet),
                    nameof(Document.Source),
                    nameof(Document.Comments),
                    nameof(Document.Files)
                });

            AnsiConsole.Render(new Markup("[grey]Will index all documents...[/]"));
            await foreach (var document in documents)
                try
                {
                    _documentIndexingService.Update(document);
                }
                catch (Exception e)
                {
                    // TODO Use structured logging
                    _logger.LogError($"Could not index document '{document.DocumentId}' ({e.Message}).");
                }

            AnsiConsole.Render(new Markup("[green]Done.[/]\n"));

            return 0;
        }
    }
}