using System.Text.Json.Serialization;
using Json.Schema.Generation;

namespace DocIntel.Core.Utils;

public class ExtractionMetaData
{
    [JsonPropertyName("structured_data")]
    [Title("Automatically extract structured data?")]
    [Description("This will automatically extract structured data for documents from the source.")]
    [Required]
    public bool StructuredData { get; set; } = true;
}