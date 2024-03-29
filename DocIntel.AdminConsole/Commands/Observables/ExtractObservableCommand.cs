using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DocIntel.Core.Authentication;
using DocIntel.Core.Authorization;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.ContentExtraction;
using DocIntel.Core.Utils.Observables;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using Synsharp;
using Synsharp.Telepath.Helpers;
using Synsharp.Telepath.Messages;

namespace DocIntel.AdminConsole.Commands.Observables
{
    public class ExtractObservableCommand : DocIntelCommand<ExtractObservableCommand.Settings>
    {
        private readonly IContentExtractionUtility _contentExtractionUtility;
        private readonly IObservablesUtility _observablesUtility;
        private readonly IDocumentRepository _documentRepository;
        private readonly ILogger<ExtractObservableCommand> _logger;
        private readonly ISynapseRepository _observablesRepository;

        public ExtractObservableCommand(DocIntelContext context,
            AppUserClaimsPrincipalFactory userClaimsPrincipalFactory,
            ApplicationSettings applicationSettings,
            IObservablesUtility observablesUtility,
            IContentExtractionUtility contentExtractionUtility,
            IDocumentRepository documentRepository,
            ILogger<ExtractObservableCommand> logger,
            ISynapseRepository observablesRepository, UserManager<AppUser> userManager,
            AppRoleManager roleManager) : base(context,
            userClaimsPrincipalFactory, applicationSettings, userManager, roleManager)
        {
            _observablesUtility = observablesUtility;
            _contentExtractionUtility = contentExtractionUtility;
            _documentRepository = documentRepository;
            _logger = logger;
            _observablesRepository = observablesRepository;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var ambientContext = await TryGetAmbientContext();
            if (ambientContext == null)
                return 1;

            var document = await _documentRepository.GetAsync(ambientContext, settings.DocumentId, new [] { "Files" });

            SynapseView view = null;
            if (settings.Save)
            {
                view = await _observablesRepository.CreateView(document);
            }
            
            var observables = new HashSet<SynapseNode>();
            foreach (var file in document.Files)
            {
                // Extract the content
                var text = _contentExtractionUtility.ExtractText(document, file);
                
                // Extract the observables
                var fileObservables = await _observablesUtility.ExtractDataAsync(document, file, text).ToListAsync();
                
                // Annotate the observables
                await _observablesUtility.AnnotateAsync(document, file, fileObservables);

                if (settings.Save)
                {
                    await _observablesRepository.Add(fileObservables, document, view);
                }
                
                foreach (var o in fileObservables) 
                {
                    if (o.Tags.ContainsKey("_di.workflow.ignore")) continue;
                    System.Console.WriteLine($"{o.Form}={o.Valu}");
                }
            }
            
            return 0;
        }

        public class Settings : DocIntelCommandSettings
        {
            [CommandArgument(0, "<DocumentId>")]
            [Description("Identifier of the document")]
            public Guid DocumentId { get; set; }
            
            [CommandOption("--save")]
            public bool Save { get; set; }
        }
    }
}