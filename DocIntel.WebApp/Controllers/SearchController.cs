/* DocIntel
 * Copyright (C) 2018-2023 Belgian Defense, Antoine Cailliau
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
using Microsoft.AspNetCore.Routing;
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
        private readonly ISavedSearchRepository _savedSearchRepository;

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
            IClassificationRepository classificationRepository, IGroupRepository groupRepository, ISavedSearchRepository savedSearchRepository)
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
            _savedSearchRepository = savedSearchRepository;
        }

        public IActionResult Help()
        {
            return View();
        }

        public async Task<ActionResult> Index(string searchTerm = "")
        {
            var defaultUserSavedSearch = _savedSearchRepository.GetDefault(AmbientContext);
            if (defaultUserSavedSearch != null)
            {
                var savedSearch = defaultUserSavedSearch.SavedSearch;
                var parameters = BuildRouteValueDictionary(savedSearch, searchTerm);
                return RedirectToAction("Search", parameters);
            }

            return RedirectToAction("Search", new { searchTerm = searchTerm });
        }

        private static RouteValueDictionary BuildRouteValueDictionary(SavedSearch savedSearch, string searchTerm)
        {
            var parameters = new RouteValueDictionary();
            if (!string.IsNullOrEmpty(searchTerm))
                parameters.Add("searchTerm", searchTerm);
            else
                parameters.Add("searchTerm", savedSearch.SearchTerm);
            
            parameters.Add("sortCriteria", savedSearch.SortCriteria);
            parameters.Add("pageSize", savedSearch.PageSize);

            int index = 0;
            foreach (var searchFilter in savedSearch.Filters)
            {
                parameters.Add($"filters[{index}].id", searchFilter.Id);
                parameters.Add($"filters[{index}].name", searchFilter.Name);
                parameters.Add($"filters[{index}].field", searchFilter.Field);
                parameters.Add($"filters[{index}].negate", searchFilter.Negate);
                parameters.Add($"filters[{index}].operator", searchFilter.Operator);
                int indexValue = 0;
                foreach (var value in searchFilter.Values)
                {
                    parameters.Add($"filters[{index}].values[{indexValue}].id", value.Id);
                    parameters.Add($"filters[{index}].values[{indexValue}].name", value.Name);
                    parameters.Add($"filters[{index}].values[{indexValue}].color", value.Color);
                    indexValue++;
                }

                index++;
            }

            return parameters;
        }

        public async Task<ActionResult> Search(
                string searchTerm = "",
                SortCriteria sortCriteria = SortCriteria.Relevance,
                SearchFilter[] filters = null,
                int pageSize = 10,
                int facetLimit = 20,
                int page = 1)
            {
            var currentUser = await GetCurrentUser();
            await AmbientContext.DatabaseContext.Entry(currentUser).Collection(u => u.Memberships).LoadAsync();

            var watch = Stopwatch.StartNew();
            var defaultPageSize = Math.Min(pageSize, 50);
            searchTerm ??= "";

            var query = new DocumentSearchQuery
            {
                SearchTerms = searchTerm,
                Filters = filters,
                FacetLimit = facetLimit,
                Page = page,
                PageSize = defaultPageSize,
                SortCriteria = sortCriteria
            };

            var defaultGroups = _groupRepository.GetDefaultGroups(AmbientContext).Select(g => g.GroupId).ToArray();
            var results = _documentSearchEngine.FacetSearch(currentUser, query, defaultGroups);

            var resultsDocuments = new List<DocumentSearchResult>();
            
            _logger.LogDebug("Retreive documents");
            var documents = await _documentRepository.GetAllAsync(AmbientContext, new DocumentQuery()
            {
                DocumentIds = results.Hits.Select(x => x.DocumentId).ToArray(),
                Limit = -1
            }, new[] {"DocumentTags", "DocumentTags.Tag", "DocumentTags.Tag.Facet"}).ToDictionaryAsync(_ => _.DocumentId);
            
            foreach (var hit in results.Hits)
            {
                try
                {
                    if (documents.ContainsKey(hit.DocumentId))
                    {
                        var document = documents[hit.DocumentId];
                        resultsDocuments.Add(new DocumentSearchResult
                        {
                            Document = document,
                            Excerpt = hit.Excerpt,
                            TitleExcerpt = hit.TitleExcerpt,
                            Position = hit.Position
                        });   
                    } else {
                        _logger.LogWarning($"Your SolR index might be out-of-sync. The document '{hit.DocumentId}' was found in the index but not in the database.");
                    }
                }
                catch (UnauthorizedOperationException)
                {
                    // TODO Use structured logging
                    _logger.LogError($"User '{currentUser.FriendlyName}' is unauthorized to view '{hit.DocumentId}' returned by the search engine.");
                }
            }

            var totalHits = results.TotalHits;

            _logger.LogDebug("Retreive tags");
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

            _logger.LogDebug("Retreive classifications");
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

            _logger.LogDebug("Retreive sources");
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

            _logger.LogDebug("Retreive users");
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

            return View(new SearchIndexViewModel
            {
                SearchTerm = searchTerm,
                SearchResultDocuments = resultsDocuments,
                Tags = databaseTags.GroupBy(_ => _.Facet),
                Registrants = registrantsVR,
                Classifications = classificationVR,
                Sources = sourcesVR,
                Reliabilities = reliabilitiesVR,
                Elapsed = watch.Elapsed,
                DocumentCount = totalHits,
                Page = page,
                PageCount = totalHits / defaultPageSize + (totalHits % defaultPageSize == 0 ? 0 : 1),
                SortBy = sortCriteria,
                FacetLimit = facetLimit,
                PageSize = defaultPageSize,
                Filters = filters
            });
        }

        public ActionResult Suggest(string term, float accuracy)
        {
            return Json(_documentSearchEngine.SuggestSimilar(term, accuracy, false));
        }

        public async Task<ActionResult> SaveAsDefault(
            string searchTerm = "",
            SortCriteria sortCriteria = SortCriteria.Relevance,
            IList<SearchFilter> filters = null,
            int pageSize = 10,
            int facetLimit = 20)
        {
            var userSavedSearch = _savedSearchRepository.GetDefault(AmbientContext);
            if (userSavedSearch != null && !userSavedSearch.SavedSearch.Public)
            {
                _logger.LogDebug("Update default search for " + AmbientContext.CurrentUser.FriendlyName);
                userSavedSearch.SavedSearch.SearchTerm = searchTerm;
                userSavedSearch.SavedSearch.Filters = filters;
                userSavedSearch.SavedSearch.SortCriteria = sortCriteria;
                userSavedSearch.SavedSearch.PageSize = pageSize;
                
                await _savedSearchRepository.UpdateAsync(AmbientContext, userSavedSearch.SavedSearch);
            }
            else
            {
                _logger.LogDebug("Saved new default search for " + AmbientContext.CurrentUser.FriendlyName);
                var savedSearch = new SavedSearch
                {
                    SearchTerm = searchTerm,
                    Filters = filters,
                    SortCriteria = sortCriteria,
                    PageSize = pageSize
                };

                userSavedSearch = await _savedSearchRepository.SetDefault(AmbientContext, savedSearch);
            }

            await AmbientContext.DatabaseContext.SaveChangesAsync();
            
            var parameters = BuildRouteValueDictionary(userSavedSearch.SavedSearch, searchTerm);
            return RedirectToAction("Search", parameters);
        }
    }
}