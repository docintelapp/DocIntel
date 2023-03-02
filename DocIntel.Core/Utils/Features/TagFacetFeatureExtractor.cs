using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DocIntel.Core.Models;
using Microsoft.Extensions.Logging;

namespace DocIntel.Core.Utils.Features;

public class TagFacetFeatureExtractor : IFeatureExtractor
{
    private readonly TagFacet _facet;
    private readonly ILogger<TagFacetFeatureExtractor> _logger;

    public TagFacetFeatureExtractor(TagFacet facet, ILogger<TagFacetFeatureExtractor> logger)
    {
        _facet = facet;
        _logger = logger;
    }

    public IEnumerable<string> Extract(string text)
    {
        // _logger.LogDebug("Extract on text " + text);
        var tagCandidates = new HashSet<string>();
        _logger.LogTrace("ExtractionRegex: " + _facet.ExtractionRegex);
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
                _logger.LogDebug($"Could not extract tags for facet '{_facet.Title}' ({e.Message})");
            }
        }

        _logger.LogTrace("AutoExtract: " + _facet.AutoExtract);
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
                {
                    tagCandidates.Add(tag.Label);
                    _logger.LogTrace($"Found {tag.Label} in the text with pattern {pattern}");
                }
            }
        }

        return tagCandidates;
    }
}