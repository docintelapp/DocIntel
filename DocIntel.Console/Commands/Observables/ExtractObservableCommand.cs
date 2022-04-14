using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.ContentExtraction;
using DocIntel.Core.Utils.Observables;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using Spectre.Console.Cli;

namespace DocIntel.Console.Commands.Observables
{
    public class ExtractObservableCommand : DocIntelCommand<ExtractObservableCommand.Settings>
    {
        private readonly IContentExtractionUtility _contentExtractionUtility;
        private readonly IObservablesExtractionUtility _observablesExtractionUtility;
        private readonly IDocumentRepository _documentRepository;
        private readonly ILogger<ExtractObservableCommand> _logger;

        public ExtractObservableCommand(DocIntelContext context,
            IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory, ApplicationSettings applicationSettings, IObservablesExtractionUtility observablesExtractionUtility, IContentExtractionUtility contentExtractionUtility, IDocumentRepository documentRepository, ILogger<ExtractObservableCommand> logger) : base(context,
            userClaimsPrincipalFactory, applicationSettings)
        {
            _observablesExtractionUtility = observablesExtractionUtility;
            _contentExtractionUtility = contentExtractionUtility;
            _documentRepository = documentRepository;
            _logger = logger;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            if (!TryGetAmbientContext(out var ambientContext))
                return 1;

            var document = await _documentRepository.GetAsync(ambientContext, settings.DocumentId, new [] { "Files" });

            var observables = new HashSet<Observable>();
            foreach (var file in document.Files)
            {
                var text = _contentExtractionUtility.ExtractText(document, file);
                observables.UnionWith(await _observablesExtractionUtility.ExtractObservable(text, file));
            }
            
            foreach (var observable in observables.Where(_ => _.Status != ObservableStatus.Whitelisted & _.Status != ObservableStatus.Rejected))
            {
                if (observable.Type == ObservableType.File)
                {
                    foreach (var hash in observable.Hashes)
                    {
                        System.Console.WriteLine($"[{observable.Status}] {hash.HashType}:{hash.Value}");
                    }
                }
                else
                {
                    System.Console.WriteLine($"[{observable.Status}] {observable.Type}: {observable.Value}");   
                }
            }

            return 0;
        }

        public class Settings : DocIntelCommandSettings
        {
            [CommandArgument(0, "<DocumentId>")]
            [Description("Identifier of the document")]
            public Guid DocumentId { get; set; }
        }
    }
}