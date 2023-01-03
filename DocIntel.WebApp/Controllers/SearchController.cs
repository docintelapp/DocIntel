/* DocIntel
 * Copyright (C) 2018-2021 Belgian Defense, Antoine Cailliau
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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DocIntel.Core.Authentication;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Helpers;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.Search.Documents;
using DocIntel.WebApp.ViewModels.SearchViewModel;
using DocIntel.WebApp.ViewModels.Shared;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DocIntel.WebApp.Controllers
{
    public class SearchController : BaseController
    {
        private readonly IClassificationRepository _classificationRepository;

        private readonly IDocumentRepository _documentRepository;

        private readonly IDocumentSearchEngine _documentSearchEngine;
        private readonly ITagFacetRepository _facetRepository;
        private readonly ILogger _logger;
        private readonly ISourceRepository _sourceRepository;
        private readonly ITagRepository _tagRepository;
        private readonly IUserRepository _userRepository;
        private readonly IGroupRepository _groupRepository;

        public SearchController(DocIntelContext context,
            ILogger<SearchController> logger,
            ApplicationSettings configuration,
            IDocumentSearchEngine documentSearchEngine,
            AppUserManager userManager,
            IAuthorizationService authorizationService,
            IDocumentRepository documentRepository,
            ISourceRepository sourceRepository,
            IUserRepository userRepository,
            ITagRepository tagRepository,
            ITagFacetRepository facetRepository,
            IClassificationRepository classificationRepository, IGroupRepository groupRepository)
            : base(context,
                userManager,
                configuration,
                authorizationService)
        {
            _logger = logger;
            _documentSearchEngine = documentSearchEngine;
            _documentRepository = documentRepository;
            _sourceRepository = sourceRepository;
            _userRepository = userRepository;
            _tagRepository = tagRepository;
            _facetRepository = facetRepository;
            _classificationRepository = classificationRepository;
            _groupRepository = groupRepository;
        }

        public IActionResult Help()
        {
            return View();
        }

        public async Task<ActionResult> Index(
            string searchTerm = "",
            SortCriteria sortCriteria = SortCriteria.Relevance,
            Guid[] tags = null,
            Guid[] sources = null,
            string[] registrants = null,
            Guid[] classifications = null,
            SourceReliability[] reliabilities = null,
            string factualScore = null,
            int pageSize = 10,
            int facetLimit = 20,
            int page = 1)
        {
            var currentUser = await GetCurrentUser();
            await AmbientContext.DatabaseContext.Entry(currentUser).Collection(u => u.Memberships).LoadAsync();

            var watch = Stopwatch.StartNew();
            var defaultPageSize = Math.Min(pageSize, 50);
            searchTerm ??= "";

            int factualScoresLow = -1, factualScoresHigh = 10;
            if (!string.IsNullOrEmpty(factualScore))
            {
                var split = factualScore.Split(";", 2);
                int.TryParse(split[0], out factualScoresLow);
                int.TryParse(split[1], out factualScoresHigh);
            }

            var query = new DocumentSearchQuery
            {
                SearchTerms = searchTerm,
                Tags = tags.ToAsyncEnumerable().SelectAwait(async _ => await _tagRepository.GetAsync(AmbientContext, _))
                    .ToEnumerable(),
                Sources = sources.ToAsyncEnumerable()
                    .SelectAwait(async _ => await _sourceRepository.GetAsync(AmbientContext, _)).ToEnumerable(),
                SelectedRegistrants = registrants.ToAsyncEnumerable()
                    .SelectAwait(async _ => await _userRepository.GetById(AmbientContext, _)).ToEnumerable(),
                SelectedClassifications = classifications.ToAsyncEnumerable()
                    .SelectAwait(async _ => await _classificationRepository.GetAsync(AmbientContext, _)).ToEnumerable(),
                FacetLimit = facetLimit,
                Page = page,
                PageSize = defaultPageSize,
                SortCriteria = sortCriteria,
                SourceReliability = reliabilities,
                FactualScore = new Interval<int>(factualScoresLow, factualScoresHigh)
            };

            var defaultGroups = _groupRepository.GetDefaultGroups(AmbientContext).Select(g => g.GroupId).ToArray();
            var results = _documentSearchEngine.FacetSearch(currentUser, query, defaultGroups);

            var resultsDocuments = new List<DocumentSearchResult>();
            
            _logger.LogInformation("Retreive documents");
            var documents = await _documentRepository.GetAllAsync(AmbientContext, new DocumentQuery()
            {
                DocumentIds = results.Hits.Select(x => x.DocumentId).ToArray()
            }, new[] {"DocumentTags", "DocumentTags.Tag", "DocumentTags.Tag.Facet"}).ToDictionaryAsync(_ => _.DocumentId);
            
            foreach (var hit in results.Hits)
                try
                {
                    var document = documents[hit.DocumentId];
                    resultsDocuments.Add(new DocumentSearchResult
                    {
                        Document = document,
                        Excerpt = hit.Excerpt,
                        TitleExcerpt = hit.TitleExcerpt,
                        Position = hit.Position
                    });
                }
                catch (UnauthorizedOperationException)
                {
                    // TODO Use structured logging
                    _logger.LogError("Unauthorized to view " + hit.DocumentId);
                }
                catch (NotFoundEntityException)
                {
                    // TODO Use structured logging
                    _logger.LogError("Not found " + hit.DocumentId);
                }

            var totalHits = results.TotalHits;

            _logger.LogInformation("Retreive tags");
            var tagIds = results.FacetTags.Select<KeyValuePair<string,int>, Guid?>(_ =>
            {
                if (Guid.TryParse(_.Key, out var id))
                {
                    return id;
                }
                _logger.LogDebug($"Cannot parse Id '{_.Key}'");
                return null;
            }).Where(_ => _ != null).OfType<Guid>().ToArray();
            
            // Get the tags            
            var databaseTags = await _tagRepository.GetAllAsync(AmbientContext,
                new TagQuery() { Ids = tagIds },
                new []{"Facet"}
            ).ToListAsync();

            _logger.LogInformation("Retreive classifications");
            var classDict = await _classificationRepository.GetAllAsync(AmbientContext).ToDictionaryAsync(_ => _.ClassificationId);
            var classificationVR = new List<VerticalResult<Classification>>();
            foreach (var r in results.Classifications)
                try
                {
                    classificationVR.Add(new VerticalResult<Classification>(
                        classDict[r.Value], r.Count
                    ));
                }
                catch (UnauthorizedOperationException)
                {
                    // TODO Use structured logging
                }
                catch (NotFoundEntityException)
                {
                    // TODO Use structured logging
                }

            var reliabilitiesVR = results.Reliabilities;

            _logger.LogInformation("Retreive sources");
            var sourceIds = results.Sources.Select(r => r.Value).ToArray();
            var sourceDict = await _sourceRepository.GetAllAsync(AmbientContext, 
                    _ => _.Where(s => sourceIds.Contains(s.SourceId))
                    ).ToDictionaryAsync(_ => _.SourceId);
            var sourcesVR = new List<VerticalResult<Source>>();
            foreach (var r in results.Sources)
                try
                {
                    sourcesVR.Add(new VerticalResult<Source>(
                        sourceDict[r.Value], r.Count
                    ));
                }
                catch (UnauthorizedOperationException)
                {
                    // TODO Use structured logging
                }
                catch (NotFoundEntityException)
                {
                    // TODO Use structured logging
                }

            _logger.LogInformation("Retreive users");
            var userIds = results.FacetRegistrants.Select(r => r.Value).ToArray();
            var userDict = await _userRepository.GetAllAsync(AmbientContext, new UserQuery() { Ids = userIds })
                .ToDictionaryAsync(_ => _.Id);
            var registrantsVR = new List<VerticalResult<AppUser>>();
            foreach (var r in results.FacetRegistrants)
                try
                {
                    registrantsVR.Add(new VerticalResult<AppUser>(
                        userDict[r.Value], r.Count
                    ));
                }
                catch (UnauthorizedOperationException)
                {
                    // TODO Use structured logging
                }
                catch (NotFoundEntityException)
                {
                    // TODO Use structured logging
                }

            watch.Stop();

            var didYouMeanTerms = new List<string>();
            var accuracy = 0.83f;
            foreach (var q in searchTerm.Split(" "))
                if (!string.IsNullOrEmpty(q))
                {
                    var spellcheck = _documentSearchEngine.SuggestSimilar(q, accuracy, totalHits > 0);
                    if (spellcheck.Length > 0)
                        didYouMeanTerms.Add(spellcheck[0]);
                    else
                        didYouMeanTerms.Add(q);
                }

            var taskSelectedRegistrants = registrants.Select(_ => _userManager.FindByIdAsync(_));
            var selectedRegistrants = await Task.WhenAll(taskSelectedRegistrants);

            _logger.LogInformation("Should be done");
            return View(new SearchIndexViewModel
            {
                SearchTerm = searchTerm,

                SearchResultDocuments = resultsDocuments,

                Tags = databaseTags.GroupBy(_ => _.Facet),
                SelectedTags = _context.Tags.Where(_ => tags.Contains(_.TagId)).ToList(),

                Registrants = registrantsVR,
                SelectedRegistrants = selectedRegistrants,

                Classifications = classificationVR,
                SelectedClassifications = _context.Classifications.Where(_ => classifications.Contains(_.ClassificationId)).ToList(),

                Sources = sourcesVR,
                SelectedSources = _context.Sources.Where(_ => sources.Contains(_.SourceId)).ToList(),

                Reliabilities = reliabilitiesVR,
                SelectedReliabilities = reliabilities,

                FactualScoreLow = factualScoresLow,
                FactualScoresHigh = factualScoresHigh,

                Elapsed = watch.Elapsed,
                DocumentCount = totalHits,
                Page = page,
                PageCount = totalHits / defaultPageSize + (totalHits % defaultPageSize == 0 ? 0 : 1),

                SortBy = sortCriteria,

                FacetLimit = facetLimit,
                PageSize = defaultPageSize,

                // DidYouMean = string.Join(" ", didYouMeanTerms)
            });
        }

        public ActionResult Suggest(string term, float accuracy)
        {
            return Json(_documentSearchEngine.SuggestSimilar(term, accuracy, false));
        }
    }
}