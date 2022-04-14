// 

using DocIntel.Core.Models;

namespace DocIntel.Core.EmailViews.Actions
{
    public class ConfirmEmailModel
    {
        public AppUser User { get; internal set; }
        public string Callback { get; internal set; }
    }
}