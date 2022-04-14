// 

using System;
using System.Linq;

namespace DocIntel.Core.Helpers
{
    public static class UriHelper
    {
        public static string GetLastPart(this Uri address)
        {
            return address?.AbsolutePath.Split('/').LastOrDefault();
        }
    }
}