/* DocIntel
 * Copyright (C) 2018-2021 Belgian Defense, Antoine Cailliau
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using DocIntel.Core.Exceptions;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Settings;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json.Linq;

namespace DocIntel.Core.Scrapers
{
    public abstract class DefaultScraper : IScraper
    {
        private readonly ILogger<DefaultScraper> _logger;
        private readonly ApplicationSettings _settings;
        
        protected IDocumentRepository _documentRepository;
        protected ITagFacetRepository _facetRepository;
        protected IServiceProvider _serviceProvider;
        protected ISourceRepository _sourceRepository;
        protected ITagRepository _tagRepository;
        protected IGroupRepository _groupRepository;
        protected IClassificationRepository _classificationRepository;

        protected DefaultScraper(IServiceProvider serviceProvider)
        {
            _settings = (ApplicationSettings) serviceProvider.GetService(typeof(ApplicationSettings));
            _logger = (ILogger<DefaultScraper>) serviceProvider.GetService(typeof(ILogger<DefaultScraper>));
            _serviceProvider = serviceProvider;
        }

        protected void Init()
        {
            _documentRepository = (IDocumentRepository) _serviceProvider.GetService(typeof(IDocumentRepository));
            _sourceRepository = (ISourceRepository) _serviceProvider.GetService(typeof(ISourceRepository));
            _groupRepository = (IGroupRepository) _serviceProvider.GetService(typeof(IGroupRepository));
            _tagRepository = (ITagRepository) _serviceProvider.GetService(typeof(ITagRepository));
            _facetRepository = (ITagFacetRepository) _serviceProvider.GetService(typeof(ITagFacetRepository));
            _classificationRepository = _serviceProvider.GetRequiredService<IClassificationRepository>();
        }

        public ScraperInformation Get()
        {
            var attribute = GetType().GetCustomAttribute<ScraperAttribute>();
            if (attribute == null)
                throw new Exception("Classes extending DefaultScraper must have the attribute Scraper.");

            return new ScraperInformation
            {
                Id = attribute.Identifier,
                Name = attribute.Name,
                Description = attribute.Description
            };
        }

        public Scraper Install()
        {
            var attribute = GetType().GetCustomAttribute<ScraperAttribute>();
            if (attribute == null)
                throw new Exception("Classes extending DefaultScraper must have the attribute Scraper.");

            return new Scraper
            {
                ReferenceClass = attribute.Identifier,
                Name = attribute.Name,
                Description = attribute.Description
            };
        }

        public abstract Task<bool> Scrape(SubmittedDocument message);

        public IEnumerable<string> Patterns
        {
            get
            {
                var attribute = GetType().GetCustomAttribute<ScraperAttribute>();
                if (attribute == null)
                    throw new Exception("Classes extending DefaultScraper must have the attribute Scraper.");
                return attribute.Patterns;
            }
        }

        protected AmbientContext GetContext(string registeredBy = null)
        {
            var userClaimsPrincipalFactory =
                (IUserClaimsPrincipalFactory<AppUser>) _serviceProvider.GetService(
                    typeof(IUserClaimsPrincipalFactory<AppUser>));
            if (userClaimsPrincipalFactory == null) throw new ArgumentNullException(nameof(userClaimsPrincipalFactory));

            var options =
                (DbContextOptions<DocIntelContext>) _serviceProvider.GetService(
                    typeof(DbContextOptions<DocIntelContext>));
            var context = new DocIntelContext(options,
                (ILogger<DocIntelContext>) _serviceProvider.GetService(typeof(ILogger<DocIntelContext>)));

            // TODO Move automation username to configuration
            var automationUser = !string.IsNullOrEmpty(registeredBy)
                ? context.Users.AsNoTracking().FirstOrDefault(_ => _.Id == registeredBy)
                : context.Users.AsNoTracking().FirstOrDefault(_ => _.UserName == "automation");
            if (automationUser == null)
                return null;

            var claims = userClaimsPrincipalFactory.CreateAsync(automationUser).Result;
            return new AmbientContext
            {
                DatabaseContext = context,
                Claims = claims,
                CurrentUser = automationUser
            };
        }

        protected async Task<Document> AddAsync(Scraper scraper, AmbientContext context, Document document,
            SubmittedDocument submission,
            string[] tags = null)
        {
            if (scraper.OverrideSource) document.SourceId = scraper.SourceId;
            document.MetaData ??= new JObject();
            document.MetaData["ScrapePriority"] = submission.Priority;

            if (scraper.OverrideClassification && scraper.ClassificationId != null)
            {
                document.ClassificationId = (Guid) scraper.ClassificationId;
            }
            else if (submission.OverrideClassification && submission.ClassificationId != null) 
            {
                document.ClassificationId = (Guid) submission.ClassificationId;
            }
            else
            {
                if (_classificationRepository.GetDefault(context) != null)
                    document.ClassificationId = (Guid) _classificationRepository.GetDefault(context).ClassificationId;
                else
                    throw new Exception("Default Classification is not defined");
            }

            if (scraper.OverrideReleasableTo)
                document.ReleasableTo = await _groupRepository.GetAllAsync(context,
                    new GroupQuery {Id = scraper.ReleasableTo.Select(_ => _.GroupId).ToArray()}).ToListAsync();
            else if (submission.OverrideReleasableTo)
                document.ReleasableTo = await _groupRepository.GetAllAsync(context,
                    new GroupQuery {Id = submission.ReleasableTo.Select(_ => _.GroupId).ToArray()}).ToListAsync();

            if (scraper.OverrideEyesOnly)
                document.EyesOnly = await _groupRepository.GetAllAsync(context,
                    new GroupQuery {Id = scraper.EyesOnly.Select(_ => _.GroupId).ToArray()}).ToListAsync();
            else if (submission.OverrideEyesOnly)
                document.EyesOnly = await _groupRepository.GetAllAsync(context,
                    new GroupQuery {Id = submission.EyesOnly.Select(_ => _.GroupId).ToArray()}).ToListAsync();

            document.Status = scraper.SkipInbox ? DocumentStatus.Registered : DocumentStatus.Submitted;

            var tagInstances = tags == null ? Enumerable.Empty<Tag>() : GetTags(scraper, context, tags);
            return await _documentRepository.AddAsync(context, document, tagInstances.ToArray());
        }

        protected async Task<DocumentFile> AddAsync(Scraper scraper, AmbientContext context, DocumentFile documentFile,
            Stream stream)
        {
            return await _documentRepository.AddFile(context, documentFile, stream);
        }

        private IEnumerable<Tag> GetTags(Scraper scraper, AmbientContext context, IEnumerable<string> attributes)
        {
            var facetCache = new Dictionary<string, TagFacet>();
            var tagCache = new Dictionary<string, Tag>();

            var tags = attributes as string[] ?? attributes.ToArray();
            List<string> finalTags;
            if (scraper.ImportRuleSets != null)
            {
                finalTags = new List<string>();
                for (var index = 0; index < tags.Length; index++)
                {
                    var originalTag = tags[index];
                    foreach (var ruleSet in scraper.ImportRuleSets.OrderBy(_ => _.Position))
                        if (ruleSet.ImportRuleSet?.ImportRules != null)
                            foreach (var rule in ruleSet.ImportRuleSet.ImportRules.OrderBy(_ => _.Position))
                            {
                                var regex = new Regex(rule.SearchPattern);
                                tags[index] = regex.Replace(tags[index], rule.Replacement);
                                _logger.LogWarning("Rewrite " + originalTag + " to " + tags[index]);
                            }

                    var collection = tags[index].Split(',',
                        StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    finalTags.AddRange(collection);
                }
            }
            else
            {
                finalTags = tags.ToList();
            }

            return finalTags.Where(_ => !string.IsNullOrEmpty(_))
                .Distinct()
                .Select(_ => CreateOrAddTag(context, _, tagCache, facetCache).Result);
        }

        // TODO This code is probably all over the place. To be refactor and provided in a central location.
        private async Task<Tag> CreateOrAddTag(AmbientContext context, string label, Dictionary<string, Tag> tagCache,
            Dictionary<string, TagFacet> facetCache)
        {
            if (tagCache.ContainsKey(label))
                return tagCache[label];

            var facetPrefix = "";
            var tagName = label;
            if (tagName.IndexOf(':') > 0)
            {
                facetPrefix = label.Split(':', 2)[0];
                tagName = label.Split(':', 2)[1];
            }

            TagFacet facet;
            if (facetCache.ContainsKey(facetPrefix))
            {
                facet = facetCache[facetPrefix];
            }
            else
            {
                try
                {
                    facet = await _facetRepository.GetAsync(context, facetPrefix);
                }
                catch (NotFoundEntityException)
                {
                    _logger.LogWarning("Creating new facet " + facetPrefix);
                    facet = await _facetRepository.AddAsync(context,
                        new TagFacet {Prefix = facetPrefix, Title = facetPrefix});
                }

                facetCache.Add(facet.Prefix, facet);
            }

            Tag tag;
            try
            {
                tag = await _tagRepository.GetAsync(context, facet.FacetId, tagName);
            }
            catch (NotFoundEntityException)
            {
                _logger.LogWarning("Creating new tag " + tagName + " for facet " + facet.Prefix);
                tag = await _tagRepository.CreateAsync(context, new Tag {FacetId = facet.FacetId, Label = tagName});
            }

            tagCache.Add(label, tag);
            return tag;
        }
    }
}