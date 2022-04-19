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
using System.Linq;

using AutoMapper;

using DocIntel.Core.Models;
using DocIntel.WebApp.Areas.API.Models;

namespace DocIntel.WebApp.Areas.API
{
    public class APIProfile : Profile
    {
        public APIProfile()
        {
            CreateMap<Importer, APIIncomingFeed>();
            CreateMap<OrderedImportRuleSet, APIOrderedImportRuleSet>();
            CreateMap<ImportRuleSet, APIImportRuleSet>();
            CreateMap<ImportRule, APIImportRule>();
            CreateMap<Document, APIDocument>()
                .ForMember(_ => _.Tags, _ => _.MapFrom(_ => _.DocumentTags.Select(__ => __.Tag)));
            CreateMap<Tag, APITag>()
                .ForMember(_ => _.Keywords,
                    _ => _.MapFrom(_ =>
                        _.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(__ => __.Trim())))
                .ForMember(_ => _.ExtractionKeywords,
                    _ => _.MapFrom(_ =>
                        _.ExtractionKeywords.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(__ => __.Trim())))
                .ForMember(_ => _.FriendlyName, _ => _.Ignore());
            CreateMap<TagFacet, APITagFacet>();
            CreateMap<Comment, APIComment>();

            CreateMap<AppUser, APIAppUser>()
                .ForMember(_ => _.UserId, _ => _.MapFrom(_ => _.Id))
                .ForMember(_ => _.FriendlyName, _ => _.Ignore());

            CreateMap<Source, APISource>()
                .ForMember(_ => _.Keywords,
                    _ => _.MapFrom(_ =>
                        _.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(__ => __.Trim())))
                .ForMember(_ => _.FactualScore, _ => _.Ignore());
            CreateMap<Document, APIDocumentExport>()
                .ForMember(_ => _.Tags, _ => _.MapFrom(_ => _.DocumentTags.Select(__ => __.Tag)))
                .ForMember(_ => _.Adversaries,
                    _ => _.MapFrom(_ => _.DocumentTags.Where(u => u.Tag.Facet.Title == "group").Select(__ => __.Tag)));
            CreateMap<Tag, APIPropertyExport>();
        }
    }
}