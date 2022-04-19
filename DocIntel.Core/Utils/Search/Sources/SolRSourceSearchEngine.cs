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
using DocIntel.Core.Utils.Indexation.SolR;
using DocIntel.Core.Utils.Search.Documents;

using Microsoft.Extensions.Logging;

using SolrNet;
using SolrNet.Commands.Parameters;
using SolrNet.Exceptions;

namespace DocIntel.Core.Utils.Search.Sources
{
    public class SolRSourceSearchEngine : ISourceSearchService
    {
        private readonly ILogger<SolRSourceSearchEngine> _logger;
        private readonly ISolrOperations<IndexedSource> solr;

        public SolRSourceSearchEngine(
            ISolrOperations<IndexedSource> solr,
            ILogger<SolRSourceSearchEngine> logger)
        {
            this.solr = solr;
            _logger = logger;
        }

        public SourceSearchResults Search(SourceSearchQuery query)
        {
            try
            {
                ISolrQuery q;
                if (!string.IsNullOrEmpty(query.SearchTerms))
                {
                    q = new SolrQuery(query.SearchTerms);
                    _logger.LogDebug("Query search terms: " + query.SearchTerms);
                }
                else
                {
                    q = SolrQuery.All;
                    _logger.LogDebug("Query all");
                }

                var weights =
                    $"{SolRHelper<IndexedSource>.GetSolRName(_ => _.Title)}^20 {SolRHelper<IndexedSource>.GetSolRName(_ => _.Description)}^10 {SolRHelper<IndexedSource>.GetSolRName(_ => _.Keywords)}^5";

                var sortOrder = new List<SortOrder>();
                _logger.LogDebug("Sorting source by " + query.SortCriteria);
                if (query.SortCriteria == SourceSortCriteria.Title)
                    sortOrder.Add(new SortOrder(SolRHelper<IndexedSource>.GetSolRName(_ => _.TitleOrder),
                        Order.ASC));
                else if (query.SortCriteria == SourceSortCriteria.LastUpdate)
                    sortOrder.Add(new SortOrder(SolRHelper<IndexedSource>.GetSolRName(_ => _.LastDocumentDate),
                        Order.DESC));
                else if (query.SortCriteria == SourceSortCriteria.DocumentsCount)
                    sortOrder.Add(new SortOrder(SolRHelper<IndexedSource>.GetSolRName(_ => _.NumDocs),
                        Order.DESC));
                sortOrder.Add(SortOrder.Parse("score desc"));
                _logger.LogDebug("Sort parameter = " + string.Join(",",sortOrder.Select(_ => _.ToString())));

                var facetQuery = BuildFacetQuery(query);
                _logger.LogDebug("Facet Query = " + facetQuery);
                
                var results = solr.Query(q, new QueryOptions
                {
                    StartOrCursor = new StartOrCursor.Start((query.Page - 1) * query.PageSize),
                    Rows = query.PageSize,
                    Highlight = new HighlightingParameters
                    {
                        Fields = new[]
                        {
                            SolRHelper<IndexedSource>.GetSolRName(_ => _.Title),
                            SolRHelper<IndexedSource>.GetSolRName(_ => _.Description)
                        }
                    },
                    Facet = new FacetParameters
                    {
                        Queries = new[]
                        {
                            new SolrFacetFieldQuery(SolRHelper<IndexedSource>.GetSolRName(_ => _.ReliabilityScore))
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
                        {"hl.simple.post", "</span>"}
                    }
                });

                _logger.LogDebug($"Found {results.NumFound} source(s) vs {results.Count}.");

                var sr = new SourceSearchResults();
                sr.TotalHits = results.NumFound;
                int position = 0;
                foreach (var r in results)
                {
                    _logger.LogDebug($"Found source: {r.SourceId} ({r.Title})");
                    var highlightedSnippets = results.Highlights[r.SourceId.ToString()];
                    var item = new SourceSearchHit
                    {
                        Position = position++,
                        Source = r
                    };

                    if (highlightedSnippets.ContainsKey(SolRHelper<IndexedSource>.GetSolRName(_ => _.Title)))
                        item.TitleExcerpt = highlightedSnippets[SolRHelper<IndexedSource>.GetSolRName(_ => _.Title)]
                            .FirstOrDefault();

                    if (highlightedSnippets.ContainsKey(SolRHelper<IndexedSource>.GetSolRName(_ => _.Description)))
                        item.Excerpt = string.Join(" ",
                            highlightedSnippets[SolRHelper<IndexedSource>.GetSolRName(_ => _.Description)].Take(3));

                    sr.Hits.Add(item);
                }
                
                var facetReliability = new List<VerticalResult<SourceReliability>>();
                foreach (var facet in results.FacetFields
                    [SolRHelper<IndexedSource>.GetSolRName(_ => _.ReliabilityScore)])
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

        public SourceSearchResults Suggest(SourceSearchQuery query)
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
                    _logger.LogDebug("Query all");
                }

                var queryOptions = new QueryOptions();
                queryOptions.RequestHandler = new RequestHandlerParameters("/suggest");
                
                var results = solr.Query(q, queryOptions);

                var sr = new SourceSearchResults() {TotalHits = results.NumFound};
                foreach (var r in results)
                {
                    sr.Hits.Add(new SourceSearchHit()
                    {
                        Source = r
                    });
                }

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

        private string BuildFacetQuery(SourceSearchQuery query)
        {
            var queryString = new List<string>();
            
            if (query.SelectedReliabilities != null && query.SelectedReliabilities.Any())
            {
                queryString.Add(string.Join(" OR ", query.SelectedReliabilities.Select(reliability =>
                {
                    var val = ((int) reliability).ToString();
                    return SolRHelper<IndexedSource>.GetSolRName(_ => _.ReliabilityScore) + ":" + val;
                })));
            }

            if (queryString.Any())
                return "(" + string.Join(") AND (", queryString)  + ")";
            return "";
        }
    }
}