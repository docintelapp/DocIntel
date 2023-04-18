using System;
using System.Text.Json.Nodes;

namespace DocIntel.Core.Collectors;

public class FileImport
{
    public string Filename { get; set; }
    public string Title { get; set; }
    public string MimeType { get; set; }
    public DateTime DocumentDate { get; set; }
    public string SourceUrl { get; set; }
    
    public IFileContent? Content { get; set; }
    
    public JsonObject MetaData { get; set; }
    public bool Visible { get; set; }
    public bool Preview { get; set; }
}