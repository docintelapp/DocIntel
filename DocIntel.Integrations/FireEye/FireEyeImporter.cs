// using System;
// using System.Collections.Generic;
// using System.Globalization;
// using System.IO;
// using System.Text.Json;
// using CommandLine;
// using DocIntel.Core.Models;
// using DocIntel.Core.Repositories;
// using Ganss.Xss;
// using Microsoft.Extensions.Logging;
// using System.Linq;
// using System.Threading.Tasks;
// using DocIntel.Core.Models;
// using BlushingPenguin.JsonPath;
// using System.Net;
// using AngleSharp.Html.Dom;
// using DocIntel.Core.Settings;
// using System.Net.Http;
// using System.Text.RegularExpressions;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.AspNetCore.Identity;
// using DocIntel.Core.Exceptions;

// namespace DocIntel.Services.Importer
// {
//     public class FireEyeImporter
//     {
//         protected readonly ILogger<FireEyeImporter> _logger;
//         protected readonly ILogger<DocIntelContext> _contextLogger;

//         protected readonly ISourceRepository _sourceRepository;
//         // protected readonly ITagRepository _tagRepository;
//         protected readonly IUserRepository _userRepository;
//         // protected readonly IDocumentRepository _documentRepository;
//         protected readonly IIncomingFeedRepository _pluginRepository;

//         protected readonly ApplicationSettings _settings;
//         protected readonly Guid _pluginId;
//         protected string proxyString;
//         protected Source source;
//         protected AppUser user;

//         protected readonly IUserClaimsPrincipalFactory<AppUser> _userClaimsPrincipalFactory;

//         protected IncomingFeed _plugin;

//         protected readonly IServiceProvider _serviceProvider;

//         public FireEyeImporter(ILogger<FireEyeImporter> logger,
//                                ISourceRepository sourceRepository,
//                                IUserRepository userRepository,
//                                IIncomingFeedRepository pluginRepository,
//                                ApplicationSettings settings,
//                                Guid pluginId,
//                                IServiceProvider serviceProvider, 
//                                IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory, 
//                                ILogger<DocIntelContext> contextLogger)
//         {
//             _logger = logger;
//             _sourceRepository = sourceRepository;
//             // _documentRepository = documentRepository;
//             // _tagRepository = tagRepository;
//             _userRepository = userRepository;
//             _pluginRepository = pluginRepository;
//             _settings = settings;
//             _pluginId = pluginId;
//             _serviceProvider = serviceProvider;
//             _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
//             _contextLogger = contextLogger;
//         }

//         public async Task<bool> InitAsync() {
//             var options = (DbContextOptions<DocIntelContext>) _serviceProvider.GetService(typeof(DbContextOptions<DocIntelContext>));
//             var context = new DocIntelContext(options, _contextLogger);
                
//             var automationUser = Context.Users.AsNoTracking().FirstOrDefault(_ => _.UserName == "automation");
//             if (automationUser == null)
//                 return false;

//             var claims = _userClaimsPrincipalFactory.CreateAsync(automationUser).Result;
//             var ambientContext = new AmbientContext {
//                 DatabaseContext = context,
//                 Claims = claims,
//                 CurrentUser = automationUser
//             };

//             try {
//                 source = await _sourceRepository.GetAsync(ambientContext, "FireEye");
                
//             } catch (NotFoundEntityException) {
//                 _logger.LogDebug("Created source");
//                 source = await _sourceRepository.CreateAsync(ambientContext, 
//                     new Source {
//                         Title = "FireEye"
//                     });
//             }

//             try {
//                 _plugin = await _pluginRepository.GetAsync(ambientContext, _pluginId, 
//                     includeRelatedData: new string [] { "ImportRuleSets", "ImportRuleSets.ImportRuleSet", "ImportRuleSets.ImportRuleSet.ImportRules" });

//             } catch (NotFoundEntityException) {
//                 var plugin = new IncomingFeed () {
//                     IncomingFeedId = Program.PluginId,
//                     Name = "FireEye Importer",
//                     Description = "Imports the reports from FireEye subscription",
//                     Enabled = false,
//                 };
//                 _plugin = await _pluginRepository.CreateAsync(ambientContext,
//                     plugin);
//             }
            
//             await ambientContext.DatabaseContext.SaveChangesAsync();

//             if (_plugin.Settings != null)
//                 if (_plugin.Settings.RootElement.TryGetProperty("proxy", out JsonElement jsonElement))
//                     proxyString = jsonElement.GetString();

//             return true;
//         }

//         protected async Task<Document> ImportReportAsync(AppUser user, Source source, JsonDocument jsonResult, Stream pdfStream)
//         {
//             var options = (DbContextOptions<DocIntelContext>) _serviceProvider.GetService(typeof(DbContextOptions<DocIntelContext>));
//             var context = new DocIntelContext(options, _contextLogger);
                
//             var automationUser = Context.Users.AsNoTracking().FirstOrDefault(_ => _.UserName == "automation");
//             if (automationUser == null)
//                 return null;

//             var claims = _userClaimsPrincipalFactory.CreateAsync(automationUser).Result;
//             var ambientContext = new AmbientContext {
//                 DatabaseContext = context,
//                 Claims = claims,
//                 CurrentUser = automationUser
//             };
                
//             var _documentRepository = (IDocumentRepository) _serviceProvider.GetService(typeof(IDocumentRepository));
//             var _tagRepository = (ITagRepository) _serviceProvider.GetService(typeof(ITagRepository));
//             var _facetRepository = (ITagFacetRepository) _serviceProvider.GetService(typeof(ITagFacetRepository));
        
//             string externalReference = GetExternalReference(jsonResult);
//             HashSet<Tag> tags = await GetTags(ambientContext, jsonResult, _tagRepository, _facetRepository);

//             Document document;
//             try {
//                 document = await _documentRepository.GetAsync(ambientContext, new DocumentQuery {
//                     Source = source, ExternalReference = externalReference
//                 });
//                 document.Title = GetTitle(jsonResult);
//                 document.ShortDescription = GetSummary(jsonResult);
//                 document.Note = await GetNotes(ambientContext, jsonResult, source, _tagRepository, _documentRepository);
//                 document.Status = DocumentStatus.Registered;
                
//                 document = await _documentRepository.UpdateAsync(ambientContext, document, tags.ToHashSet(), pdfStream);
//                 _logger.LogDebug(document.URL);
                
//             } catch (NotFoundEntityException) {
//                 var doc = new Document
//                 {
//                     Title = GetTitle(jsonResult),
//                     ExternalReference = externalReference,
//                     ShortDescription = GetSummary(jsonResult),
//                     DocumentDate = GetPublicationDate(jsonResult),
//                     SourceId = source.SourceId,
//                     Classification = Classification.Restricted,
//                     Status = DocumentStatus.Registered,
//                     Note = await GetNotes(ambientContext, jsonResult, source, _tagRepository, _documentRepository)
//                 };

//                 try {
//                     document = await _documentRepository.AddAsync(ambientContext, doc, pdfStream, tags.ToArray());

//                 } catch (FileAlreadyKnownException e) {
//                     _logger.LogWarning("File already known.");
//                     return null;
//                 }
//             }

//             await ambientContext.DatabaseContext.SaveChangesAsync();

//             return document;
//         }


//         protected static string GetExternalReference(JsonDocument jsonResult)
//         {
//             return jsonResult.SelectToken("$.message.report.reportId")?.GetString();
//         }

//         protected static string GetTitle(JsonDocument jsonResult)
//         {
//             return jsonResult.SelectToken("$.message.report.title")?.GetString();
//         }

//         protected static string GetSummary(JsonDocument jsonResult)
//         {
//             var summary = jsonResult.SelectToken("$.message.report.execSummary")?.GetString();

//             if (string.IsNullOrEmpty(summary))
//                 summary = jsonResult.SelectToken("$.message.report.threatDescription")?.GetString();

//             var sanitizer = new HtmlSanitizer();
//             sanitizer.AllowedTags.Clear();
//             sanitizer.KeepChildNodes = true;
//             summary = sanitizer.Sanitize(summary);
//             return summary;
//         }

//         protected DateTime GetPublicationDate(JsonDocument jsonResult)
//         {
//             CultureInfo enUS = new CultureInfo("en-US");
//             var publishedDateElement = jsonResult.SelectToken("$.message.report.publishDate")?.GetString();
//             DateTime dateValue;
//             if (DateTime.TryParseExact(publishedDateElement, "MMMM dd, yyyy hh:mm:ss tt", enUS, DateTimeStyles.None, out dateValue))
//             {
//                 _logger.LogInformation(dateValue.ToString());
//             }
//             else
//             {
//                 _logger.LogError("Could not parse date " + publishedDateElement);
//                 dateValue = DateTime.Now;
//             }

//             return dateValue;
//         }

//         protected async Task<HashSet<Tag>> GetTags(AmbientContext ambientContext, JsonDocument jsonResult, ITagRepository _tagRepository, ITagFacetRepository _facetRepository)
//         {
//             var tags = new HashSet<string>();
//             GetAudienceTags(jsonResult, tags);
//             GetProductTags(jsonResult, tags);
//             GetReportTypeTags(jsonResult, tags);
//             GetMainTags(jsonResult, tags);
//             GetRelationTags(jsonResult, tags);
//             HashSet<Tag> tagCollection = await FilterTags(ambientContext, tags, _tagRepository, _facetRepository);
//             return tagCollection;
//         }

//         protected async Task<HashSet<Tag>> FilterTags(AmbientContext ambientContext, HashSet<string> tags, ITagRepository _tagRepository, ITagFacetRepository _facetRepository)
//         {
//             var tagCollection = new HashSet<Tag>();
//             var facetCache = new Dictionary<string, TagFacet>();
//             foreach (var tag in tags)
//             {
//                 var resultingTag = tag;
//                 if (_plugin.ImportRuleSets != null) {
//                     foreach (var ruleSet in _plugin.ImportRuleSets.OrderBy(_ => _.Position))
//                     {
//                         if (ruleSet.ImportRuleSet.ImportRules != null) {
//                             foreach (var rule in ruleSet.ImportRuleSet.ImportRules.OrderBy(_ => _.Position))
//                             {
//                                 var regex = new Regex(rule.SearchPattern);
//                                 resultingTag = regex.Replace(resultingTag, rule.Replacement);
//                             }
//                         }
//                     }
//                 }

//                 if (!string.IsNullOrEmpty(resultingTag))
//                 {
//                     foreach (var splittedTag in resultingTag.Split(','))
//                     {
//                         if (!string.IsNullOrEmpty(splittedTag.Trim())) {
//                             tagCollection.Add(await CreateOrAddTag(facetCache, ambientContext, splittedTag.Trim(), _tagRepository, _facetRepository));
//                             _logger.LogDebug("Tag: " + splittedTag);
//                         } else {
//                             _logger.LogDebug("Ignoring " + splittedTag);
//                         }
//                     }
//                 } else {
//                     _logger.LogDebug("Ignoring " + tag);
//                 }
//             }

//             return tagCollection;
//         }

//         protected void GetRelationTags(JsonDocument jsonResult, HashSet<string> tags)
//         {
//             var relationSections = jsonResult.SelectToken("$.message.report.relations")?.EnumerateObject();
//             if (relationSections != null)
//             {
//                 foreach (var relationSection in relationSections)
//                 {
//                     foreach (var relation in relationSection.Value.EnumerateArray())
//                     {
//                         if (relation.ValueKind == JsonValueKind.String)
//                         {
//                             _logger.LogInformation("Detected Relation: " + relationSection.Name + ":" + relation.GetString());
//                             tags.Add(relationSection.Name + ":" + relation.GetString());
//                         }
//                         else if (relation.ValueKind == JsonValueKind.Object)
//                         {
//                             if (relation.TryGetProperty("name", out var tagName))
//                             {
//                                 _logger.LogInformation("Detected Relation: " + relationSection.Name + ":" + tagName);
//                                 tags.Add(relationSection.Name + ":" + tagName.GetString());
//                             }
//                             else
//                             {
//                                 _logger.LogInformation("Ignore Relation: (" + relationSection.Name + ") " + relation.ValueKind);
//                             }
//                         }
//                         else
//                         {
//                             _logger.LogInformation("Ignore Relation: (" + relationSection.Name + ")");
//                         }
//                     }
//                 }
//             }
//         }

//         protected void GetMainTags(JsonDocument jsonResult, HashSet<string> tags)
//         {
//             var tagSections = jsonResult.SelectToken("$.message.report.tagSection.main")?.EnumerateObject();
//             if (tagSections != null)
//             {
//                 foreach (var tagSection in tagSections)
//                 {
//                     foreach (var subSection in tagSection.Value.EnumerateObject())
//                     {
//                         foreach (var tag in subSection.Value.EnumerateArray())
//                         {
//                             if (tag.ValueKind == JsonValueKind.String)
//                             {
//                                 _logger.LogInformation("Detected Tag: " + subSection.Name + ":" + tag.GetString());
//                                 tags.Add(subSection.Name + ":" + tag.GetString());
//                             }
//                             else if (tag.ValueKind == JsonValueKind.Object)
//                             {
//                                 if (tag.TryGetProperty("name", out var tagName))
//                                 {
//                                     _logger.LogInformation("Detected Tag: " + subSection.Name + ":" + tagName);
//                                     tags.Add(subSection.Name + ":" + tagName.GetString());
//                                 }
//                                 else
//                                     _logger.LogInformation("Ignore Tag: (" + subSection.Name + ") " + tag.ValueKind);
//                             }
//                             else
//                             {
//                                 _logger.LogInformation("Ignore Tag: (" + subSection.Name + ")");
//                             }
//                         }
//                     }
//                 }
//             }
//         }

//         protected static void GetReportTypeTags(JsonDocument jsonResult, HashSet<string> tags)
//         {
//             var reportType = jsonResult.SelectToken("$.message.report.reportType")?.GetString();
//             if (!string.IsNullOrEmpty(reportType))
//             {
//                 tags.Add("reportType:" + reportType);
//             }
//         }

//         protected static void GetProductTags(JsonDocument jsonResult, HashSet<string> tags)
//         {
//             var products = jsonResult.SelectToken("$.message.report.ThreatScape.product")?.EnumerateArray();
//             foreach (var product in products)
//             {
//                 tags.Add("product:" + product.GetString());
//             }
//         }

//         protected static void GetAudienceTags(JsonDocument jsonResult, HashSet<string> tags)
//         {
//             var audiences = jsonResult.SelectToken("$.message.report.audience")?.EnumerateArray();
//             foreach (var audience in audiences)
//             {
//                 tags.Add("audience:" + audience.GetString());
//             }
//         }

//         protected async Task<string> GetNotes(AmbientContext ambientContext, JsonDocument jsonResult, Source source, ITagRepository _tagRepository, IDocumentRepository _documentRepository)
//         {
//             var notes = jsonResult.SelectToken("$.message.report.threatDetail")?.GetString();

//             var notesSanitizer = new HtmlSanitizer();
//             notesSanitizer.AllowedAttributes.Remove("bgcolor");
//             notesSanitizer.AllowedAttributes.Remove("style");
//             notesSanitizer.AllowedAttributes.Remove("alt");
//             notesSanitizer.AllowedCssProperties.Clear();
//             notesSanitizer.PostProcessNode += (s, e) =>
//             {
//                 if (e.Node?.NodeValue?.Trim() == string.Empty)
//                 {
//                     e.Node.ParentElement.RemoveChild(e.Node);
//                 }
//                 (e.Node as IHtmlAnchorElement)?.SetAttribute("target", "_blank");
                
//                 if (e.Node is IHtmlAnchorElement a) 
//                 {
//                     var href = a.GetAttribute("href");
//                     if (href != null) {
//                         _logger.LogDebug("HREF: " + href);
//                         if (href.StartsWith("https://attack.mitre.org/techniques/"))
//                         {
//                             var id = href.Replace("https://attack.mitre.org/techniques/", "").Trim('/');
//                             var tags = _tagRepository.GetAllAsync(ambientContext, new TagQuery { StartsWith = id });
//                             if (tags.CountAsync().Result > 0) {
//                                 a.SetAttribute("href", _settings.ApplicationBaseURL + "/Tag/Details/" + WebUtility.UrlDecode((tags.FirstAsync().Result).FriendlyName));
//                             }
//                         }

//                         if (href.StartsWith("https://intelligence.fireeye.com/reports/"))
//                         {
//                             var id = href.Replace("https://intelligence.fireeye.com/reports/", "").Trim('/');
//                             var query = new DocumentQuery {
//                                     Source = source, ExternalReference = id
//                                 };
                                
//                             try {
//                                 if (_documentRepository.ExistsAsync(ambientContext, query).Result) {
//                                     var document = _documentRepository.GetAsync(ambientContext, query).Result;
//                                     a.SetAttribute("href", _settings.ApplicationBaseURL + "/Document/Details/" + WebUtility.UrlDecode(document.Reference));
//                                 }

//                             } catch (NotFoundEntityException) {

//                             }
//                         }

//                         if (href.StartsWith("https://fireeye.satmetrix.com/")) {
//                             e.Node.ParentElement.RemoveChild(e.Node);
//                         }
//                     }
//                 }

//                 if (e.Node is IHtmlImageElement img) {
//                     var src = img.GetAttribute("src"); 
//                     if (src != null && src.StartsWith("http")) {
//                         var base64 = GetImageAsBase64Url(src).Result; // Why is await not working?
//                         if (!string.IsNullOrEmpty(base64)) {
//                             img.SetAttribute("src", base64);
//                             _logger.LogDebug("Download and embed image for " + src);
//                             // _logger.LogDebug(img.GetAttribute("src"));
//                         } else {
//                             _logger.LogWarning("Ignoring image " + src + " (could not download).");
//                         }
//                     } else {
//                         _logger.LogWarning("Ignoring image " + src);
//                     }
//                 }
//             };

//             return notesSanitizer.Sanitize(notes);
//         }

//         protected async Task<string> GetImageAsBase64Url(string url)
//         {
//             var proxy = new WebProxy(proxyString);

//             using (var httpClientHandler = new HttpClientHandler { Proxy = proxy })
//             using (var client = new HttpClient(httpClientHandler))
//             {
//                 try {
//                     var bytes = await client.GetByteArrayAsync(url);
//                     if (url.EndsWith("png"))
//                         return "data:image/png;base64," + Convert.ToBase64String(bytes);
//                     else if (url.EndsWith("jpeg") | url.EndsWith("jpg"))
//                         return "data:image/jpeg;base64," + Convert.ToBase64String(bytes);
//                     else if (url.EndsWith("gif") | url.EndsWith("jpg"))
//                         return "data:image/gif;base64," + Convert.ToBase64String(bytes);

//                 } catch (HttpRequestException e) {
//                     _logger.LogWarning("Could not download " + url + " (" + e.Message + ")");
//                 }
//                 return "";
//             }
//         }
        
//         private async Task<Tag> CreateOrAddTag(Dictionary<string, TagFacet> facetCache, AmbientContext ambientContext, string label, ITagRepository _tagRepository, ITagFacetRepository _facetRepository)
//         {
//             var facetName = "";
//             var tagName = label;
//             if (tagName.IndexOf(':') > 0)
//             {
//                 facetName = label.Split(':', 2)[0];
//                 tagName = label.Split(':', 2)[1];
//             }

//             TagFacet facet;
//             if (facetCache.ContainsKey(facetName)) {
//                 facet = facetCache[facetName];
//             } else {
//                 try
//                 {
//                     facet = await _facetRepository.GetAsync(ambientContext, facetName);
//                     facetCache[facet.Prefix] = facet;
//                 }
//                 catch (NotFoundEntityException)
//                 {
//                     facet = await _facetRepository.AddAsync(ambientContext, new TagFacet { Prefix = facetName, Title = facetName });
//                     facetCache[facet.Prefix] = facet;
//                 }
//             }
            
//             Tag tag;
//             try
//             {
//                 tag = await _tagRepository.GetAsync(ambientContext, facet.Id, tagName);

//             }
//             catch (NotFoundEntityException)
//             {
//                 tag = await _tagRepository.CreateAsync(ambientContext, new Tag { FacetId = facet.Id, Label = tagName });
//             }

//             return tag;
//         }
//     }
// }
