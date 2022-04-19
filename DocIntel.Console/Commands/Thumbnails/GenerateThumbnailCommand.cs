using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DocIntel.Console.Commands.Observables;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.Thumbnail;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DocIntel.Console.Commands.Thumbnails;

public class GenerateThumbnailCommand : DocIntelCommand<GenerateThumbnailCommand.Settings>
{
    private readonly ILogger<ExtractObservableCommand> _logger;
    private readonly IThumbnailUtility _utility;
    private readonly IDocumentRepository _documentRepository;

    public GenerateThumbnailCommand(DocIntelContext context,
        IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory,
        ApplicationSettings applicationSettings,
        ILogger<ExtractObservableCommand> logger, IThumbnailUtility utility, IDocumentRepository documentRepository) : base(context,
        userClaimsPrincipalFactory, applicationSettings)
    {
        _logger = logger;
        _utility = utility;
        _documentRepository = documentRepository;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        if (!TryGetAmbientContext(out var ambientContext))
            return 1;

        var documentId = settings.DocumentId;
            
        try
        {
            if (documentId == null)
            {
                var documents =
                     await _documentRepository.GetAllAsync(ambientContext, _ => _.Include(x => x.Files)).ToListAsync();
                foreach (var document in documents)
                {
                    await _utility.GenerateThumbnail(ambientContext, document);   
                }
                await ambientContext.DatabaseContext.SaveChangesAsync();
            }
            else
            {
                var document =
                    await _documentRepository.GetAsync(ambientContext, (Guid)documentId, new[] {"Files"});
                await _utility.GenerateThumbnail(ambientContext, document);
                await ambientContext.DatabaseContext.SaveChangesAsync();   
            }
        }
        catch (UnauthorizedOperationException)
        {
            AnsiConsole.WriteLine("Document not found");   
        }
        catch (NotFoundEntityException)
        {
            AnsiConsole.WriteLine("Document not found");
        }
        catch (Exception e)
        {
            AnsiConsole.WriteException(e);
        }
            
        return 0;
    }

    public class Settings : DocIntelCommandSettings
    {
        [CommandOption("-d|--document <DocumentId>")]
        [Description("Document Identifier")]
        public Guid? DocumentId { get; set; }
    }
}