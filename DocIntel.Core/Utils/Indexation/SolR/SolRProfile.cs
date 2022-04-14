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

namespace DocIntel.Core.Utils.Indexation.SolR
{
    public class SolRProfile : Profile
    {
        public SolRProfile()
        {
            CreateMap<Document, IndexedDocument>()
                .ForMember(_ => _.Tags, _ => _.MapFrom(_ => _.DocumentTags.Select(__ => __.Tag.FriendlyName)))
                .ForMember(_ => _.TagsId,
                    _ => _.MapFrom(_ => _.DocumentTags.Select(__ => __.Tag.Facet.Id + "/" + __.Tag.TagId)))
                .ForMember(_ => _.Reliability, _ => _.MapFrom(_ => _.Source.Reliability))
                .ForMember(_ => _.Comments, _ => _.MapFrom(_ => _.Comments.Select(__ => __.Body)))
                .ForMember(_ => _.Classification, _ => _.MapFrom(_ => _.Classification != null ? _.Classification.ClassificationId : default(Guid)))
                .ForMember(_ => _.ReleasableTo, _ => _.MapFrom(__ => __.ReleasableTo != null ? __.ReleasableTo.Select(___ => ___.GroupId).ToArray() : new Guid[]{}))
                .ForMember(_ => _.EyesOnly, _ => _.MapFrom(__ => __.EyesOnly != null ? __.EyesOnly.Select(___ => ___.GroupId).ToArray() : new Guid[]{}));

            CreateMap<Tag, IndexedTag>()
                .ForMember(_ => _.FacetPrefix, _ => _.MapFrom(_ => _.Facet.Prefix))
                .ForMember(_ => _.FacetTitle, _ => _.MapFrom(_ => _.Facet.Title))
                .ForMember(_ => _.FacetDescription, _ => _.MapFrom(_ => _.Facet.Description))
                .ForMember(_ => _.Keywords,
                    _ => _.MapFrom(_ =>
                        _.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(__ => __.Trim())));

            CreateMap<TagFacet, IndexedTagFacet>()
                .ForMember(_ => _.FacetId, _ => _.MapFrom(_ => _.Id));
            
            CreateMap<Source, IndexedSource>()
                .ForMember(_ => _.NumDocs, _ => _.MapFrom(_ => _.Documents.Count))
                .ForMember(_ => _.Reliability, _ => _.Ignore())
                .ForMember(_ => _.ReliabilityScore, _ => _.MapFrom(_ => (int) _.Reliability))
                .ForMember(_ => _.LastDocumentDate, _ => _.MapFrom(_ => _.Documents.Select(document => document.RegistrationDate).Union(new DateTime[] { DateTime.MinValue }).Max()))
                .ForMember(_ => _.Keywords,
                    _ => _.MapFrom(_ => _.Keywords.Split(',',
                            StringSplitOptions.RemoveEmptyEntries)
                        .Select(__ => __.Trim())));
        }
    }
}