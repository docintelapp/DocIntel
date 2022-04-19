using System.Collections.Generic;

namespace DocIntel.Core.Utils.Search.Tags;

public class TagFacetSearchResults
{
    public TagFacetSearchResults()
    {
        TotalHits = 0;
        Hits = new List<TagFacetSearchHit>();
    }

    public long TotalHits { get; internal set; }
    public List<TagFacetSearchHit> Hits { get; internal set; }
}