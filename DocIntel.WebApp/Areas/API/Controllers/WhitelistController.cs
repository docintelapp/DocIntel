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
using System.Collections.Generic;
using System.Threading.Tasks;

using AutoMapper;

using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Utils.Observables;
using DocIntel.WebApp.Areas.API.Models;
using DocIntel.WebApp.Helpers;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocIntel.WebApp.Areas.API.Controllers
{
    [Area("API")]
    [Route("API/Whitelist")]
    [ApiController]
    public class WhitelistController : DocIntelAPIControllerBase
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IAppAuthorizationService _appAuthorizationService;
        private readonly ILogger<WhitelistController> _logger;
        private readonly IMapper _mapper;
        private readonly IObservableWhitelistUtility _whitelistUtility;

        public WhitelistController(UserManager<AppUser> userManager,
            DocIntelContext context,
            IOptions<IdentityOptions> identityOptions,
            IAppAuthorizationService appAuthorizationService,
            ILogger<WhitelistController> logger,
            IObservableWhitelistUtility whitelistUtility,
            IHttpContextAccessor accessor,
            IMapper mapper, IHttpContextAccessor httpContextAccessor)
            : base(userManager, context)
        {
            _logger = logger;
            _appAuthorizationService = appAuthorizationService;
            _accessor = accessor;
            _whitelistUtility = whitelistUtility;
            _mapper = mapper;
        }

        [HttpGet("")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<APIObservable>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Index()
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var results = await _whitelistUtility.GetWhitelistedObservables();
                return Ok(_mapper.Map<IEnumerable<APIObservable>>(results));
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIListWhitelistFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to list tag without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
        }

        [HttpPost("")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Create([FromBody] APIObservable submittedViewModel)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                if (submittedViewModel is null)
                {
                    ModelState.AddModelError("Id", "Provide valid data");
                }
                else
                {
                    if (submittedViewModel.Id == Guid.Empty)
                        submittedViewModel.Id = Guid.NewGuid();
                    //TODO else check if ID exists

                    submittedViewModel.RegisteredById = currentUser.Id;
                    submittedViewModel.LastModifiedById = currentUser.Id;
                }

                if (ModelState.IsValid)
                {
                    var res = await _whitelistUtility.AddWhitelistedObservable(
                        _mapper.Map<Observable>(submittedViewModel));

                    _logger.Log(LogLevel.Information,
                        EventIDs.APICreateWhitelistSucces,
                        new LogEvent(
                                $"User '{currentUser.UserName}' successfully created a new whitelist '{submittedViewModel.Value ?? submittedViewModel.Hashes[0].Value}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddProperty("observable.id", submittedViewModel.Id),
                        null,
                        LogEvent.Formatter);

                    return Ok();
                }

                throw new InvalidArgumentException();
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APICreateWhitelistFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new whitelist '{submittedViewModel.Value ?? submittedViewModel.Hashes[0].Value}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (InvalidArgumentException e)
            {
                ModelState.Clear();
                foreach (var kv in e.Errors)
                foreach (var errorMessage in kv.Value)
                    ModelState.AddModelError(kv.Key, errorMessage);

                _logger.Log(LogLevel.Information,
                    EventIDs.APICreateWhitelistFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new whitelist observable with an invalid model.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return BadRequest(ModelState);
            }
        }

        [HttpDelete("")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Delete(Guid observableId)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var result = await _whitelistUtility.DeleteWhitelistedObservable(observableId);

                if (result)
                {
                    _logger.Log(LogLevel.Information,
                        EventIDs.APIDeleteWhitelistSucces,
                        new LogEvent(
                                $"User '{currentUser.UserName}' successfully deleted whitelist observable '{observableId}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddProperty("observable.id", observableId),
                        null,
                        LogEvent.Formatter);

                    return Ok();
                }

                _logger.Log(LogLevel.Warning,
                    EventIDs.APIDeleteWhitelistFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to delete a non-existing whitelist observable '{observableId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("observable.id", observableId),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIDeleteWhitelistFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to delete whitelist observable '{observableId}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("observable.id", observableId),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
        }
    }
}