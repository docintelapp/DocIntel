using System.Collections.Generic;
using DocIntel.Core.Models;

namespace DocIntel.WebApp.Views.User;

public class DeleteAPIKeyViewModel
{
    public AppUser User { get; set; }
    public APIKey Key { get; set; }
}