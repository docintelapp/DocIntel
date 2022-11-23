using System.Text.Json.Serialization;
using Json.Schema.Generation;

namespace DocIntel.Core.Utils;

public class RegistrationMetadata
{
    [JsonPropertyName("auto")]
    [Title("Automatically register documents?")]
    [Description("This will automatically register documents from the source, skipping the pending list.")]
    [Required]
    public bool Auto { get; set; }
}