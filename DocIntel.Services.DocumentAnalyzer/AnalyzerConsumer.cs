/* DocIntel
 * Copyright (C) 2018-2021 Belgian Defense, Antoine Cailliau, Kevin Menten
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using DocIntel.Core.Messages;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils;
using DocIntel.Core.Utils.Indexation.SolR;
using DocIntel.Core.Utils.Observables;

using MassTransit;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using SolrNet;

namespace DocIntel.Services.DocumentAnalyzer
{
    public class AnalyzerConsumer : IConsumer<DocumentCreatedMessage>
    {
        private readonly ApplicationSettings _appSettings;
        private readonly IDocumentRepository _documentRepository;
        private readonly IServiceProvider _serviceProvider;

        private readonly ILogger<AnalyzerConsumer> _logger;

        private readonly IObservablesUtility _observablesUtility;
        private readonly ITagRepository _tagRepository;
        private readonly IUserClaimsPrincipalFactory<AppUser> _userClaimsPrincipalFactory;

        private readonly ISolrOperations<IndexedDocument> _solr;
        private TagFacet _singletonCveFacet;
        private TagFacet facet;
        private readonly TagUtility _tagUtility;
        private HashSet<Tag> _tagCache;

        public AnalyzerConsumer(ILogger<AnalyzerConsumer> logger,
            IDocumentRepository documentRepository,
            DocIntelContext dbContext,
            IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory,
            ApplicationSettings appSettings, 
            ITagRepository tagRepository,
            ISolrOperations<IndexedDocument> solr, 
            ITagFacetRepository facetRepository,
            IObservablesUtility observablesUtility, TagUtility tagUtility, IServiceProvider serviceProvider) 
        {
            _logger = logger;
            _documentRepository = documentRepository;
            _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
            _appSettings = appSettings;
            _tagRepository = tagRepository;
            _solr = solr;
            _observablesUtility = observablesUtility;
            _tagUtility = tagUtility;
            _serviceProvider = serviceProvider;
        }

        public async Task Consume(ConsumeContext<DocumentCreatedMessage> context)
        {
            _logger.LogDebug("DocumentCreatedMessage: {0}", context.Message.DocumentId);
            var ambientContext = GetAmbientContext();

            try
            {
                var document =
                    await _documentRepository.GetAsync(ambientContext, context.Message.DocumentId, new[]
                    {
                        nameof(Document.Files),
                        nameof(Document.DocumentTags),
                        nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag),
                        nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag) + "." + nameof(Tag.Facet)
                    });
                await Analyze(ambientContext, document);
            }
            catch (Exception e)
            {
                _logger.LogError($"Document {context.Message.DocumentId} could not be analyzed ({e.Message}).");
                _logger.LogError(e.StackTrace);
            }
        }

        public async Task ConsumeBacklogAsync()
        {
            foreach (var submitted in await _documentRepository.GetAllAsync(GetAmbientContext(),
                _ => _.Include(__ => __.Classification)
                    .Include(__ => __.ReleasableTo)
                    .Include(__ => __.EyesOnly)
                    .Include(__ => __.Files)
                    .Include(__ => __.DocumentTags)
                    .ThenInclude(__ => __.Tag)
                    .ThenInclude(__ => __.Facet)
                    .AsQueryable()
                    .Where(__ => __.Status == DocumentStatus.Submitted)).ToListAsync())
            {
                var ambientContext = GetAmbientContext();
                await Analyze(ambientContext, submitted);
            }
        }

        private async Task Analyze(AmbientContext ambientContext, Document document)
        {
            try
            {
                _tagCache = new HashSet<Tag>();
                var doExtractObservables = true;
                if (document.MetaData != default && document.MetaData.ContainsKey("ExtractObservables"))
                    if (document.MetaData["ExtractObservables"].ToString().ToLower() == "false")
                        doExtractObservables = false;

                foreach (var file in document.Files.Where(_ =>
                    (_.MimeType == "application/pdf") | _.MimeType.StartsWith("text/")))
                {
                    var filename = Path.Combine(_appSettings.DocFolder, file.Filepath);
                    if (File.Exists(filename))
                    {
                        try
                        {
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

                            DetectAttckTechniques(response, ambientContext, document);
                            await DetectCVE(response, ambientContext, document);
                            await DetectTLP(response, ambientContext, document);
                            await DetectActor(response, ambientContext, document);

                            if (doExtractObservables)
                            {
                                var res = await _observablesUtility.DetectObservables(response.Content,
                                    document, file, document.Status == DocumentStatus.Registered);
                                // for attack/cve/tlp the tags are null
                                if (document.DocumentTags.Any(u => u.Tag is not null) && res.Any())
                                {
                                    var rel = await _observablesUtility.DetectRelations(res, document);
                                }
                            }

                            if (string.IsNullOrEmpty(document.Title) & !string.IsNullOrEmpty(title))
                                document.Title = title;

                            if (file.DocumentDate == DateTime.MinValue)
                                file.DocumentDate = date;
                        }
                        catch (SolrNet.Exceptions.SolrConnectionException e)
                        {
                            _logger.LogError($"Document {document.DocumentId} could not be analyzed due to an error with SolR.");
                            _logger.LogError(e.Url);
                            _logger.LogError(e.ToString());
                        }
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
                    document.Status = DocumentStatus.Analyzed;

                await ambientContext.DatabaseContext.SaveChangesAsync();
                facet = null;
                _singletonCveFacet = null;

                // TODO Use structured logging
                _logger.LogInformation($"Document {document.Reference} ({document.DocumentId}) successfully analyzed.");
            }
            catch (Exception e)
            {
                _logger.LogError($"Document {document.DocumentId} could not be analyzed ({e.GetType()} {e.Message}).");
                _logger.LogError(e.StackTrace);
                _logger.LogError(e.InnerException?.Message);
            }
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

        private void DetectAttckTechniques(ExtractResponse response, AmbientContext ambientContext, Document document)
        {
            var pattern = @"(T[0-9]{4}(\.[0-9]{3})?)";
            var patternMatches = Regex.Matches(response.Content, pattern).Select(_ => _.ToString().ToUpper())
                .Distinct();
            
            _logger.LogDebug("Detected techniques: " + string.Join(",", patternMatches));
            
            foreach (var match in patternMatches)
            {
                var id = match;
                var tags = _tagRepository.GetAllAsync(ambientContext, new TagQuery
                {
                    StartsWith = id
                }).ToEnumerable();
                foreach (var t in tags)
                    if (!document.DocumentTags.Any(_ => _.DocumentId == document.DocumentId && _.TagId == t.TagId))
                        document.DocumentTags.Add(new DocumentTag
                        {
                            DocumentId = document.DocumentId,
                            TagId = t.TagId
                        });
            }
        }

        private async Task DetectCVE(ExtractResponse response, AmbientContext ambientContext, Document document)
        {   
            // TODO Move 'vulnerability' to configuration so it can be customized
            _singletonCveFacet ??= await _tagUtility.GetOrCreateFacet(ambientContext, "vulnerability", "Vulnerability");

            var pattern = @"CVE-\d{4}-\d{4,7}";
            var patternMatches = Regex.Matches(response.Content, pattern, RegexOptions.IgnoreCase)
                .Select(_ => _.ToString().ToUpper())
                .Distinct();
            
            _logger.LogDebug("Detected CVEs: " + string.Join(",", patternMatches));
            
            foreach (var match in patternMatches)
            {
                var tag = await _tagUtility.GetOrCreateTag(ambientContext, _singletonCveFacet.Id, match.ToUpper(), _tagCache);
                if (!document.DocumentTags.Any(_ => _.DocumentId == document.DocumentId && _.TagId == tag.TagId))
                    document.DocumentTags.Add(new DocumentTag
                    {
                        DocumentId = document.DocumentId,
                        TagId = tag.TagId
                    });
            }
        }

        private async Task DetectTLP(ExtractResponse response, AmbientContext ambientContext, Document document)
        {
            // TODO Move 'vulnerability' to configuration so it can be customized
            facet ??= await _tagUtility.GetOrCreateFacet(ambientContext, "TLP", "TLP");
            var pattern = @"tlp[\:/_\s\-]+(red|green|white|amber)";
            var patternMatches = Regex
                .Matches(response.Content, pattern, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace)
                .Select(_ => _.Groups[1].ToString()).Distinct();
            
            _logger.LogDebug("Detected TLPs: " + string.Join(",", patternMatches));
            
            foreach (var match in patternMatches)
            {
                var tag = await _tagUtility.GetOrCreateTag(ambientContext, facet.Id, match.ToUpper(), _tagCache);
                if (!document.DocumentTags.Any(_ => _.DocumentId == document.DocumentId && _.TagId == tag.TagId))
                    document.DocumentTags.Add(new DocumentTag
                    {
                        DocumentId = document.DocumentId,
                        TagId = tag.TagId
                    });
            }
        }

        private async Task DetectActor(ExtractResponse response, AmbientContext ambientContext, Document document)
        {
            // TODO Move 'actor' to configuration so it can be customized
            var facet = await _tagUtility.GetOrCreateFacet(ambientContext, "actor", "actor");
            var pattern = @" (apt|ta)[ \-]*[0-9]+";
            var patternMatches = Regex
                .Matches(response.Content, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace)
                .Select(_ => _.Groups[0].ToString().Trim())
                .ToHashSet();

            _logger.LogDebug("Detected actors: " + string.Join(",", patternMatches));
            
            foreach (var match in patternMatches)
            {
                var tag = await _tagUtility.GetOrCreateTag(ambientContext, facet.Id, match.ToUpper(), _tagCache);
                if (!document.DocumentTags.Any(_ => _.DocumentId == document.DocumentId && _.TagId == tag.TagId))
                    document.DocumentTags.Add(new DocumentTag
                    {
                        DocumentId = document.DocumentId,
                        TagId = tag.TagId
                    });
            }
        }

        // TODO Refactor, code duplication
        private AmbientContext GetAmbientContext()
        {
            var dbContextOptions = _serviceProvider.GetRequiredService<DbContextOptions<DocIntelContext>>();
            var dbContextLogger = _serviceProvider.GetRequiredService<ILogger<DocIntelContext>>();
            var _dbContext = new DocIntelContext(dbContextOptions, dbContextLogger);
            var automationUser = _dbContext.Users.FirstOrDefault(_ => _.UserName == _appSettings.AutomationAccount);
            if (automationUser == null)
                throw new ArgumentNullException($"User '{_appSettings.AutomationAccount}' was not found.");

            var claims = _userClaimsPrincipalFactory.CreateAsync(automationUser).Result;
            var ambientContext = new AmbientContext
            {
                DatabaseContext = _dbContext,
                Claims = claims,
                CurrentUser = automationUser
            };
            return ambientContext;
        }
    }
}