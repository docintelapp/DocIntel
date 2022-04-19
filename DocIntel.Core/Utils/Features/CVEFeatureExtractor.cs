using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DocIntel.Core.Utils.Features;

public class CVEFeatureExtractor : IFeatureExtractor
{
    public IEnumerable<string> Extract(string text)
    {
        var pattern = @"CVE-\d{4}-\d{4,8}";
        var patternMatches = Regex
            .Matches(text, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace)
            .Select(_ => _.Groups[0].ToString().ToUpperInvariant().Trim())
            .Distinct();
        return patternMatches;
    }
}