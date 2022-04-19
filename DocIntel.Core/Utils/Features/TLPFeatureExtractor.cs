using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DocIntel.Core.Utils.Features;

public class TLPFeatureExtractor : IFeatureExtractor
{
    public IEnumerable<string> Extract(string text)
    {
        var pattern = @"tlp[\:/_\s\-]+(red|green|white|amber(\-strict)?)";
        var patternMatches = Regex
            .Matches(text, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace)
            .Select(_ => _.Groups[1].ToString().ToLowerInvariant().Trim())
            .Distinct();
        return patternMatches;
    }
}