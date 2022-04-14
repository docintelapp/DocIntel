using System.Collections.Generic;

using Newtonsoft.Json;

namespace DocIntel.Integrations.ThreatMatch
{
    public class ReportMetadata
    {
        [JsonProperty("types")] public Response<ReportType> Types { get; set; }
        [JsonProperty("sectors")]  public IEnumerable<Sector> Sectors { get; set; }
    }
}