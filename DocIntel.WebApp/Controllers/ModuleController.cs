/* DocIntel
 * Copyright (C) 2018-2023 Belgian Defense, Antoine Cailliau
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
using DocIntel.Core.Authentication;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Modules;
using DocIntel.Core.Settings;
using DocIntel.WebApp.Helpers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DocIntel.WebApp.Controllers
{
    /// <summary>
    ///     Provides the functionalities for managing modules.
    /// </summary>
    public class ModuleController : BaseController
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly ILogger _logger;
        private readonly ModuleFactory _moduleFactory;

        public ModuleController(DocIntelContext context,
            ILogger<ModuleController> logger,
            ApplicationSettings configuration,
            AppUserManager userManager,
            IAuthorizationService authorizationService,
            ModuleFactory moduleFactory,
            IHttpContextAccessor accessor)
            : base(context,
                userManager,
                configuration,
                authorizationService)
        {
            _logger = logger;
            _moduleFactory = moduleFactory;
            _accessor = accessor;
        }

        /// <summary>
        ///     Provides the listing for the modules.
        /// </summary>
        /// <returns>
        ///     A view listing the modules. An "Unauthorized" response if
        ///     the user does not have the appropriate rights.
        /// </returns>
        [HttpGet("Module")]
        [HttpGet("Module/Index")]
        public async Task<IActionResult> Index()
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var enumerable = _moduleFactory.GetAll();

                _logger.Log(LogLevel.Information,
                    EventIDs.ListModuleSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully list modules.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return View(enumerable);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.ListModuleFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to list modules without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
        }
    }
}