using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DocIntel.Core.Utils.Features;

public class AttckTechniqueFeatureExtractor : IFeatureExtractor
{
    public IEnumerable<string> Extract(string text)
    {
        var pattern = @"(T[0-9]{4}(\.[0-9]{3})?)";
        var patternMatches = Regex.Matches(text, pattern)
            .Select(_ => _.ToString().ToUpper())
            .Distinct();
        return patternMatches;
    }
}