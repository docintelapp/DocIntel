using System;

namespace DocIntel.Core.Models;

public class Member
{
    public Guid MemberId { get; set; }
    public Guid GroupId { get; set; }
    public Group Group { get; set; }
    public string UserId { get; set; }
    public AppUser User { get; set; }
}