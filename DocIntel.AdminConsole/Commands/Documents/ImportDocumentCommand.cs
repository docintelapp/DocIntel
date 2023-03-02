using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DocIntel.Core.Authentication;
using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.Thumbnail;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DocIntel.AdminConsole.Commands.Documents;

public class ImportDocumentCommand : DocIntelCommand<ImportDocumentCommand.Settings>
{
    private readonly ILogger<ImportDocumentCommand> _logger;
    private readonly IThumbnailUtility _utility;
    private readonly IDocumentRepository _documentRepository;
    private readonly IClassificationRepository _classificationRepository;
    private readonly ISourceRepository _sourceRepository;

    public ImportDocumentCommand(DocIntelContext context,
        AppUserClaimsPrincipalFactory userClaimsPrincipalFactory,
        ApplicationSettings applicationSettings,
        ILogger<ImportDocumentCommand> logger, IThumbnailUtility utility, IDocumentRepository documentRepository,
        IClassificationRepository classificationRepository, ISourceRepository sourceRepository,
        UserManager<AppUser> userManager, AppRoleManager roleManager) : base(context,
        userClaimsPrincipalFactory, applicationSettings, userManager, roleManager)
    {
        _logger = logger;
        _utility = utility;
        _documentRepository = documentRepository;
        _classificationRepository = classificationRepository;
        _sourceRepository = sourceRepository;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var ambientContext = await TryGetAmbientContext();
        if (ambientContext == null)
            return 1;

        try
        {
            // Read the JSON file
            var json = File.ReadAllText(settings.Filepath);
            var importDoc = JsonConvert.DeserializeObject<DocumentImport>(json);

            var classification = _classificationRepository.GetDefault(ambientContext);
            var source = await _sourceRepository.GetAsync(ambientContext, Guid.Parse(settings.SourceId));

            if (await _documentRepository.ExistsAsync(ambientContext,
                    _ => _.Where(__ => __.SourceUrl == importDoc.SourceUrl)))
            {
                _logger.LogDebug("Document already imported.");
                return 0;
            }
            
            // Create an empty document
            var doc = new Document()
            {
                Title = importDoc.Title,
                DocumentDate = importDoc.DocumentDate,
                ShortDescription = importDoc.ShortDescription,
                SourceUrl = importDoc.SourceUrl,
                Classification = classification,
                Source = source
            };
            doc = await _documentRepository.AddAsync(ambientContext, doc);

            var handler = new HttpClientHandler();
            if (!string.IsNullOrEmpty(_applicationSettings.Proxy))
            {
                handler.Proxy = new WebProxy(_applicationSettings.Proxy, true, _applicationSettings.NoProxy.Split(","));
            }
            var httpClient = new HttpClient(handler);
            
            // Create the files
            foreach (var fileImport in importDoc.Files)
            {
                Stream fileStream = null;
                if (fileImport.Filepath.StartsWith("http"))
                {
                    Uri uriResult;
                    if (!Uri.TryCreate(fileImport.Filepath, UriKind.Absolute, out uriResult))
                    {
                        _logger.LogWarning($"File {fileImport.Filepath} could not be downloaded, skipped.");
                        continue;
                    }

                    var httpStream = await httpClient.GetStreamAsync(uriResult);
                    fileStream = new MemoryStream();
                    httpStream.CopyTo(fileStream);
                }
                else
                {
                    var combine = Path.Combine(settings.FileDir, fileImport.Filepath);
                    if (!File.Exists(combine))
                    {
                        _logger.LogWarning($"File {combine} does not exists, skipped.");
                        continue;
                    }
                    
                    fileStream = new FileStream(combine,
                        FileMode.Open,
                        FileAccess.Read);
                }

                var file = new DocumentFile()
                {
                    Document = doc,
                    Filename = fileImport.Filename,
                    Title = fileImport.Title,
                    Visible = true,
                    Preview = true
                };
                await _documentRepository.AddFile(ambientContext, file, fileStream);
                await fileStream.DisposeAsync();
            }
            
            await ambientContext.DatabaseContext.SaveChangesAsync();
        }
        catch (UnauthorizedOperationException)
        {
            AnsiConsole.WriteLine("You are not authorized to import the document");   
        }
        catch (NotFoundEntityException e)
        {
            AnsiConsole.WriteLine($"Document not found: {e.Message}");
        }
        catch (Exception e)
        {
            AnsiConsole.WriteException(e);
        }
            
        return 0;
    }

    public class Settings : DocIntelCommandSettings
    {
        [CommandArgument(0, "<Filepath>")]
        public string Filepath { get; set; }
            
        [CommandOption("--fileDir")]
        public string FileDir { get; set; }
            
        [CommandOption("--sourceId")]
        public string SourceId { get; set; }
    }
}

public class DocumentImport
{
    [JsonPropertyName("title")] public string Title { get; set; }
    [JsonPropertyName("short_description")] public string ShortDescription { get; set; }
    [JsonPropertyName("source_url")] public string SourceUrl { get; set; }
    [JsonPropertyName("document_date")] public DateTime DocumentDate { get; set; }
    [JsonPropertyName("files")] public FileImport[] Files { get; set; }
}

public class FileImport
{
    [JsonPropertyName("filename")] public string Filename { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; }
    [JsonPropertyName("filepath")] public string Filepath { get; set; }
}