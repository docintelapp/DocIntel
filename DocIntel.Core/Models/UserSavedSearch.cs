using System;

namespace DocIntel.Core.Models;

public class UserSavedSearch
{
    public SavedSearch SavedSearch { get; set; }
    public Guid SavedSearchId { get; set; }

    public AppUser User { get; set; }
    public string UserId { get; set; }

    public bool Default { get; set; }

    public bool Notify { get; set; }
    public DateTime LastNotification { get; set; }
    public TimeSpan NotificationSpan { get; set; }
}