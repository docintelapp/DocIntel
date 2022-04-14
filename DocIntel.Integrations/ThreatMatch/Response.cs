using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace DocIntel.Integrations.ThreatMatch
{
    public class Response<T>
    {
        [JsonProperty("date_applied")] public DateTime DateApplied { get; set; }
        [JsonProperty("list")] public IEnumerable<T> List { get; set; }
        [JsonProperty("data")] public IEnumerable<T> Data { get; set; }
    }
}