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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

using AutoMapper;

using DocIntel.Core.Helpers;
using DocIntel.Core.Models;
using DocIntel.Core.Models.STIX;
using DocIntel.Core.Models.ThreatQuotient;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.ContentExtraction;
using DocIntel.Core.Utils.Indexation.SolR;
using DocIntel.Core.Utils.Search.Documents;

using Ganss.XSS;


using Microsoft.Extensions.Logging;

using SolrNet;

using BaseObject = DocIntel.Core.Models.ThreatQuotient.BaseObject;
using Relationship = DocIntel.Core.Models.Relationship;
using Report = DocIntel.Core.Models.STIX.Report;

namespace DocIntel.Core.Utils.Observables
{
    public class ObservablesUtility : IObservablesUtility
    {
        private readonly ApplicationSettings _appSettings;
        private readonly IDocumentRepository _documentRepository;
        private readonly ILogger<ObservablesUtility> _logger;
        private readonly IMapper _mapper;
        private readonly IObservableRepository _observableRepository;
        private readonly IObservablesExtractionUtility _observablesExtractionUtility;
        private readonly ISolrOperations<IndexedDocument> _solr;
        private readonly IContentExtractionUtility _extractionUtility;

        public ObservablesUtility(ILogger<ObservablesUtility> logger,
            IDocumentRepository documentRepository,
            IObservableRepository observableRepository,
            ApplicationSettings appSettings,
            ISolrOperations<IndexedDocument> solr,
            IObservablesExtractionUtility observablesExtractionUtility, IMapper mapper, IContentExtractionUtility extractionUtility)
        {
            _logger = logger;
            _documentRepository = documentRepository;
            _appSettings = appSettings;
            _solr = solr;
            _observablesExtractionUtility = observablesExtractionUtility;
            _observableRepository = observableRepository;
            _mapper = mapper;
            _extractionUtility = extractionUtility;
        }

        public async Task ExtractObservables(AmbientContext ambientContext,
            Guid documentId,
            bool autoApprove = false)
        {
            try
            {
                var document = await _documentRepository.GetAsync(ambientContext, documentId, new[] {"Files"});

                await _observableRepository.DeleteAllObservables(document.DocumentId);

                foreach (var file in document.Files)
                {
                    var response = _extractionUtility.ExtractText(document, file);
                    if (response != null) await DetectObservables(response, document, file, autoApprove);
                }

                _logger.LogInformation(
                    $"Document {document.Reference} ({document.DocumentId}) observables successfully extracted.");
            }
            catch (Exception e)
            {
                _logger.LogError($"Document {documentId} observables could not be extracted ({e.Message}).");
                _logger.LogError(e.StackTrace);
            }
        }

        public async Task<IEnumerable<DocumentFileObservables>> DetectObservables(string response, Document document,
            DocumentFile file, bool autoApprove = false)
        {
            var observables = await _observablesExtractionUtility.ExtractObservable(response, file);

            var res = _mapper.Map<IEnumerable<DocumentFileObservables>>(observables.ToList());

            res = res.Select(u =>
            {
                u.Status = autoApprove
                    ? RecommendAccepted(u.Type, u.Status, u.History)
                        ? ObservableStatus.Accepted
                        : ObservableStatus.Rejected
                    : u.Status;
                u.DocumentId = document.DocumentId;
                // u.FileId = file.FileId;
                return u;
            }).ToList();

            var result = await _observableRepository.IngestObservables(res);
            _logger.LogDebug("IngestObservables returns " + result);
            
            return res;
        }

        public async Task<bool> DeleteObservable(Guid observableId)
        {
            return await _observableRepository.DeleteObservable(observableId);
        }

        public async Task<IEnumerable<Relationship>> DetectRelations(IEnumerable<DocumentFileObservables> dfoList,
            Document document)
        {
            // remove empty tags that haven't been saved yet (cve/vulnerability/tlp)
            var documentTags = document.DocumentTags.Where(u => u.Tag is not null).ToArray();
            var relations = new List<Relationship>();
            var actors = documentTags.Where(u => u.Tag.Facet.getStixObjectNameFromFacet() == "actor");
            var malware = documentTags.Where(u => u.Tag.Facet.getStixObjectNameFromFacet() == "malware");
            var campaign = documentTags.Where(u => u.Tag.Facet.getStixObjectNameFromFacet() == "campaign");
            if (actors.Count() == 1) relations.AddRange(CreateRelations(dfoList, actors.First().TagId));

            if (malware.Count() == 1) relations.AddRange(CreateRelations(dfoList, malware.First().TagId));

            if (campaign.Count() == 1) relations.AddRange(CreateRelations(dfoList, campaign.First().TagId));

            var result = await _observableRepository.ReplaceRelationships(document.DocumentId, relations);
            return relations;
        }

        public async Task<IEnumerable<DocumentFileObservables>> GetDocumentObservables(Guid documentId,
            ObservableStatus? status = null)
        {
            return await _observableRepository.GetObservables(documentId, status);
        }

        public async Task<IEnumerable<DocumentFileObservables>> GetDocumentObservables(string value,
            ObservableType observableType)
        {
            return await _observableRepository.SearchObservables(value, observableType);
        }

        public async Task<IEnumerable<DocumentFileObservables>> ExportObservables(ObservableType ot)
        {
            return await _observableRepository.ExportDocumentObservables(ot);
        }

        public bool RecommendAccepted(ObservableType type, ObservableStatus status, ObservableStatus history)
        {
            if ((type == ObservableType.IPv4 || type == ObservableType.File || type == ObservableType.FQDN) &&
                status == ObservableStatus.AutomaticallyAccepted && history != ObservableStatus.Rejected)
                return true;
            if (history == ObservableStatus.Accepted && (status == ObservableStatus.Review) |
                (status == ObservableStatus.AutomaticallyAccepted))
                return true;
            if (status == ObservableStatus.Accepted)
                return true;

            return false;
        }

        public async Task<Bundle> CreateStixBundle(AmbientContext ambientContext, Guid documentId)
        {
            var rl = await _observableRepository.GetRelationships(documentId);

            var document = await _documentRepository.GetAsync(
                ambientContext,
                documentId,
                new[]
                {
                    nameof(Document.DocumentTags),
                    nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag),
                    nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag) + "." + nameof(Tag.Facet),
                    nameof(Document.Source),
                    nameof(Document.Comments)
                }
            );
            var objects = new List<Models.STIX.BaseObject>();

            var b = new Bundle {Id = "bundle--" + Guid.NewGuid()};

            //TODO : label

            var sanitizer = new HtmlSanitizer();
            var sanitizedHtml = sanitizer.Sanitize(document.ShortDescription).Replace("\\\"", "\"");

            var r = new Report
            {
                Id = "report--" + documentId, Created = document.RegistrationDate,
                Modified = document.ModificationDate, Name = document.Title, Published = document.DocumentDate,
                Description = sanitizedHtml,
                ExternalReferences = new List<ExternalReference> {new() {SourceName = "url", URL = document.URL}}
            };

            var actors = document.DocumentTags.Where(u => u.Tag.Facet.getStixObjectNameFromFacet() == "actor");
            foreach (var a in actors)
            {
                var i = CreateStixIntrusionSet(a.Tag);
                objects.Add(i);
            }

            var malware = document.DocumentTags.Where(u => u.Tag.Facet.getStixObjectNameFromFacet() == "malware");
            foreach (var a in malware)
            {
                var i = CreateStixMalware(a.Tag);
                objects.Add(i);
            }

            var campaign = document.DocumentTags.Where(u => u.Tag.Facet.getStixObjectNameFromFacet() == "campaign");
            foreach (var a in campaign)
            {
                var i = CreateStixCampaign(a.Tag);
                objects.Add(i);
            }

            /*
            var observedData = await CreateStixObservedData(documentId, document);
            if (observedData != null)
                objects.Add(observedData);
            */

            var observedUrl = CreateStixExternalUrl(documentId, document);
            objects.Add(observedUrl);

            var indicators = await CreateStixIndicator(documentId, document);
            objects.AddRange(indicators);

            r.ObjectRefs = objects.Select(u => u.Id).ToList();

            objects.Add(r);

            foreach (var re in rl)
            {
                var i = CreateStixRelationship(re, actors, campaign, malware);
                objects.Add(i);
            }

            b.Objects = objects;
            return b;
        }

        public async Task<ExportObject> CreateTqExport(AmbientContext ambientContext, int pageSize, int page,
            DateTime dateFrom, DateTime dateTo)
        {
            var c = new ArrayList();
            var pendingDocuments = _documentRepository.GetAllAsync(ambientContext, new DocumentQuery
                {
                    ModifiedAfter = dateFrom,
                    ModifiedBefore = dateTo,
                    Page = page,
                    Limit = pageSize,
                    OrderBy = SortCriteria.ModificationDate
                },
                new[] {nameof(Document.RegisteredBy), nameof(Document.LastModifiedBy)}).ToEnumerable();
            foreach (var d in pendingDocuments)
            {
                var t = await CreateTqExportDocument(ambientContext, d.DocumentId);
                c.AddRange(t);
            }

            return new ExportObject {Data = c};
        }

        public async Task<ArrayList> CreateTqExportDocument(AmbientContext ambientContext,
            Guid documentId)
        {
            var rl = await _observableRepository.GetRelationships(documentId);

            //var tags = _tagRepository.GetAllAsync(ambientContext, new TagQuery(), null);

            var document = await _documentRepository.GetAsync(
                ambientContext,
                documentId,
                new[]
                {
                    nameof(Document.DocumentTags),
                    nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag),
                    nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag) + "." + nameof(Tag.Facet),
                    nameof(Document.Source),
                    nameof(Document.Comments),
                    nameof(Document.Files)
                }
            );

            var objects_relations = new ArrayList();

            //TODO : label
            var sanitizer = new HtmlSanitizer();
            var sanitizedHtml = sanitizer.Sanitize(document.ShortDescription).Replace("\\\"", "\"");

            var observedData = await CreateTQIndicator(documentId, document);

            var d = new List<KeyValuePair<string, string>>();
            d.Add(new KeyValuePair<string, string>("docintel_url",
                new Uri(new Uri(_appSettings.ApplicationBaseURL), "Document/Details/" + document.URL).ToString()));
            d.Add(new KeyValuePair<string, string>("report_url", document.ExternalReference));
            d.Add(new KeyValuePair<string, string>("classification", document.Classification.Title));

            var r = new Models.ThreatQuotient.Report
            {
                Id = documentId, CreatedAt = document.RegistrationDate, // tags = new []{"tag1", "tag2"},
                UpdatedAt = document.ModificationDate, Value = document.Title,
                PublishedAt = document.Files.First().DocumentDate,
                Description = sanitizedHtml, Attributes = d
                //docintel_url = new Uri(new Uri(_appSettings.ApplicationBaseURL), "Document/Details/" + document.URL).ToString(), 
                //report_url = document.ExternalReference
            };
            objects_relations.Add(r);
            
            // TODO Ugly because of hard coding, should be abstracted.
            IEnumerable<DocumentTag> tlpTags = document.DocumentTags.Where(_ => _.Tag.Facet.Title.ToLower() == "tlp");
            var lowerClassificationTitle = document.Classification.Title.ToLower();
            if (tlpTags.Any(_ => _.Tag.Label.ToLower() == "red"))
            {
                r.TLP = "TLP-Red";
            } else if (tlpTags.Any(_ => _.Tag.Label.ToLower() == "amber") 
                    || lowerClassificationTitle.Contains("restricted") 
                    || lowerClassificationTitle.Contains("diffusion restreinte") 
                    || lowerClassificationTitle.Contains("beperkte verspreiding"))
            {
                r.TLP = "TLP-Amber";
            } else if (tlpTags.Any(_ => _.Tag.Label.ToLower() == "green"))
            {
                r.TLP = "TLP-Green";
            } else if (tlpTags.Any(_ => _.Tag.Label.ToLower() == "white"))
            {
                r.TLP = "TLP-White";
            }

            var tagList = new[]
                {"Adversary", "Malware", "Campaign", "Identity", "Vulnerability", "Tool", "Attack Pattern"};
            var addTagList = document.DocumentTags
                .Where(u => tagList.Contains(u.Tag.Facet.getThreatQObjectNameFromFacet())).Select(u =>
                    CreateTQObjectRelations(u.Tag, u.Tag.Facet.getThreatQObjectNameFromFacet()));
            objects_relations.AddRange(addTagList.ToList());
            objects_relations.AddRange(observedData.Select(u => new BaseObjectRelations(u)).ToList());

            foreach (var or in objects_relations)
                CreateTQRelations((BaseObjectRelations) or, document, rl, observedData);
            return objects_relations;
        }

        public IEnumerable<Relationship> CreateRelations(IEnumerable<DocumentFileObservables> dfoList, Guid targetRef)
        {
            var r = dfoList.Select(u => new Relationship
            {
                DocumentRef = u.DocumentId, SourceRef = u.Id, TagId = targetRef
            });
            return r;
        }

        private static IntrusionSet CreateStixIntrusionSet(Tag tag)
        {
            var i = new IntrusionSet
            {
                Created = tag.CreationDate, Modified = tag.ModificationDate,
                Id = "intrusion-set--" + tag.TagId, Name = tag.Label
            };
            return i;
        }

        private static Malware CreateStixMalware(Tag tag)
        {
            var i = new Malware
            {
                Created = tag.CreationDate, Modified = tag.ModificationDate, Id = "malware--" + tag.TagId,
                Name = tag.Label
            };
            return i;
        }

        private static Campaign CreateStixCampaign(Tag tag)
        {
            var i = new Campaign
            {
                Created = tag.CreationDate, Modified = tag.ModificationDate, Id = "campaign--" + tag.TagId,
                Name = tag.Label
            };
            return i;
        }

        private static Models.STIX.Relationship CreateStixRelationship(Relationship r,
            IEnumerable<DocumentTag> actors, IEnumerable<DocumentTag> malware, IEnumerable<DocumentTag> campaigns)
        {
            var prefixDestination = "";
            if (actors.Where(u => u.TagId == r.TagId).Any()) prefixDestination = "intrusion-set--";
            if (malware.Where(u => u.TagId == r.TagId).Any()) prefixDestination = "malware--";
            if (campaigns.Where(u => u.TagId == r.TagId).Any()) prefixDestination = "campaign--";

            var i = new Models.STIX.Relationship
            {
                Created = r.Created, Modified = r.Modified, Id = "relationship--" + r.Id,
                RelationshipType = r.RelationshipType.GetAttributeOfType<EnumMemberAttribute>().Value,
                SourceRef = "observed-data--" + r.SourceRef,
                TargetRef = prefixDestination + r.TagId
            };
            return i;
        }

        private ObservedData CreateStixExternalUrl(Guid documentId, Document document)
        {
            var od = new ObservedData
            {
                Id = "observed-data--" + document.DocumentId, Created = document.RegistrationDate,
                Modified = document.ModificationDate, FirstObserved = document.DocumentDate,
                LastObserved = document.DocumentDate, NumberObserved = 1
            };
            var o = new ObservableObject(ObservableType.URL) {Value = document.SourceUrl};
            od.Objects.Add(od.Objects.Count.ToString(), o);
            od.Labels = new List<string>
                {"misp:type=\"url\"", "misp:category=\"External analysis\"", "misp:to_ids=\"False\""};

            return od;
        }

        private async Task<ObservedData> CreateStixObservedData(Guid documentId, Document document)
        {
            var dfoList = await GetDocumentObservables(documentId, ObservableStatus.Accepted);

            if (dfoList.Any())
            {
                var od = new ObservedData
                {
                    Id = "observed-data--" + document.DocumentId, Created = document.RegistrationDate,
                    Modified = document.ModificationDate, FirstObserved = document.DocumentDate,
                    LastObserved = document.DocumentDate, NumberObserved = 1
                };
                foreach (var dfo in dfoList)
                {
                    var o = new ObservableObject(dfo.Type);
                    switch (dfo.Type)
                    {
                        case ObservableType.File:
                        case ObservableType.Artefact:
                            o.Hashes = new Dictionary<string, string>();
                            foreach (var h in dfo.Hashes)
                                o.Hashes.Add(h.HashType.GetAttributeOfType<EnumMemberAttribute>().Value, h.Value);

                            break;
                        case ObservableType.URL:
                            o.Value = Uri.EscapeUriString(dfo.Value);
                            break;
                        default:
                            o.Value = dfo.Value;
                            break;
                    }

                    od.Objects.Add(od.Objects.Count.ToString(), o);
                }

                return od;
            }

            return null;
        }

        private async Task<List<Indicator>> CreateStixIndicator(Guid documentId, Document document)
        {
            var ret = new List<Indicator>();
            var dfoList = await GetDocumentObservables(documentId, ObservableStatus.Accepted);

            foreach (var dfo in dfoList)
            {
                var o = new Indicator
                {
                    Id = "indicator--" + dfo.Id, Created = document.RegistrationDate,
                    Modified = document.ModificationDate, ValidFrom = document.DocumentDate, Labels =
                        new List<IndicatorLabel>
                        {
                            IndicatorLabel.MaliciousActivity
                        }
                };
                switch (dfo.Type)
                {
                    case ObservableType.File:
                    case ObservableType.Artefact:
                        var l = new List<string>();
                        foreach (var h in dfo.Hashes)
                            l.Add("file:hashes.'" +
                                  h.HashType.GetAttributeOfType<EnumMemberAttribute>().Value + "' = '" +
                                  h.Value + "'");
                        o.Pattern = "[" + string.Join(" OR ", l.ToArray()) + "]";
                        o.PatternType = PatternType.STIX;

                        break;
                    case ObservableType.IPv4:
                        o.Pattern = "[ipv4-addr:value = '" + dfo.Value + "']";
                        o.PatternType = PatternType.STIX;
                        break;
                    case ObservableType.URL:
                        o.Pattern = "[url:value = '" + Uri.EscapeUriString(dfo.Value) + "']";
                        o.PatternType = PatternType.STIX;
                        break;
                    case ObservableType.FQDN:
                        o.Pattern = "[domain-name:value = '" + dfo.Value + "']";
                        o.PatternType = PatternType.STIX;
                        break;
                }

                ret.Add(o);
            }

            return ret;
        }

        private static void CreateTQRelations(BaseObjectRelations or, Document document, IEnumerable<Relationship> rl,
            IEnumerable<BaseObject> observedData)
        {
            var listr = rl.Where(u => u.SourceRef == or.Id).Select(u => u.TagId).ToList()
                .Concat(rl.Where(u => u.TagId == or.Id).Select(u => u.SourceRef).ToList());

            // for report : get all tags and link them to report
            // for other observables, use relations and check if they are still tagged to the document
            var t = or.Type == "Report"
                ? document.DocumentTags
                : document.DocumentTags.Where(u => listr.Contains(u.Tag.TagId));
            var l_malware = t.Where(u => u.Tag.Facet.getThreatQObjectNameFromFacet() == "Malware")
                .Select(u => CreateTQObject(u.Tag, u.Tag.Facet.getThreatQObjectNameFromFacet()));
            var l_identity = t.Where(u => u.Tag.Facet.getThreatQObjectNameFromFacet() == "Identity")
                .Select(u => CreateTQObject(u.Tag, u.Tag.Facet.getThreatQObjectNameFromFacet()));
            var l_vulnerability = t.Where(u => u.Tag.Facet.getThreatQObjectNameFromFacet() == "Vulnerability")
                .Select(u => CreateTQObject(u.Tag, u.Tag.Facet.getThreatQObjectNameFromFacet()));
            var l_attack_pattern = t.Where(u => u.Tag.Facet.getThreatQObjectNameFromFacet() == "Attack Pattern")
                .Select(u => CreateTQObject(u.Tag, u.Tag.Facet.getThreatQObjectNameFromFacet()));
            var l_adversary = t.Where(u => u.Tag.Facet.getThreatQObjectNameFromFacet() == "Adversary")
                .Select(u => CreateTQObject(u.Tag, u.Tag.Facet.getThreatQObjectNameFromFacet()));
            var l_campaign = t.Where(u => u.Tag.Facet.getThreatQObjectNameFromFacet() == "Campaign")
                .Select(u => CreateTQObject(u.Tag, u.Tag.Facet.getThreatQObjectNameFromFacet()));
            var l_indicators = or.Type == "Report" ? observedData : observedData.Where(u => listr.Contains(u.Id));

            or.Adversary = l_adversary.Any() ? l_adversary : null;
            or.Malware = l_malware.Any() ? l_malware : null;
            or.Identity = l_identity.Any() ? l_identity : null;
            or.AttackPattern = l_attack_pattern.Any() ? l_attack_pattern : null;
            or.Campaign = l_campaign.Any() ? l_campaign : null;
            or.Indicator = l_indicators.Any() ? l_indicators : null;
            or.Vulnerability = l_vulnerability.Any() ? l_vulnerability : null;
            if (or.Type != "Report")
                or.Report = new[]
                {
                    new Models.ThreatQuotient.Report
                    {
                        Id = document.DocumentId, CreatedAt = document.RegistrationDate,
                        UpdatedAt = document.ModificationDate, Value = document.Title
                    }
                };
        }

        private static BaseObject CreateTQObject(Tag tag, string type)
        {
            var i = new BaseObject(type)
            {
                CreatedAt = tag.CreationDate, UpdatedAt = tag.ModificationDate, Id = tag.TagId,
                Value = tag.Label, Description = tag.Description.IsNullOrEmpty() ? null : tag.Description
            };
            return i;
        }

        private static BaseObjectRelations CreateTQObjectRelations(Tag tag, string type)
        {
            var i = new BaseObjectRelations(type)
            {
                CreatedAt = tag.CreationDate, UpdatedAt = tag.ModificationDate, Id = tag.TagId,
                Value = tag.Label, Description = tag.Description.IsNullOrEmpty() ? null : tag.Description
            };
            return i;
        }

        private string CreateTQIndicatorType(string original)
        {
            switch (original)
            {
                case "MD5":
                    return "MD5";
                case "SHA-1":
                    return "SHA-1";
                case "SHA-256":
                    return "SHA-256";
                case "SHA-512":
                    return "SHA-512";
                case "ipv4-addr":
                    return "IP Address";
                case "domain-name":
                    return "FQDN";
                case "url":
                    return "URL";
                default:
                    return "";
            }
        }

        private async Task<IEnumerable<BaseObject>> CreateTQIndicator(Guid documentId, Document document)
        {
            var dfoList = await GetDocumentObservables(documentId, ObservableStatus.Accepted);
            var l = new List<BaseObject>();
            foreach (var dfo in dfoList)
            {
                BaseObject o;
                switch (dfo.Type)
                {
                    case ObservableType.File:
                    case ObservableType.Artefact:
                        foreach (var h in dfo.Hashes)
                        {
                            o = new BaseObject(
                                    CreateTQIndicatorType(h.HashType.GetAttributeOfType<EnumMemberAttribute>().Value))
                                {CreatedAt = dfo.RegistrationDate, UpdatedAt = dfo.ModificationDate, Id = dfo.Id};
                            o.Value = h.Value;
                            l.Add(o);
                        }

                        break;
                    case ObservableType.URL:
                        o = new BaseObject(
                                CreateTQIndicatorType(dfo.Type.GetAttributeOfType<EnumMemberAttribute>().Value))
                            {CreatedAt = dfo.RegistrationDate, UpdatedAt = dfo.ModificationDate, Id = dfo.Id};
                        o.Value = Uri.EscapeUriString(dfo.Value);
                        l.Add(o);
                        break;
                    default:
                        o = new BaseObject(
                                CreateTQIndicatorType(dfo.Type.GetAttributeOfType<EnumMemberAttribute>().Value))
                            {CreatedAt = dfo.RegistrationDate, UpdatedAt = dfo.ModificationDate, Id = dfo.Id};
                        o.Value = dfo.Value;
                        l.Add(o);
                        break;
                }
            }

            return l;
        }
    }
}