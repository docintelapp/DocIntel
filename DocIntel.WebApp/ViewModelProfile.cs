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

using System.Linq;

using AutoMapper;

using DocIntel.Core.Models;
using DocIntel.WebApp.ViewModels.DocumentViewModel;

namespace DocIntel.WebApp
{
    public class ViewModelProfile : Profile
    {
        public ViewModelProfile()
        {
            CreateMap<Document, DocumentCreateViewModel>()
                .ForMember(_ => _.DocumentDate, _ => _.MapFrom(__ => __.Files.Min(___ => ___.DocumentDate)));
            
            // CreateMap<DocumentFileObservables, DocumentFileObservablesViewModel>();
            
            CreateMap<Observable, ObservableViewModel>()
                .ForMember(
                    dest => dest.Value,
                    opt => opt.MapFrom
                    (src => src.Type == ObservableType.File || src.Type == ObservableType.Artefact
                        ? src.Hashes[0].Value
                        : src.Value))
                .AfterMap((src, dst) =>
                {
                    if ((dst.Type == ObservableType.File || dst.Type == ObservableType.Artefact) && dst.Hashes != null)
                        dst.HashType = src.Hashes[0].HashType;
                });
            
            CreateMap<Observable, OTest>()
                .ForMember(
                    dest => dest.Value,
                    opt => opt.MapFrom
                    (src => src.Type == ObservableType.File || src.Type == ObservableType.Artefact
                        ? src.Hashes[0].Value
                        : src.Value))
                .AfterMap((src, dst) =>
                {
                    if ((dst.Type == ObservableType.File || dst.Type == ObservableType.Artefact) && dst.Hashes != null)
                        dst.HashType = src.Hashes[0].HashType;
                });

            CreateMap<ObservableViewModel, Observable>()
                .AfterMap((src, dst) =>
                {
                    if ((dst.Type == ObservableType.File || dst.Type == ObservableType.Artefact) && dst.Hashes != null)
                        dst.Hashes[0].Value = src.Value;
                });
        }
    }
}