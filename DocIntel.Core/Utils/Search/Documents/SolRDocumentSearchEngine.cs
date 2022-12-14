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
using System.IO;
using System.Linq;
using System.Net;

using DocIntel.Core.Models;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.Indexation.SolR;

using Microsoft.Extensions.Logging;

using SolrNet;
using SolrNet.Commands.Parameters;
using SolrNet.Exceptions;

namespace DocIntel.Core.Utils.Search.Documents
{
    public class SolRDocumentSearchEngine : IDocumentSearchEngine
    {
        private readonly ILogger<SolRDocumentSearchEngine> _logger;
        private readonly ISolrOperations<IndexedDocument> _solr;
        private readonly ApplicationSettings _settings; 

        public SolRDocumentSearchEngine(
            ISolrOperations<IndexedDocument> solr,
            ILogger<SolRDocumentSearchEngine> logger, 
            ApplicationSettings settings)
        {
            _solr = solr;
            _logger = logger;
            _settings = settings;
        }

        public SearchResults FacetSearch(AppUser user, DocumentSearchQuery query, Guid[] defaultGroups)
        {
            try
            {
                ISolrQuery q;
                if (!string.IsNullOrEmpty(query.SearchTerms))
                {
                    q = new SolrQuery(query.SearchTerms);
                    _logger.LogDebug("Query search terms: " + ((SolrQuery) q).Query);
                }
                else
                {
                    q = SolrQuery.All;
                }

                var facetQuery = BuildFacetQuery(user, query, defaultGroups);
                
                var weights = $"{SolRHelper<IndexedDocument>.GetSolRName(_ => _.Reference)}^20 "
                              + $"{SolRHelper<IndexedDocument>.GetSolRName(_ => _.Title)}^10 "
                              + $"{SolRHelper<IndexedDocument>.GetSolRName(_ => _.Tags)}^10 "
                              + $"{SolRHelper<IndexedDocument>.GetSolRName(_ => _.ShortDescription)}^5 "
                              + $"{SolRHelper<IndexedDocument>.GetSolRName(_ => _.FileContents)}^5 "
                              + $"{SolRHelper<IndexedDocument>.GetSolRName(_ => _.Observables)}^5 "
                              + $"{SolRHelper<IndexedDocument>.GetSolRName(_ => _.Comments)}^3";

                /*
                _logger.LogDebug("Weights: " + weights);
                _logger.LogDebug("Page: " + query.Page);
                _logger.LogDebug("Page: " + query.PageSize);
                */
                
                var sortOrder = new List<SortOrder>();
                if (query.SortCriteria == SortCriteria.DocumentDate)
                    sortOrder.Add(new SortOrder(SolRHelper<IndexedDocument>.GetSolRName(_ => _.DocumentDate),
                        Order.DESC));
                else if (query.SortCriteria == SortCriteria.ModificationDate)
                    sortOrder.Add(new SortOrder(SolRHelper<IndexedDocument>.GetSolRName(_ => _.ModificationDate),
                        Order.DESC));
                else if (query.SortCriteria == SortCriteria.RegistrationDate)
                    sortOrder.Add(new SortOrder(SolRHelper<IndexedDocument>.GetSolRName(_ => _.RegistrationDate),
                        Order.DESC));
                sortOrder.Add(SortOrder.Parse("score desc"));

                // Ensure that page is always at least 1
                query.Page = Math.Max(query.Page, 1);

                var results = _solr.Query(q, new QueryOptions
                {
                    StartOrCursor = new StartOrCursor.Start((query.Page - 1) * query.PageSize),
                    Rows = query.PageSize,
                    Highlight = new HighlightingParameters
                    {
                        Fields = new[]
                        {
                            SolRHelper<IndexedDocument>.GetSolRName(_ => _.Title),
                            SolRHelper<IndexedDocument>.GetSolRName(_ => _.ShortDescription),
                            SolRHelper<IndexedDocument>.GetSolRName(_ => _.FileContents)
                        }
                    },
                    Facet = new FacetParameters
                    {
                        Limit = 1000,
                        Queries = new[]
                        {
                            new SolrFacetFieldQuery(SolRHelper<IndexedDocument>.GetSolRName(_ => _.TagsId)),
                            new SolrFacetFieldQuery(SolRHelper<IndexedDocument>.GetSolRName(_ => _.RegisteredById)),
                            new SolrFacetFieldQuery(SolRHelper<IndexedDocument>.GetSolRName(_ => _.Classification)),
                            new SolrFacetFieldQuery(SolRHelper<IndexedDocument>.GetSolRName(_ => _.SourceId)),
                            new SolrFacetFieldQuery(SolRHelper<IndexedDocument>.GetSolRName(_ => _.ReliabilityScore))
                        }
                    },
                    OrderBy = sortOrder,
                    ExtraParams = new Dictionary<string, string>
                    {
                        {"qf", weights},
                        {"fq", facetQuery},
                        {"defType", "edismax"},
                        {"hl.fragsize", "250"},
                        {"hl.simple.pre", "<span class='bg-warning-50'>"},
                        {"hl.simple.post", "</span>"},
                        {"bf", "recip(ms(NOW,registration_date),3.16e-11,1,1)"}
                    }
                });

                var sr = new SearchResults();
                sr.TotalHits = results.NumFound;
                foreach (var r in results)
                {
                    var highlightedSnippets = results.Highlights[r.DocumentId.ToString()];
                    var item = new SearchHit
                    {
                        DocumentId = r.DocumentId
                    };

                    if (highlightedSnippets.ContainsKey(SolRHelper<IndexedDocument>.GetSolRName(_ => _.Title)))
                        item.TitleExcerpt = highlightedSnippets[SolRHelper<IndexedDocument>.GetSolRName(_ => _.Title)]
                            .FirstOrDefault();

                    if (highlightedSnippets.ContainsKey(
                        SolRHelper<IndexedDocument>.GetSolRName(_ => _.ShortDescription)))
                        item.Excerpt = string.Join(" ",
                            highlightedSnippets[SolRHelper<IndexedDocument>.GetSolRName(_ => _.ShortDescription)]
                                .Take(3));

                    if (highlightedSnippets.ContainsKey(SolRHelper<IndexedDocument>.GetSolRName(_ => _.FileContents)))
                        item.Excerpt = string.Join(" ",
                            highlightedSnippets[SolRHelper<IndexedDocument>.GetSolRName(_ => _.FileContents)].Take(3));

                    sr.Hits.Add(item);
                }

                var hvrs = new Dictionary<string, HierarchicalVerticalResult<Guid, Guid>>();
                foreach (var facet in results.FacetFields[SolRHelper<IndexedDocument>.GetSolRName(_ => _.TagsId)])
                {
                    _logger.LogDebug(facet.Key);
                    var prefix = facet.Key;
                    var label = "";
                    if (facet.Key.Contains("/"))
                    {
                        prefix = facet.Key.Split("/", 2)[0];
                        label = facet.Key.Split("/", 2)[1].Trim('/');
                    }

                    if (string.IsNullOrEmpty(label))
                    {
                        if (hvrs.ContainsKey(prefix))
                            hvrs[prefix].Count = facet.Value;
                        else
                            hvrs.Add(prefix,
                                new HierarchicalVerticalResult<Guid, Guid>(Guid.Parse(prefix), facet.Value));
                    }
                    else
                    {
                        if (!hvrs.ContainsKey(prefix))
                            hvrs.Add(prefix, new HierarchicalVerticalResult<Guid, Guid>(Guid.Parse(prefix), 0));

                        hvrs[prefix].Elements.Add(new VerticalResult<Guid>(Guid.Parse(label), facet.Value));
                    }
                }

                sr.FacetTags = hvrs.Values.ToList();

                var facetRegistrants = new List<VerticalResult<string>>();
                foreach (var facet in results.FacetFields[
                    SolRHelper<IndexedDocument>.GetSolRName(_ => _.RegisteredById)])
                    facetRegistrants.Add(new VerticalResult<string>(facet.Key, facet.Value));
                sr.FacetRegistrants = facetRegistrants;

                var facetClassification = new List<VerticalResult<Guid>>();
                foreach (var facet in results.FacetFields
                    [SolRHelper<IndexedDocument>.GetSolRName(_ => _.Classification)])
                    facetClassification.Add(new VerticalResult<Guid>(
                        (Guid) Guid.Parse(facet.Key), facet.Value));
                sr.Classifications = facetClassification;

                var facetSource = new List<VerticalResult<Guid>>();
                foreach (var facet in results.FacetFields
                    [SolRHelper<IndexedDocument>.GetSolRName(_ => _.SourceId)])
                    facetSource.Add(new VerticalResult<Guid>(
                        (Guid) Guid.Parse(facet.Key), facet.Value));
                sr.Sources = facetSource;

                var facetReliability = new List<VerticalResult<SourceReliability>>();
                foreach (var facet in results.FacetFields
                    [SolRHelper<IndexedDocument>.GetSolRName(_ => _.ReliabilityScore)])
                    facetReliability.Add(new VerticalResult<SourceReliability>(
                        Enum.Parse<SourceReliability>(facet.Key), facet.Value));
                sr.Reliabilities = facetReliability;

                return sr;
            }
            catch (SolrConnectionException e)
            {
                _logger.LogError(e.Message);
                _logger.LogError(e.Url);
                _logger.LogWarning(e.StackTrace);

                if (e.InnerException is WebException ee)
                {
                    var resp = new StreamReader(ee.Response.GetResponseStream()).ReadToEnd();
                    _logger.LogWarning(resp);
                }

                throw e;
            }
        }

        public string[] SuggestSimilar(string term, float accuracy, bool more_popular)
        {
            return new string[] { };
        }

        private string BuildFacetQuery(AppUser user, DocumentSearchQuery query, Guid[] defaultGroup)
        {
            var facetQueries = new List<string>();
            
            if (query.Sources != null && query.Sources.Any())
                facetQueries.Add(
                    string.Join(" OR ",
                        query.Sources.Select(_ =>
                            SolRHelper<IndexedDocument>.GetSolRName(_ => _.SourceId) + ":" + _.SourceId))
                );
            
            if (query.SourceReliability != null && query.SourceReliability.Any())
                facetQueries.Add(
                    string.Join(" OR ",
                        query.SourceReliability.Select(_ =>
                            SolRHelper<IndexedDocument>.GetSolRName(_ => _.ReliabilityScore) + ":" + (int)_))
                );
            
            if (query.Tags != null && query.Tags.Any())
                facetQueries.Add("(" + string.Join(") AND (", query.Tags.GroupBy(_ => _.FacetId).Select(groupTag =>
                    string.Join(" OR ",
                        groupTag.Select(_ =>
                            SolRHelper<IndexedDocument>.GetSolRName(_ => _.TagsId) + ":" + _.FacetId + "/" + _.TagId +
                            ""))
                )) + ")");
            
            if (query.SelectedClassifications != null && query.SelectedClassifications.Any())
                facetQueries.Add(
                    string.Join(" OR ",
                        query.SelectedClassifications.Select(_ =>
                            SolRHelper<IndexedDocument>.GetSolRName(_ => _.Classification) + ":" + _.ClassificationId))
                );
            
            facetQueries.Add(
                // No EYES ONLY specified.
                "(*:* NOT " + SolRHelper<IndexedDocument>.GetSolRName(_ => _.EyesOnly) + ":*)"
                // Or the user belongs to the group.
                + ((user.Memberships?.Any() ?? false) ? 
                    " OR " 
                    + string.Join(" OR ", user.Memberships.Select(_ => SolRHelper<IndexedDocument>.GetSolRName(_ => _.EyesOnly) + ":"  + _.GroupId))
                : "")
            );

            // If a user is not member of the default groups, the documents must be releasable to the user.
            // A document might be released to extra groups
            if (user.Memberships.Any())
            {
                string membershipQuery;
                
                // User is member of some default groups
                if (defaultGroup.Length > 0 && !user.Memberships.Any(_ => defaultGroup?.Contains(_.GroupId) ?? true))
                    membershipQuery = "(*:* " + SolRHelper<IndexedDocument>.GetSolRName(_ => _.EyesOnly) +
                                      ":[* TO *]) AND ";
                else // User is not member of any default group
                    membershipQuery = "(*:* NOT " + SolRHelper<IndexedDocument>.GetSolRName(_ => _.EyesOnly) +
                                      ":[* TO *]) OR ";

                membershipQuery += "(" +
                                   string.Join(" OR ", user.Memberships.Select(_ => SolRHelper<IndexedDocument>.GetSolRName(_ => _.ReleasableTo) + ":"  + _.GroupId))
                                   + ")";
                _logger.LogDebug("Membership FacetQuery: " + membershipQuery);
                facetQueries.Add(membershipQuery);
            }
            
            if (query.SelectedRegistrants != null && query.SelectedRegistrants.Any())
                facetQueries.Add(
                    string.Join(" OR ",
                        query.SelectedRegistrants.Select(_ =>
                            SolRHelper<IndexedDocument>.GetSolRName(_ => _.RegisteredById) + ":" + _.Id))
                );
            
            var facetQuery = "";
            if (facetQueries.Any())
                facetQuery = "(" + string.Join(") AND (", facetQueries) + ")";
            return facetQuery;
        }
    }
}