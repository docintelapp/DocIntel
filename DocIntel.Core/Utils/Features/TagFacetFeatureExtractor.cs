using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DocIntel.Core.Models;

namespace DocIntel.Core.Utils.Features;

public class TagFacetFeatureExtractor : IFeatureExtractor
{
    private readonly TagFacet _facet;

    public TagFacetFeatureExtractor(TagFacet facet)
    {
        _facet = facet;
    }

    public IEnumerable<string> Extract(string text)
    {
        var tagCandidates = new HashSet<string>();
        if (!string.IsNullOrWhiteSpace(_facet.ExtractionRegex))
        {
            try
            {
                var pattern = _facet.ExtractionRegex;
                var patternMatches = Regex
                    .Matches(text, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace)
                    .Select(_ => _.Groups[0].ToString().Trim())
                    .Distinct();
                foreach (var match in patternMatches)
                {
                    tagCandidates.Add(match);
                }
            }
            catch (Exception e)
            {
                // TODO Log exception
            }
        }

        if (_facet.AutoExtract)
        {
            foreach (var tag in _facet.Tags)
            {
                var kw = new HashSet<string>();
            
                // Match on tag label
                kw.Add(tag.Label.Trim().ToLower());
            
                // Add the extraction keywords to the possibilities
                if (!string.IsNullOrWhiteSpace(tag.ExtractionKeywords))
                    foreach (var ekw in tag.ExtractionKeywords.Split(",").Select(_ => _.Trim().ToLower()))
                        kw.Add(ekw);
            
                // Create the regex, execute and add the tag label if successful.
                var pattern = @"[^\w]+(" + string.Join("|", kw.Select(Regex.Escape)) + @")[^\w]+";
                var patternMatches = Regex
                    .Match(text, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
                if (patternMatches.Success)
                    tagCandidates.Add(tag.Label);
            }
        }

        return tagCandidates;
    }
}