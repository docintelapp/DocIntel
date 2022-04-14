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

using System.Threading.Tasks;

using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DocIntel.WebApp.Controllers
{
    public class WhitelistController : BaseController
    {
        private readonly IObservableRepository _observableRepository;

        public WhitelistController(DocIntelContext context,
            ApplicationSettings configuration,
            UserManager<AppUser> userManager,
            IAuthorizationService authorizationService,
            IObservableRepository observableRepository)
            : base(context,
                userManager,
                configuration,
                authorizationService)
        {
            _observableRepository = observableRepository;
        }

        // REVIEW if necessary
        [HttpGet("Whitelist")]
        [HttpGet("Whitelist/Index")]
        public async Task<IActionResult> Index()
        {
            var currentUser = await GetCurrentUser();

            var o = new Observable
            {
                Status = ObservableStatus.Whitelisted, History = ObservableStatus.Whitelisted,
                Type = ObservableType.IPv4, Value = "194.5.98.55",
                RegisteredById = currentUser.Id, LastModifiedById = currentUser.Id
            };

            var result = await _observableRepository.IngestWhitelistedObservables(o);

            return new EmptyResult();
        }
    }
}