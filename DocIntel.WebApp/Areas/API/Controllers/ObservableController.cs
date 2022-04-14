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

using AutoMapper;

using DocIntel.Core.Exceptions;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Utils.Observables;
using DocIntel.WebApp.Helpers;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DocIntel.WebApp.Areas.API.Controllers
{
    [Area("API")]
    [Route("API/Observables")]
    [ApiController]
    public class ObservableController : DocIntelAPIControllerBase
    {
        private readonly IHttpContextAccessor _accessor;

        private readonly IDocumentRepository _documentRepository;
        private readonly ILogger<ObservableController> _logger;

        // TODO Use the mapper. Always return APIObject 
        private readonly IMapper _mapper;
        private readonly IObservablesUtility _observablesUtility;

        public ObservableController(UserManager<AppUser> userManager,
            DocIntelContext context,
            IDocumentRepository documentRepository,
            ILogger<ObservableController> logger,
            IMapper mapper,
            IHttpContextAccessor accessor,
            IObservablesUtility observablesUtility)
            : base(userManager, context)
        {
            _documentRepository = documentRepository;
            _logger = logger;
            _mapper = mapper;
            _accessor = accessor;
            _observablesUtility = observablesUtility;
        }

        [HttpGet("Document")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<DocumentFileObservables>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> DocumentObservables(Guid documentId)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var document = await _documentRepository.GetAsync(
                    AmbientContext,
                    new DocumentQuery {DocumentId = documentId},
                    new string[] { }
                );

                return base.Ok(
                    await _observablesUtility.GetDocumentObservables(document.DocumentId)
                );
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIDetailsDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view document '{documentId}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", documentId),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIDetailsDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view a non-existing document '{documentId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", documentId),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpGet("Search")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<DocumentFileObservables>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Search(string value, ObservableType observableType)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                // TODO Check for permissions
                // TODO Check for classification
                // TODO Check for groups (RELTO/EYESONLY)
                return base.Ok(
                    await _observablesUtility.GetDocumentObservables(value, observableType)
                );
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIDetailsDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to search observables without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
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
                var result = await _observablesUtility.DeleteObservable(observableId);

                if (result)
                {
                    _logger.Log(LogLevel.Information,
                        EventIDs.APIDeleteWhitelistSucces,
                        new LogEvent(
                                $"User '{currentUser.UserName}' successfully deleted observable '{observableId}'.")
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
                            $"User '{currentUser.UserName}' attempted to delete a non-existing observable '{observableId}'.")
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
                            $"User '{currentUser.UserName}' attempted to delete observable '{observableId}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("observable.id", observableId),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
        }

        [HttpGet("Export")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<DocumentFileObservables>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Export(ObservableType observableType)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                // TODO Check for permissions
                // TODO Check for classification
                // TODO Check for groups (RELTO/EYESONLY)

                return base.Ok(
                    await _observablesUtility.ExportObservables(observableType)
                );
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIDetailsDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to export observables without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
        }

        [HttpGet("Extract")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Extract(Guid documentId)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var document =
                    await _documentRepository.GetAsync(AmbientContext, new DocumentQuery {DocumentId = documentId});

                if (document.MetaData != default && document.MetaData.ContainsKey("ExtractObservables"))
                    if (document.MetaData["ExtractObservables"]?.ToString().ToLower() == "false")
                        return Unauthorized();
                await _observablesUtility.ExtractObservables(AmbientContext, documentId, true);
                return Ok();
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIUnsubscribeDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to unsubscribe from document '{documentId}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", documentId),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIUnsubscribeDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to unsubscribe from a non-existing document '{documentId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", documentId),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }
    }
}