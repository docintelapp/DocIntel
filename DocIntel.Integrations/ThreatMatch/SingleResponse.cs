using Newtonsoft.Json;

namespace DocIntel.Integrations.ThreatMatch
{
    public class SingleResponse<T>
    {
        [JsonProperty("list")] public T List { get; set; }
        [JsonProperty("data")] public T Data { get; set; }
        [JsonProperty("countThisPage")] public int CountThisPage { get; set; }
    }
}