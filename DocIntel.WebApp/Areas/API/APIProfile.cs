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
using System.Linq;

using AutoMapper;

using DocIntel.Core.Models;
using DocIntel.Core.Utils.Search.Sources;
using DocIntel.Core.Utils.Search.Tags;
using DocIntel.WebApp.Areas.API.Models;
using RazorLight.Extensions;
using Synsharp;
using Synsharp.Telepath.Messages;

namespace DocIntel.WebApp.Areas.API
{
    public class APIProfile : Profile
    {
        public APIProfile()
        {
            CreateMap<ApiObservableDetails, SynapseNode>()
                .ForMember(_ => _.Form, _ => _.MapFrom(__ => __.Type))
                .ForMember(_ => _.Valu, _ => _.MapFrom(__ => __.Value))
                .ForMember(_ => _.Tags, _ => _.MapFrom(__ => __.Tags.Select(t => new KeyValuePair<string,long?[]>(t, new long?[]{})).ToDictionary(kv => kv.Key, kv => kv.Value)))
                .ForMember(_ => _.Props, _ => _.MapFrom(__ => __.Properties));
            
            CreateMap<Importer, APIImporter>();

            ImportRuleSetProfile();
            ImportRuleProfile();
            RoleProfile();

            CreateMap<Document, ApiDocumentDetails>()
                .ForMember(_ => _.Tags, _ => _.MapFrom(_ => _.DocumentTags.Select(__ => __.Tag)))
                .MaxDepth(2);
            CreateMap<Document, ApiDocument>()
                .ForMember(_ => _.Tags, _ => _.MapFrom(_ => _.DocumentTags.Select(__ => __.Tag)))
                .MaxDepth(2);
            CreateMap<ApiDocument, Document>()
                .ForMember(_ => _.Title, _ =>
                {
                    _.PreCondition(_ => !string.IsNullOrEmpty(_.Title));
                    _.MapFrom(_ => _.Title);
                })
                .ForMember(_ => _.ShortDescription, _ =>
                {
                    _.PreCondition(_ => _.Summary != null);
                    _.MapFrom(_ => _.Summary);
                })
                .ForMember(_ => _.ExternalReference, _ =>
                {
                    _.PreCondition(_ => _.ExternalReference != null);
                    _.MapFrom(_ => _.ExternalReference);
                })
                .ForMember(_ => _.SourceUrl, _ =>
                {
                    _.PreCondition(_ => _.SourceUrl != null);
                    _.MapFrom(_ => _.SourceUrl);
                })
                .ForMember(_ => _.DocumentDate, _ =>
                {
                    _.PreCondition(_ => _.DocumentDate != null
                                        && _.DocumentDate != DateTime.MinValue
                                        && _.DocumentDate != DateTime.MaxValue);
                    _.MapFrom(_ => _.DocumentDate);
                })
                .ForMember(_ => _.Note, _ =>
                {
                    _.PreCondition(_ => _.Note != null);
                    _.MapFrom(_ => _.Note);
                })
                .ForMember(_ => _.SourceId, _ =>
                {
                    _.PreCondition(_ => _.SourceId != null);
                    _.MapFrom(_ => _.SourceId);
                })
                .ForMember(_ => _.ClassificationId, _ =>
                {
                    _.PreCondition(_ => _.ClassificationId != null);
                    _.MapFrom(_ => _.ClassificationId);
                });
            
            CreateMap<DocumentFile, APIFileDetails>()
                .MaxDepth(2);
            CreateMap<DocumentFile, APIFile>()
                .MaxDepth(2);
            CreateMap<APIFile, DocumentFile>()
                .ForMember(_ => _.Title, _ =>
                {
                    _.PreCondition(_ => !string.IsNullOrEmpty(_.Title));
                    _.MapFrom(_ => _.Title);
                })
                .ForMember(_ => _.MimeType, _ =>
                {
                    _.PreCondition(_ => !string.IsNullOrEmpty(_.MimeType));
                    _.MapFrom(_ => _.MimeType);
                })
                .ForMember(_ => _.DocumentDate, _ =>
                {
                    _.PreCondition(_ => _.FileDate != null
                                        && _.FileDate != DateTime.MinValue
                                        && _.FileDate != DateTime.MaxValue);
                    _.MapFrom(_ => _.FileDate);
                })
                .ForMember(_ => _.SourceUrl, _ =>
                {
                    _.PreCondition(_ => !string.IsNullOrEmpty(_.SourceUrl));
                    _.MapFrom(_ => _.SourceUrl);
                })
                .ForMember(_ => _.OverrideClassification, _ =>
                {
                    _.PreCondition(_ => _.OverrideClassification != null);
                    _.MapFrom(_ => _.OverrideClassification);
                })
                .ForMember(_ => _.ClassificationId, _ =>
                {
                    _.PreCondition(_ => _.ClassificationId != null);
                    _.MapFrom(_ => _.ClassificationId);
                })
                .ForMember(_ => _.OverrideReleasableTo, _ =>
                {
                    _.PreCondition(_ => _.OverrideReleasableTo != null);
                    _.MapFrom(_ => _.OverrideReleasableTo);
                })
                .ForMember(_ => _.OverrideEyesOnly, _ =>
                {
                    _.PreCondition(_ => _.OverrideEyesOnly != null);
                    _.MapFrom(_ => _.OverrideEyesOnly);
                })
                .ForMember(_ => _.Visible, _ =>
                {
                    _.PreCondition(_ => _.Visible != null);
                    _.MapFrom(_ => _.Visible);
                })
                .ForMember(_ => _.Preview, _ =>
                {
                    _.PreCondition(_ => _.Preview != null);
                    _.MapFrom(_ => _.Preview);
                })
                .ForMember(_ => _.AutoGenerated, _ =>
                {
                    _.PreCondition(_ => _.AutoGenerated != null);
                    _.MapFrom(_ => _.AutoGenerated);
                });
                
            TagProfile();
            FacetProfile();
            
            CreateMap<UserFacetSubscription, ApiSubscriptionStatus>();
            CreateMap<SubscriptionStatus, ApiSubscriptionStatus>();
            
            CreateMap<Comment, ApiCommentDetails>();
            
            CreateMap<Classification, APIClassificationDetails>();
            CreateMap<Classification, APIClassification>();
            CreateMap<APIClassification, Classification>()
                .ForMember(_ => _.Title, _ =>
                {
                    _.PreCondition(_ => !string.IsNullOrEmpty(_.Title));
                    _.MapFrom(_ => _.Title);
                })
                .ForMember(_ => _.Subtitle, _ =>
                {
                    _.PreCondition(_ => _.Subtitle != null);
                    _.MapFrom(_ => _.Subtitle);
                })
                .ForMember(_ => _.Color, _ =>
                {
                    _.PreCondition(_ => !string.IsNullOrEmpty(_.Color));
                    _.MapFrom(_ => _.Color);
                })
                .ForMember(_ => _.Abbreviation, _ =>
                {
                    _.PreCondition(_ => _.Abbreviation != null);
                    _.MapFrom(_ => _.Abbreviation);
                })
                .ForMember(_ => _.Description, _ =>
                {
                    _.PreCondition(_ => _.Description != null);
                    _.MapFrom(_ => _.Description);
                })
                .ForMember(_ => _.ParentClassificationId, _ =>
                {
                    _.PreCondition(_ => _.ParentClassificationId != null);
                    _.MapFrom(_ => _.ParentClassificationId);
                })
                .ForMember(_ => _.Default, _ =>
                {
                    _.PreCondition(_ => _.Default != null);
                    _.MapFrom(_ => (bool)_.Default);
                });

            CreateMap<AppUser, APIAppUser>()
                .ForMember(_ => _.UserId, _ => _.MapFrom(_ => _.Id))
                .ForMember(_ => _.FriendlyName, _ => _.Ignore());

            SourceProfile();

            CreateMap<SynapseNode, ApiObservableDetails>()
                .ForMember(_ => _.Iden, _ => _.MapFrom(__ => __.Iden))
                .ForMember(_ => _.Type, _ => _.MapFrom(__ => __.Form))
                .ForMember(_ => _.Value, _ => _.MapFrom(__ => __.Valu))
                .ForMember(_ => _.Tags, _ => _.MapFrom(__ => __.Tags.Keys.ToArray()))
                .ForMember(_ => _.Properties, _ => _.MapFrom(__ => __.Props));
        }

        private void SourceProfile()
        {
            CreateMap<ApiSourceSearchQuery, SourceSearchQuery>()
                .ForMember(_ => _.SelectedReliabilities, _ => _.MapFrom(_ => _.Reliabilities));
            
            CreateMap<Source, ApiSource>()
                .ForMember(_ => _.Keywords,
                    _ => _.MapFrom(_ =>
                        _.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(__ => __.Trim())));
            CreateMap<Source, ApiSourceDetails>()
                .ForMember(_ => _.Keywords,
                    _ => _.MapFrom(_ =>
                        _.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(__ => __.Trim())));
        }

        private void FacetProfile()
        {
            CreateMap<ApiFacetSearchQuery, TagFacetSearchQuery>();
            CreateMap<TagFacet, ApiFacet>();
            CreateMap<TagFacet, ApiFacetDetails>()
                .ForMember(_ => _.FacetId, _ => _.MapFrom(_ => _.FacetId))
                .MaxDepth(2);
        }

        private void TagProfile()
        {
            CreateMap<ApiTagSearchQuery, TagSearchQuery>();
            
            CreateMap<Tag, APITagDetails>()
                .ForMember(_ => _.FacetPrefix, _ =>
                {
                    _.PreCondition(__ => __.Facet != null);
                    _.MapFrom(__ => __.Facet.Prefix);
                })
                .ForMember(_ => _.Keywords,
                    _ => _.MapFrom(_ =>
                        _.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(__ => __.Trim())))
                .ForMember(_ => _.ExtractionKeywords,
                    _ => _.MapFrom(_ =>
                        _.ExtractionKeywords.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(__ => __.Trim())));

            CreateMap<Tag, APITag>()
                .ForMember(_ => _.Keywords,
                    _ => _.MapFrom(_ =>
                        _.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(__ => __.Trim())))
                .ForMember(_ => _.ExtractionKeywords,
                    _ => _.MapFrom(_ =>
                        _.ExtractionKeywords.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(__ => __.Trim())));
        }

        private void ImportRuleProfile()
        {
            CreateMap<APIRewritingRule, ImportRule>()
                .ForMember(_ => _.Position, _ =>
                {
                    _.PreCondition(_ => _.Position >= 0);
                    _.MapFrom(_ => _.Position);
                })
                .ForMember(_ => _.Name, _ =>
                {
                    _.PreCondition(_ => _.Name != null);
                    _.MapFrom(_ => _.Name);
                })
                .ForMember(_ => _.Description, _ =>
                {
                    _.PreCondition(_ => _.Description != null);
                    _.MapFrom(_ => _.Description);
                })
                .ForMember(_ => _.SearchPattern, _ =>
                {
                    _.PreCondition(_ => _.SearchPattern != null);
                    _.MapFrom(_ => _.SearchPattern);
                })
                .ForMember(_ => _.Replacement, _ =>
                {
                    _.PreCondition(_ => _.Replacement != null);
                    _.MapFrom(_ => _.Replacement);
                });

            CreateMap<ImportRule, APIRewritingRule>();
            CreateMap<ImportRule, APIRewritingRuleDetails>()
                .ForMember(_ => _.RuleId, _ => _.MapFrom(_ => _.ImportRuleId))
                .ForMember(_ => _.RuleSetId, _ => _.MapFrom(_ => _.ImportRuleSetId));
        }
        
        private void ImportRuleSetProfile()
        {
            CreateMap<APIRewritingRuleSet, ImportRuleSet>()
                .ForMember(_ => _.Position, _ =>
                {
                    _.PreCondition(_ => _.Position >= 0);
                    _.MapFrom(_ => _.Position);
                })
                .ForMember(_ => _.Name, _ =>
                {
                    _.PreCondition(_ => _.Name != null);
                    _.MapFrom(_ => _.Name);
                })
                .ForMember(_ => _.Description, _ =>
                {
                    _.PreCondition(_ => _.Description != null);
                    _.MapFrom(_ => _.Description);
                });

            CreateMap<ImportRuleSet, APIRewritingRuleSet>();
            CreateMap<ImportRuleSet, ApiRewritingRuleSetDetails>()
                .ForMember(_ => _.RuleSetId, _ => _.MapFrom(_ => _.ImportRuleSetId));
        }
        
        private void RoleProfile()
        {
            CreateMap<APIRole, AppRole>()
                .ForMember(_ => _.Name, _ =>
                {
                    _.PreCondition(_ => _.Name != null);
                    _.MapFrom(_ => _.Name);
                })
                .ForMember(_ => _.Description, _ =>
                {
                    _.PreCondition(_ => _.Description != null);
                    _.MapFrom(_ => _.Description);
                });

            CreateMap<AppRole, APIRole>();
            CreateMap<AppRole, APIRoleDetails>();
        }
    }
}