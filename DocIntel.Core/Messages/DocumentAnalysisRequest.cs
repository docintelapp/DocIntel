using System;

namespace DocIntel.Core.Messages;

public class DocumentAnalysisRequest
{
    public Guid DocumentId { get; init; }
    public string UserId { get; set; }
}