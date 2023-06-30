using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DocIntel.Core.Messages;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.Features;
using DocIntel.Core.Utils.Indexation.SolR;
using DocIntel.Core.Utils.Observables;
using Microsoft.Extensions.Logging;
using SolrNet;
using SolrNet.Exceptions;

namespace DocIntel.Core.Utils;

public class DocumentAnalyzerUtility
{
    private readonly ApplicationSettings _appSettings;
    private readonly IDocumentRepository _documentRepository;
    private readonly ITagFacetRepository _facetRepository;
    private readonly ILogger<DocumentAnalyzerUtility> _logger;
    private readonly ILogger<TagFacetFeatureExtractor> _loggerExtractor;
    private readonly ISynapseRepository _observablesRepository;
    private readonly IObservablesUtility _observablesUtility;
    private readonly ISolrOperations<IndexedDocument> _solr;
    private readonly TagUtility _tagUtility;

    public DocumentAnalyzerUtility(ILogger<DocumentAnalyzerUtility> logger,
        ILogger<TagFacetFeatureExtractor> loggerExtractor,
        IDocumentRepository documentRepository,
        ApplicationSettings appSettings, 
        ISolrOperations<IndexedDocument> solr,
        IObservablesUtility observablesUtility,
        TagUtility tagUtility,
        ISynapseRepository observablesRepository, 
        ITagFacetRepository facetRepository)
    {
        _logger = logger;
        _loggerExtractor = loggerExtractor;
        _documentRepository = documentRepository;
        _appSettings = appSettings;
        _solr = solr;
        _observablesUtility = observablesUtility;
        _tagUtility = tagUtility;
        _observablesRepository = observablesRepository;
        _facetRepository = facetRepository;
    }

    public async Task<bool> Analyze(Guid documentId, AmbientContext ambientContext)
    {   
        try
        {
            var document =
                await _documentRepository.GetAsync(ambientContext, documentId, new[]
                {
                    nameof(Document.Classification),
                    nameof(Document.ReleasableTo),
                    nameof(Document.EyesOnly),
                    nameof(Document.Files),
                    nameof(Document.Source),
                    nameof(Document.DocumentTags),
                    nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag),
                    nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag) + "." + nameof(Tag.Facet)
                });

            if (document.Status == DocumentStatus.Registered)
                return true;
            
            var tagCache = new HashSet<Tag>();
            var facetCache = new HashSet<TagFacet>();
            
            var doExtractObservables = true;
            ExtractionMetaData extractionMetaData = null;
            if (document.MetaData != default 
                && document.MetaData.ContainsKey("extraction")
                && (extractionMetaData = document.MetaData["extraction"].Deserialize<ExtractionMetaData>()) != null
                && !extractionMetaData.StructuredData)
            {
                _logger.LogDebug("Skip extraction of structured data to document metadata");
                doExtractObservables = false;
            }

            // Check if source requires no extraction
            if (document.Source != null)
            {
                ExtractionMetaData sourceExtractionMetaData = null;
                if (document.Source.MetaData != default 
                    && document.Source.MetaData.ContainsKey("extraction")
                    && (sourceExtractionMetaData = document.Source.MetaData["extraction"].Deserialize<ExtractionMetaData>()) != null
                    && !sourceExtractionMetaData.StructuredData) {
                    _logger.LogDebug("Skip extraction of structured data to source metadata");
                    doExtractObservables = false;
                }
            }

            foreach (var file in document.Files.Where(_ =>
                (_.MimeType == "application/pdf") | _.MimeType.StartsWith("text/")))
            {
                if (file.Filepath != null)
                {
                    var filename = Path.Combine(_appSettings.DocFolder, file.Filepath);
                    if (File.Exists(filename))
                    {
                        try
                        {
                            _logger.LogDebug($"Will analyze file {file.FileId}");
                            await using var f = File.OpenRead(filename);
                            var response = await _solr.ExtractAsync(new ExtractParameters(f, document.DocumentId.ToString())
                            {
                                ExtractOnly = true,
                                ExtractFormat = ExtractFormat.Text,
                                StreamType = file.MimeType
                            });

                            var metadata = response.Metadata.ToDictionary(_ => _.FieldName, _ => _.Value);
                            var title = ExtractTitle(metadata);

                            var date = ExtractDate(metadata);
                            if (date == DateTime.MinValue)
                                date = DateTime.UtcNow;

                            await DetectAutoExtractFacet(response, ambientContext, document, tagCache, facetCache);

                            if (doExtractObservables)
                            {
                                try
                                {
                                    var view = await _observablesRepository.CreateView(document);
                                    var fileObservables = await _observablesUtility.ExtractDataAsync(document, file, response.Content).ToListAsync();
                                    _logger.LogDebug($"Found {fileObservables.Count} observables");
                                    await _observablesUtility.AnnotateAsync(document, file, fileObservables);
                                    _logger.LogDebug($"Annotated {fileObservables.Count} observables");
                                    await _observablesRepository.Add(fileObservables, document, view);
                                    _logger.LogDebug($"Addded {fileObservables.Count} observables to document");
                                }
                                catch (Exception e)
                                {
                                    _logger.LogError($"Could not extract observable in file '{file.FileId}' on document '{document.DocumentId}': {e.Message}");
                                }
                            }

                            if (string.IsNullOrEmpty(document.Title) & !string.IsNullOrEmpty(title))
                                document.Title = title;

                            if (file.DocumentDate == DateTime.MinValue)
                                file.DocumentDate = date;
                        }
                        catch (SolrConnectionException e)
                        {
                            _logger.LogError($"File {file.FileId} from {document.DocumentId} could not be analyzed due to an error with SolR: {e.Url}\n{e.ToString()}");
                        }
                        catch (Exception e)
                        {
                            _logger.LogError($"File {file.FileId} from {document.DocumentId} could not be analyzed due to an error {e.Message}\n{e.ToString()}");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Could not find file '{filename}' for document {document.DocumentId}");
                    }
                }
                else
                {
                    _logger.LogWarning($"Could not find file '{file.FileId}' for document {document.DocumentId}: no filename");
                }
            }

            if (document.DocumentDate == DateTime.MinValue)
            {
                var ddate = document.Files?.Min(_ => _.DocumentDate) ?? DateTime.MinValue;
                if (ddate == DateTime.MinValue)
                    ddate = DateTime.UtcNow;
                document.DocumentDate = ddate;
            }

            if (document.Status == DocumentStatus.Submitted)
            {
                document.Status = DocumentStatus.Analyzed;

                bool skip = false;
                if (document.MetaData?.ContainsKey("registration") ?? false)
                {
                    RegistrationMetadata registrationMetadata = null;
                    if ((registrationMetadata =
                            document.MetaData["registration"].Deserialize<RegistrationMetadata>()) != null
                        && registrationMetadata.Auto)
                    {
                        _logger.LogDebug("Skip inbox due to document setting");
                        await _documentRepository.UpdateStatusAsync(ambientContext, document.DocumentId, DocumentStatus.Registered);
                        await _observablesRepository.Merge(document);
                        skip = true;
                    }
                }
                
                // Check if auto-register is enabled for the source
                if (!skip && document.Source != null)
                {
                    RegistrationMetadata registrationMetadata = null;
                    if (document.Source.MetaData != default
                        && document.Source.MetaData.ContainsKey("registration")
                        && (registrationMetadata =
                            document.Source.MetaData["registration"].Deserialize<RegistrationMetadata>()) != null
                        && registrationMetadata.Auto)
                    {
                        _logger.LogDebug("Skip inbox due to source setting");
                        await _documentRepository.UpdateStatusAsync(ambientContext, document.DocumentId, DocumentStatus.Registered);
                        await _observablesRepository.Merge(document);
                    }
                }
            }

            _logger.LogDebug($"Status of document '{document.DocumentId}' is '{document.Status}'.");
            
            var trackingEntity = ambientContext.DatabaseContext.Update(document);
            
            // TODO Move outside of utility?
            await ambientContext.DatabaseContext.SaveChangesAsync();
            
            // TODO Use structured logging
            _logger.LogInformation($"Document {document.Reference} ({document.DocumentId}) successfully analyzed.");
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError($"Document {documentId} could not be analyzed ({e.GetType()} {e.Message})\n{e.StackTrace}\n----\n{e.InnerException?.Message}.");
        }

        return false;
    }

    private static DateTime ExtractDate(Dictionary<string, string> metadata)
    {
        var date = DateTime.MinValue;
        var dateFields = new[]
            {"dcterms:created", "meta:creation-date", "created", "Creation-Date", "pdf:docinfo:created"};
        foreach (var df in dateFields)
            if (metadata.ContainsKey(df) && !string.IsNullOrEmpty(metadata[df]))
                if (DateTime.TryParseExact(metadata[df], "yyyy-MM-ddThh:mm:ssZ", CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out date))
                    break;

        return date;
    }

    private static string ExtractTitle(Dictionary<string, string> metadata)
    {
        var title = "";
        var titleFields = new[] {"dc:title", "title"};
        foreach (var df in titleFields)
            if (metadata.ContainsKey(df) && !string.IsNullOrEmpty(metadata[df]))
            {
                title = metadata[df];
                break;
            }

        return title;
    }

    private async Task DetectAutoExtractFacet(ExtractResponse response, AmbientContext ambientContext,
        Document document, HashSet<Tag> tagCache, HashSet<TagFacet> facetCache)
    {
        var facets = await _facetRepository.GetAllAsync(ambientContext, new FacetQuery(), new string[] { "Tags" }).ToListAsync();

        foreach (var facet in facets)
        {
            _logger.LogTrace($"Checking for tags in facet '{facet.Title}'");
            var extractor = new TagFacetFeatureExtractor(facet, _loggerExtractor);
            var patternMatches = extractor.Extract(response.Content).ToList();

            if (patternMatches.Any())
            {
                _logger.LogDebug($"Detected tags in '{facet.Title}': " + string.Join(",", patternMatches));

                try
                {
                    var tags = _tagUtility
                        .GetOrCreateTags(ambientContext, patternMatches
                                .Where(match => !string.IsNullOrEmpty(match))
                                .Select(_ => facet.Prefix+":"+_), tagCache,
                            facetCache);

                    _logger.LogDebug($"Detected tags: " + string.Join(",", tags.Select(_ => _.TagId)));
                    
                    foreach (var tag in tags)
                    {
                        if (!document.DocumentTags.Any(_ =>
                                _.DocumentId == document.DocumentId && _.TagId == tag.TagId))
                            document.DocumentTags.Add(new DocumentTag
                            {
                                DocumentId = document.DocumentId,
                                TagId = tag.TagId
                            });
                    }
                } 
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                }
            }
             
        }
    }
}
