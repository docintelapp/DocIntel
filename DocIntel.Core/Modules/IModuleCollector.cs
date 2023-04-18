using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DocIntel.Core.Importers;
using DocIntel.Core.Models;
using DocIntel.Core.Scrapers;
using Json.Schema;

namespace DocIntel.Core.Modules;

public interface IModuleScraper
{
    bool HasSettings { get; }
    IEnumerable<string> Patterns { get; }
    ScraperInformation Get();
    Task<bool> Scrape(SubmittedDocument message);
    JsonSchema GetSettingsSchema();
    Type GetSettingsType();
    string GetSettingsView();
}

public interface IModuleImporter
{
    bool HasSettings { get; }
    ImporterInformation Get();
    JsonSchema GetSettingsSchema();
    Type GetSettingsType();
    string GetSettingsView();
}

public interface IModuleImporter<T> : IModuleImporter
{
    IAsyncEnumerable<SubmittedDocument> PullAsync(DateTime? lastPull, int limit, T settings = default);
}