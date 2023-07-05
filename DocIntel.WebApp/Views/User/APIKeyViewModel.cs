using System.Collections.Generic;
using DocIntel.Core.Models;

namespace DocIntel.WebApp.Views.User;

public class APIKeyViewModel
{
    public AppUser User { get; set; }
    public IEnumerable<APIKey> Keys { get; set; }
}