using System;

namespace DocIntel.Core.Messages;

public class FileUpdatedMessage
{
    public Guid FileId { get; init; }
    public string UserId { get; set; }
}