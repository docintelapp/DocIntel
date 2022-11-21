using System.Collections.Generic;
using DocIntel.Core.Models;
using DocIntel.Core.Utils.Search.Sources;
using Newtonsoft.Json;

namespace DocIntel.WebApp.Areas.API.Models;

public class ApiTagSearchQuery
{
    [JsonProperty("search_term")]
    public string SearchTerms { get; set; }
    public int Page { get; set; } = 1;
    [JsonProperty("page_size")]
    public int PageSize { get; set; } = 25;
}

public class ApiFacetSearchQuery
{
    [JsonProperty("search_term")]
    public string SearchTerms { get; set; }
    public int Page { get; set; } = 1;
    [JsonProperty("page_size")]
    public int PageSize { get; set; } = 25;
}

public class ApiSourceSearchQuery
{
    [JsonProperty("search_term")]
    public string SearchTerms { get; set; }
    [JsonProperty("sort")]
    public SourceSortCriteria SortCriteria { get; set; }
    public int Page { get; set; } = 1;
    [JsonProperty("page_size")]
    public int PageSize { get; set; } = 25;
    public SourceReliability[] Reliabilities { get; set; }
}