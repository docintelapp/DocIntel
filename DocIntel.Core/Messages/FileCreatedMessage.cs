using System;

namespace DocIntel.Core.Messages;

public class FileCreatedMessage
{
    public Guid FileId { get; init; }
    public string UserId { get; set; }
}