/* DocIntel
 * Copyright (C) 2018-2021 Belgian Defense, Antoine Cailliau, Kevin Menten
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
using System.Threading.Tasks;

using DocIntel.Core.Models;
using DocIntel.Core.Models.STIX;
using DocIntel.Core.Models.ThreatQuotient;
using DocIntel.Core.Utils.Observables;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DocIntel.WebApp.Areas.API.Controllers
{
    [Area("API")]
    [Route("API/Export")]
    [ApiController]
    public class ExportController : DocIntelAPIControllerBase
    {
        private readonly IObservablesUtility _observablesUtility;

        public ExportController(UserManager<AppUser> userManager,
            DocIntelContext context,
            IObservablesUtility observablesUtility
        )
            : base(userManager, context)
        {
            _observablesUtility = observablesUtility;
        }

        // TODO Should be moved to a separate namespace and a generic way to export to various format should be provided.
        [HttpGet("Stix")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Bundle))]
        [Produces("application/json", "application/xml")]
        public async Task<Bundle> StixExport(Guid documentId)
        {
            // TODO Add permission checks
            var currentUser = await GetCurrentUser();
            var r = await _observablesUtility.CreateStixBundle(AmbientContext, documentId);

            return r;
        }

        // TODO Should be moved to a separate namespace and a generic way to export to various format should be provided.
        [HttpGet("TQDocument")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ArrayList))]
        [Produces("application/json", "application/xml")]
        public async Task<ArrayList> TQExport(Guid documentId)
        {
            // TODO Add permission checks
            var currentUser = await GetCurrentUser();
            var r = await _observablesUtility.CreateTqExportDocument(AmbientContext, documentId);
            return r;
        }

        // TODO Should be moved to a separate namespace and a generic way to export to various format should be provided.
        [HttpGet("TQ")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ExportObject))]
        [Produces("application/json", "application/xml")]
        public async Task<ExportObject> TQList(int pageSize, int page, DateTime dateFrom, DateTime dateTo)
        {
            // TODO Add permission checks
            var currentUser = await GetCurrentUser();
            var r = await _observablesUtility.CreateTqExport(AmbientContext, pageSize, page, dateFrom, dateTo);
            return r;
        }
    }
}