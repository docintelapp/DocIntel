using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DocIntel.Core.Utils.Features;

public class ActorFeatureExtractor : IFeatureExtractor
{
    public IEnumerable<string> Extract(string text)
    {
        var pattern = @"\s((apt|ta|unc|dev)[ \-]*[0-9]+)[^\w]+";
        var patternMatches = Regex
            .Matches(text, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace)
            .Select(_ => _.Groups[1].ToString().Trim())
            .Distinct();
        return patternMatches;
    }
}