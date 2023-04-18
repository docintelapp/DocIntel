using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace DocIntel.Core.Collectors;

public class DocumentImport
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string SourceName { get; set; }
    public string ExternalReference { get; set; }
    public DateTime? DocumentDate { get; set; }
    public string[] Tags { get; set; }
    public string SourceURL { get; set; }
    public ICollection<FileImport> Files { get; set; }
    public ICollection<NodeImport> Nodes { get; set; }
    public JsonObject MetaData { get; set; }

    public void AddFile(FileImport content)
    {
        Files ??= new List<FileImport>();

        if (content.Content != null)
            Files.Add(content);
    }
}