/* DocIntel
 * Copyright (C) 2018-2023 Belgian Defense, Antoine Cailliau, Kevin Menten
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using DocIntel.Core.Utils.Indexation.SolR;
using DocIntel.Core.Utils.Search.Documents;
using Microsoft.Extensions.Logging;
using SolrNet;
using SolrNet.Commands.Parameters;
using SolrNet.Exceptions;

namespace DocIntel.Core.Utils.Search.Tags
{
    public class SolRTagSearchEngine : ITagSearchService
    {
        private readonly ILogger<SolRTagSearchEngine> _logger;
        private readonly ISolrOperations<IndexedTag> _solr;

        public SolRTagSearchEngine(
            ISolrOperations<IndexedTag> solr,
            ILogger<SolRTagSearchEngine> logger)
        {
            _solr = solr;
            _logger = logger;
        }

        public TagSearchResults Search(TagSearchQuery query)
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

                var weights = string.Join(' ', $"{SolRHelper<IndexedTag>.GetSolRName(_ => _.Label)}^20",
                    $"{SolRHelper<IndexedTag>.GetSolRName(_ => _.Keywords)}^15",
                    $"{SolRHelper<IndexedTag>.GetSolRName(_ => _.FacetPrefix)}^10",
                    $"{SolRHelper<IndexedTag>.GetSolRName(_ => _.FacetTitle)}^10",
                    $"{SolRHelper<IndexedTag>.GetSolRName(_ => _.Description)}^5",
                    $"{SolRHelper<IndexedTag>.GetSolRName(_ => _.FacetDescription)}");

                _logger.LogDebug("Weights: " + weights);
                _logger.LogDebug("Page: " + query.Page);
                _logger.LogDebug("Page: " + query.PageSize);
                
                var extraParams = new List<KeyValuePair<string,string>>
                {
                    new ("qf", weights),
                    new ("defType", "edismax"),
                    new ("hl.fragsize", "250"),
                    new ("hl.simple.pre", "<span class='bg-warning-50'>"),
                    new ("hl.simple.post", "</span>"),
                    new("bf", "recip(ms(NOW,last_doc_update),3.16e-11,1,1)")
                };

                var facetQuery = BuildFacetQuery(query);
                foreach (var fq in facetQuery)
                {
                    _logger.LogDebug("fq:" + fq);
                    extraParams.Add(new KeyValuePair<string, string>("fq", fq));
                }
                
                var results = _solr.Query(q, new QueryOptions
                {
                    StartOrCursor = new StartOrCursor.Start((query.Page - 1) * query.PageSize),
                    Rows = query.PageSize,
                    Highlight = new HighlightingParameters
                    {
                        Fields = new[]
                        {
                            SolRHelper<IndexedTag>.GetSolRName(_ => _.Label),
                            SolRHelper<IndexedTag>.GetSolRName(_ => _.Keywords),
                            SolRHelper<IndexedTag>.GetSolRName(_ => _.Description)
                        }
                    },
                    ExtraParams = extraParams
                });

                _logger.LogDebug($"Found {results.NumFound} tags(s) vs {results.Count}.");

                var sr = new TagSearchResults {TotalHits = results.NumFound};
                foreach (var r in results)
                {
                    var highlightedSnippets = results.Highlights[r.TagId.ToString()];
                    var item = new TagSearchHit
                    {
                        Tag = r
                    };

                    if (highlightedSnippets.ContainsKey(SolRHelper<IndexedTag>.GetSolRName(_ => _.Label)))
                        item.LabelExcerpt = highlightedSnippets[SolRHelper<IndexedTag>.GetSolRName(_ => _.Label)]
                            .FirstOrDefault();

                    if (highlightedSnippets.ContainsKey(SolRHelper<IndexedTag>.GetSolRName(_ => _.Description)))
                        item.Excerpt = string.Join(" ",
                            highlightedSnippets[SolRHelper<IndexedTag>.GetSolRName(_ => _.Description)].Take(3));

                    sr.Hits.Add(item);
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

        private List<string> BuildFacetQuery(TagSearchQuery query)
        {
            var facetQueries = new List<string>();
            
            if (!string.IsNullOrEmpty(query.FacetPrefix))
                facetQueries.Add(
                    SolRHelper<IndexedTag>.GetSolRName(_ => _.FacetPrefix) + ":" + query.FacetPrefix
                );

            return facetQueries;
        }

        public TagSearchResults Suggest(TagSearchQuery query)
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
                
                var extraParams = new List<KeyValuePair<string,string>>
                {
                    new("bf", "recip(ms(NOW,last_doc_update),3.16e-11,1,1)")
                };

                var facetQuery = BuildFacetQuery(query);
                foreach (var fq in facetQuery)
                {
                    _logger.LogDebug("fq:" + fq);
                    extraParams.Add(new KeyValuePair<string, string>("fq", fq));
                }
                
                var queryOptions = new QueryOptions()
                {
                    ExtraParams = extraParams
                };
                queryOptions.RequestHandler = new RequestHandlerParameters("/suggest");
                
                var results = _solr.Query(q, queryOptions);

                var sr = new TagSearchResults {TotalHits = results.NumFound};
                foreach (var r in results)
                {
                    sr.Hits.Add(new TagSearchHit
                    {
                        Tag = r
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
    }
}