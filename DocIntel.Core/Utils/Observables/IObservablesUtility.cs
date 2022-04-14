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
using System.Threading.Tasks;

using DocIntel.Core.Models;
using DocIntel.Core.Models.STIX;
using DocIntel.Core.Models.ThreatQuotient;
using DocIntel.Core.Repositories;

using Relationship = DocIntel.Core.Models.Relationship;

namespace DocIntel.Core.Utils.Observables
{
    public interface IObservablesUtility
    {
        Task ExtractObservables(AmbientContext ambientContext, Guid documentId, bool autoApprove = false);

        // TODO Return IAsyncEnumerable
        Task<IEnumerable<DocumentFileObservables>> ExportObservables(ObservableType ot);

        // TODO Return IAsyncEnumerable
        Task<IEnumerable<DocumentFileObservables>> DetectObservables(string response,
            Document document,
            DocumentFile file,
            bool autoApprove = false);

        bool RecommendAccepted(ObservableType type, ObservableStatus status, ObservableStatus history);

        // TODO Return IAsyncEnumerable
        Task<IEnumerable<Relationship>>
            DetectRelations(IEnumerable<DocumentFileObservables> dfoList, Document document);

        // TODO Return IAsyncEnumerable
        Task<IEnumerable<DocumentFileObservables>> GetDocumentObservables(Guid documentId,
            ObservableStatus? status = null);

        // TODO Return IAsyncEnumerable
        Task<IEnumerable<DocumentFileObservables>> GetDocumentObservables(string value, ObservableType observableType);

        Task<bool> DeleteObservable(Guid observableId);
        
        // TODO Move to another package, only for STIX-related code.
        Task<Bundle> CreateStixBundle(AmbientContext ambientContext, Guid documentId);

        // TODO Move to another package, only for ThreatQuotient-related code.
        Task<ArrayList> CreateTqExportDocument(AmbientContext ambientContext, Guid documentId);

        // TODO Move to another package, only for ThreatQuotient-related code.
        Task<ExportObject> CreateTqExport(AmbientContext ambientContext,
            int pageSize,
            int page,
            DateTime dateFrom,
            DateTime dateTo);
    }
}