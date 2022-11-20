using System;

namespace DocIntel.Core.Messages;

public class FileDeletedMessage
{
    public Guid FileId { get; init; }
    public string UserId { get; set; }
}