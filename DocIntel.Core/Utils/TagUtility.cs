/* DocIntel
 * Copyright (C) 2018-2023 Belgian Defense, Antoine Cailliau
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using Microsoft.EntityFrameworkCore;

namespace DocIntel.Core.Utils
{
    public class TagUtility
    {
        private readonly ITagRepository _tagRepository;
        private readonly ITagFacetRepository _facetRepository;

        public TagUtility(ITagRepository tagRepository, ITagFacetRepository facetRepository)
        {
            _tagRepository = tagRepository;
            _facetRepository = facetRepository;
        }

        public IEnumerable<Tag> GetOrCreateTags(AmbientContext ambientContext, IEnumerable<string> labels, 
            HashSet<Tag> tagCache = null, HashSet<TagFacet> facetCache = null)
        {
            tagCache ??= new HashSet<Tag>();
            facetCache ??= new HashSet<TagFacet>();

            var regexes = new List<Tuple<Regex,string>>();
            // Compile all the regexes for the rewrite
            foreach (var ruleSet in ambientContext.DatabaseContext.ImportRuleSets.Include(_ => _.ImportRules)
                         .Where(_ => _.ImportRules.Any())
                         .OrderBy(_ => _.Position))
                regexes.AddRange(ruleSet.ImportRules
                    .OrderBy(_ => _.Position)
                    .Select(rule => new Tuple<Regex,string>(new Regex(rule.SearchPattern),rule.Replacement))
                    );

            return labels.Distinct()
                .SelectMany(_ => GetOrCreateTag(ambientContext,
                        _,
                        tagCache,
                        facetCache,
                        regexes)
                    .ToListAsync().Result)
                .Where(_ => _ != null)
                .DistinctBy(_ => _.TagId);
        }
        
        private string[] Rewrite (string tag, List<Tuple<Regex,string>> regexes)
        {
            string rt = tag;
            foreach (var regex in regexes)
                rt = regex.Item1.Replace(rt, regex.Item2);

            return rt.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        }

        private async IAsyncEnumerable<Tag> GetOrCreateTag(AmbientContext ambientContext,
            string label,
            HashSet<Tag> tagCache,
            HashSet<TagFacet> facetCache,
            List<Tuple<Regex,string>> regexes)
        {
            var labels = Rewrite(label, regexes);
            foreach (var l in labels)
            {
                var splittedLabel = l.Split(':', 2);
                if (splittedLabel.Length != 2)
                    continue;
            
                var facetPrefix = splittedLabel[0];
                var tagLabel = splittedLabel[1];
                var f = await GetOrCreateFacet(ambientContext, facetPrefix, facetCache);
                var t = await GetOrCreateTag(ambientContext, f, tagLabel, tagCache);
                yield return t;
            }
        }

        private async Task<Tag> GetOrCreateTag(AmbientContext ambientContext, TagFacet facet, string label, HashSet<Tag> cache)
        {
            Tag cached;
            if ((cached = cache.FirstOrDefault(_ => _.FacetId == facet.FacetId & _.Label.ToUpper() == label.ToUpper())) != null)
            {
                return cached;
            }

            var retrievedTag = _tagRepository.GetAllAsync(ambientContext, new TagQuery()
            {
                FacetId = facet.FacetId,
                Label = label
            }).ToEnumerable().SingleOrDefault();
            
            if (retrievedTag == null)
            {
                retrievedTag = await _tagRepository.CreateAsync(ambientContext, new Tag
                {
                    Label = label,
                    Facet = facet,
                    FacetId = facet.FacetId
                });
            }
            
            cache.Add(retrievedTag);
            return retrievedTag;
        }

        private async Task<TagFacet> GetOrCreateFacet(AmbientContext ambientContext, string prefix, HashSet<TagFacet> cache)
        {
            TagFacet cached;
            if ((cached = cache.FirstOrDefault(_ => _.Prefix == prefix)) != null)
            {
                return cached;
            }
            
            var retrievedFacet = await _facetRepository.GetAllAsync(ambientContext, new FacetQuery
            {
                Prefix = prefix
            }).SingleOrDefaultAsync();

            if (retrievedFacet == null)
            {
                retrievedFacet = await _facetRepository.AddAsync(ambientContext, new TagFacet
                {
                    Title = prefix,
                    Prefix = prefix
                });
            }

            cache.Add(retrievedFacet);
            return retrievedFacet;
        }
    }
}