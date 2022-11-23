using System;
using System.Text.Json.Serialization;
using Json.Schema.Generation;

namespace DocIntel.Core.Importers;

public class RssSourceImporterMetaData
{
    [JsonPropertyName("enabled")]
    [Title("Scrape RSS feed?")]
    [Description("Enabling this feature requires scrapers to be setup, check the documentation.")]
    [Required]
    public bool Enabled { get; set; }

    [JsonPropertyName("keywords")]
    [Title("RSS Keywords")]
    [Description("The scraper will only ingest documents that contains at least one of the specified keyword.")]
    [Required]
    public string Keywords { get; set; }
}

public class RssSourceImporterFullMetaData : RssSourceImporterMetaData
{
    [JsonPropertyName("last_pull")]
    public DateTime? LastPull { get; set; }
}