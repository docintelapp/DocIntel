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

namespace DocIntel.Core.Utils.Search.Tags;

public class SolrFacetSearchEngine : IFacetSearchService
{
    private ILogger<SolrFacetSearchEngine> _logger;
    private readonly ISolrOperations<IndexedTagFacet> _solr;

    public SolrFacetSearchEngine(ILogger<SolrFacetSearchEngine> logger, ISolrOperations<IndexedTagFacet> solr)
    {
        _logger = logger;
        _solr = solr;
    }
        
    public TagFacetSearchResults Search(TagFacetSearchQuery query)
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

            var weights = string.Join(' ', $"{SolRHelper<IndexedTagFacet>.GetSolRName(_ => _.Title)}^20",
                $"{SolRHelper<IndexedTagFacet>.GetSolRName(_ => _.Prefix)}^15",
                $"{SolRHelper<IndexedTagFacet>.GetSolRName(_ => _.Description)}^10");

            _logger.LogDebug("Weights: " + weights);
            _logger.LogDebug("Page: " + query.Page);
            _logger.LogDebug("Page: " + query.PageSize);

            var results = _solr.Query(q, new QueryOptions
            {
                StartOrCursor = new StartOrCursor.Start((query.Page - 1) * query.PageSize),
                Rows = query.PageSize,
                Highlight = new HighlightingParameters
                {
                    Fields = new[]
                    {
                        SolRHelper<IndexedTagFacet>.GetSolRName(_ => _.Title),
                        SolRHelper<IndexedTagFacet>.GetSolRName(_ => _.Description)
                    }
                },
                ExtraParams = new Dictionary<string, string>
                {
                    {"qf", weights},
                    {"defType", "edismax"},
                    {"hl.fragsize", "250"},
                    {"hl.simple.pre", "<span class='bg-warning-50'>"},
                    {"hl.simple.post", "</span>"}
                }
            });

            _logger.LogDebug($"Found {results.NumFound} facet(s) vs {results.Count}.");

            var sr = new TagFacetSearchResults();
            sr.TotalHits = results.NumFound;
            foreach (var r in results)
            {
                var highlightedSnippets = results.Highlights[r.FacetId.ToString()];
                var item = new TagFacetSearchHit
                {
                    Facet = r
                };

                if (highlightedSnippets.ContainsKey(SolRHelper<IndexedTagFacet>.GetSolRName(_ => _.Title)))
                    item.TitleExcerpt = highlightedSnippets[SolRHelper<IndexedTagFacet>.GetSolRName(_ => _.Title)]
                        .FirstOrDefault();

                if (highlightedSnippets.ContainsKey(SolRHelper<IndexedTagFacet>.GetSolRName(_ => _.Description)))
                    item.Excerpt = string.Join(" ",
                        highlightedSnippets[SolRHelper<IndexedTagFacet>.GetSolRName(_ => _.Description)].Take(3));

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

    public TagFacetSearchResults Suggest(TagFacetSearchQuery query)
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
                
            var results = _solr.Query(q, queryOptions);

            var sr = new TagFacetSearchResults {TotalHits = results.NumFound};
            foreach (var r in results)
            {
                sr.Hits.Add(new TagFacetSearchHit
                {
                    Facet = r
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