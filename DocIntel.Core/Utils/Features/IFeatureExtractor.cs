using System.Collections.Generic;

namespace DocIntel.Core.Utils.Features;

public interface IFeatureExtractor
{
    IEnumerable<string> Extract(string text);
}