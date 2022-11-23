using System.Collections.Generic;
using System.Text.Json.Serialization;
using DocIntel.Core.Models;
using DocIntel.Core.Utils.Search.Sources;
using Newtonsoft.Json;

namespace DocIntel.WebApp.Areas.API.Models;

public class ApiTagSearchQuery
{
    [JsonPropertyName("search_term")]
    public string SearchTerms { get; set; }
    public int Page { get; set; } = 1;
    [JsonPropertyName("page_size")]
    public int PageSize { get; set; } = 25;
}

public class ApiFacetSearchQuery
{
    [JsonPropertyName("search_term")]
    public string SearchTerms { get; set; }
    public int Page { get; set; } = 1;
    [JsonPropertyName("page_size")]
    public int PageSize { get; set; } = 25;
}

public class ApiSourceSearchQuery
{
    [JsonPropertyName("search_term")]
    public string SearchTerms { get; set; }
    [JsonPropertyName("sort")]
    public SourceSortCriteria SortCriteria { get; set; }
    public int Page { get; set; } = 1;
    [JsonPropertyName("page_size")]
    public int PageSize { get; set; } = 25;
    public SourceReliability[] Reliabilities { get; set; }
}