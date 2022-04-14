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
using System.Threading.Tasks;

using DocIntel.Core.Models;

namespace DocIntel.Core.Repositories
{
    public interface IObservableRepository
    {
        Task<IEnumerable<DocumentFileObservables>> GetObservables(Guid documentId, ObservableStatus? status = null);
        Task<IEnumerable<DocumentFileObservables>> SearchObservables(string value, ObservableType observableType);
        Task<IEnumerable<DocumentFileObservables>> SearchExistingObservables(IEnumerable<Observable> observables,
            string[] status = null);
        Task<bool> IngestObservables(IEnumerable<DocumentFileObservables> observables);
        Task<bool> UpdateObservables(IEnumerable<DocumentFileObservables> observables);
        Task<bool> IngestWhitelistedObservables(Observable observable);
        Task<IEnumerable<Observable>> GetWhitelistedObservables();
        Task DeleteAllObservables(Guid documentId);
        Task<bool> DeleteObservable(Guid observableId);
        Task<IEnumerable<DocumentFileObservables>> ExportDocumentObservables(ObservableType ot);
        Task<bool> IngestRelationships(IEnumerable<Relationship> relationships);
        Task<bool> ReplaceRelationships(Guid documentId, IEnumerable<Relationship> relationships);
        Task<IEnumerable<Relationship>> GetRelationships(Guid documentId);
        Task<bool> DeleteRelationshipForTag(Guid documentId, Guid tagId);
        Task<bool> DeleteWhitelistedObservable(Guid observableId);
    }
}