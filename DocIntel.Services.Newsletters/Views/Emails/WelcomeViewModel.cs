using System;
using System.Collections.Generic;
using DocIntel.Core.Models;

namespace DocIntel.Services.Newsletters.Views.Emails
{
    public class WelcomeViewModel
    {
        public DateTime Date { get; set; }
        public IEnumerable<Document> Documents { get; internal set; }
        public AppUser User { get; internal set; }
    }
}