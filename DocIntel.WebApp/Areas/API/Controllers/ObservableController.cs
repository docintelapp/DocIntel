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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AutoMapper;
using DocIntel.Core.Authentication;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Utils.Observables;
using DocIntel.WebApp.Areas.API.Models;
using DocIntel.WebApp.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using Synsharp.Telepath.Messages;

namespace DocIntel.WebApp.Areas.API.Controllers;

/// <summary>
/// Observables are technical elements such as IP addresses, or domain names that allow analyst to detect, respond,
/// or understand cyber threats. Observables in DocIntel are at their infancy although the underlying model is very
/// mature, as seen in [Synapse documentation](https://synapse.docs.vertex.link/en/latest/synapse/).
/// 
/// ## Observable Attributes
///
/// * *Iden*: The identifier
/// * *Type*: The type
/// * *Value*: The value 
/// * *Tags*: The associated tags (these are not DocIntel tags, but Synapse tags)
/// * *Properties*: The properties
/// </summary>
[Area("API")]
[Route("API/Observable")]
[ApiController]
public class ObservableController : DocIntelAPIControllerBase
{
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly ISynapseRepository _observableRepository;
    private readonly IDocumentRepository _documentRepository;

    public ObservableController(UserManager<AppUser> userManager,
        DocIntelContext context,
        ILogger<ObservableController> logger,
        ISynapseRepository observableRepository,
        IHttpContextAccessor accessor,
        IMapper mapper, IDocumentRepository documentRepository)
        : base(userManager, context)
    {
        _logger = logger;
        _observableRepository = observableRepository;
        _accessor = accessor;
        _mapper = mapper;
        _documentRepository = documentRepository;
    }

    /// <summary>
    /// Get observables
    /// </summary>
    /// <remarks>
    /// Returns all observables referenced in a document
    ///
    /// For example, with cURL
    /// 
    ///     curl --request GET \
    ///         --url http://localhost:5001/API/Observable/6EB0681C-9E7B-48A8-AA72-50DD1CF00506 \
    ///         --header 'Authorization: Bearer $TOKEN'
    /// 
    /// </remarks>
    /// <returns>The observables</returns>
    /// <param name="documentId" example="6EB0681C-9E7B-48A8-AA72-50DD1CF00506">The document identifier</param>
    /// <response code="200">Returns the observable</response>
    /// <response code="404">The document does not exist</response>
    /// <response code="401">Action is not authorized</response>
    [HttpGet("/API/Document/{documentId}/Observables")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ApiObservableDetails>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [SwaggerOperation(
        OperationId = "Get",
        Tags = new [] { "Document", "Observable" }
    )]
    public async Task<IActionResult> Get(Guid documentId)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var document = await _documentRepository.GetAsync(AmbientContext, documentId); 
            
            return Ok(
                _mapper.Map<IEnumerable<ApiObservableDetails>>(await _observableRepository.GetObservables(document).ToListAsync())
            );
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.ListObservableFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to list observables without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.ListObservableFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to list observables on a non-existing document '{documentId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("document.id", documentId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }
    
    /// <summary>
    /// Get observable details
    /// </summary>
    /// <remarks>
    /// Returns the details of an observable.
    ///
    /// For example, with cURL
    /// 
    ///     curl --request GET \
    ///         --url http://localhost:5001/API/Observable/64cbe362c8b2c6df61e0d264fc55c1a6454d407dc5048a0445480907aedc1487 \
    ///         --header 'Authorization: Bearer $TOKEN'
    /// 
    /// </remarks>
    /// <param name="observableId" example="64cbe362c8b2c6df61e0d264fc55c1a6454d407dc5048a0445480907aedc1487">The identifier of the observable</param>
    /// <returns>The observable</returns>
    /// <response code="201">Returns the observable</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="404">The observable does not exist</response>
    [HttpGet("{observableId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiObservableDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "Get"
    )]
    public async Task<IActionResult> Details(string observableId)
    {
        var currentUser = await GetCurrentUser();
        try
        {
            var observable = await _observableRepository.GetObservableByIden(observableId);
            return Ok(_mapper.Map<ApiObservableDetails>(observable));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DetailsObservableFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to view details of observable '{observableId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("observable.id", observableId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DetailsObservableFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to view details of a non-existing observable '{observableId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("observable.id", observableId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }
    
    /// <summary>
    /// Create a observable
    /// </summary>
    /// <remarks>
    /// Creates a new observable. Currently, the endpoint only supports creating simple observables and will ignore
    /// the properties.
    ///
    /// For example, with cURL
    /// 
    /// 
    /// </remarks>
    /// <param name="observable">The observable to create</param>
    /// <returns>The created observable</returns>
    /// <response code="200">Returns the newly created observable</response>
    /// <response code="400">The provided data are invalid (e.g. empty title, non-existing parent observable, etc.)</response>
    /// <response code="401">Action is not authorized</response>
    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(ApiObservableDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(
        OperationId = "Create"
    )]
    public async Task<IActionResult> Create([FromBody] ApiObservableDetails observable)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            if (ModelState.IsValid)
            {
                var node = _mapper.Map<ApiObservableDetails, SynapseNode>(observable);
                node = await _observableRepository.Add(node);
                
                _logger.Log(LogLevel.Information,
                    EventIDs.CreateObservableSuccess,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully created a new observable '{observable.Value}' with id '{node.Iden}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddObservable(node),
                    null,
                    LogEvent.Formatter);

                return Ok(_mapper.Map<ApiObservableDetails>(node));
            }

            return BadRequest(ModelState);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.CreateObservableFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to create a new observable '{observable.Value}' without legitimate rights.")
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
                EventIDs.CreateObservableFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to create a new observable with an invalid model.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return BadRequest(ModelState);
        }
    }
    
    /// <summary>
    /// Reference an observable
    /// </summary>
    /// <remarks>
    /// Mark an observable, created if necessary, as referenced by a document. The observable is added to the main
    /// layer if the document is registered, and to the layer to be reviewed if not.
    /// </remarks>
    /// <param name="documentId">The document identifier</param>
    /// <param name="observable">The observable to create</param>
    /// <returns>The created observable</returns>
    /// <response code="200">Returns the newly created observable</response>
    /// <response code="400">The provided data are invalid (e.g. empty value, unsupported type, etc.)</response>
    /// <response code="401">Action is not authorized</response>
    [HttpPost("/API/Document/{documentId}/Observable")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(ApiObservableDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(
        OperationId = "Reference",
        Tags = new [] { "Observable", "Document" }
    )]
    public async Task<IActionResult> Reference([FromBody] ApiObservableDetails observable, [FromRoute] Guid documentId)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var document = await _documentRepository.GetAsync(AmbientContext, documentId);
            
            if (ModelState.IsValid)
            {
                var node = _mapper.Map<ApiObservableDetails, SynapseNode>(observable);
                node = await _observableRepository.Add(node);
                if (document.Status != DocumentStatus.Registered)
                {
                    var view = await _observableRepository.CreateView(document);
                    await _observableRepository.Add(node, document, view);   
                }
                else
                {
                    await _observableRepository.Add(node, document);   
                }
                
                _logger.Log(LogLevel.Information,
                    EventIDs.CreateObservableSuccess,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully created a new observable '{observable.Value}' with id '{node.Iden}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddObservable(node),
                    null,
                    LogEvent.Formatter);

                return Ok(_mapper.Map<ApiObservableDetails>(node));
            }

            return BadRequest(ModelState);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.CreateObservableFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to create a new observable '{observable.Value}' without legitimate rights.")
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
                EventIDs.CreateObservableFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to create a new observable with an invalid model.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return BadRequest(ModelState);
        }
    }
    
    /// <summary>
    /// Dereference an observable
    /// </summary>
    /// <remarks>
    /// Removes an observable from the document references.
    /// </remarks>
    /// <param name="documentId">The document identifier</param>
    /// <param name="observableId">The observable identifier</param>
    /// <response code="200">Obserable is removed</response>
    /// <response code="404">Document or observable does not exists</response>
    /// <response code="401">Action is not authorized</response>
    [HttpDelete("/API/Document/{documentId}/Observable/{observableId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(
        OperationId = "Dereference",
        Tags = new [] { "Observable", "Document" }
    )]
    public async Task<IActionResult> Dereference([FromRoute] string observableId, [FromRoute] Guid documentId)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var document = await _documentRepository.GetAsync(AmbientContext, documentId);
            
            if (ModelState.IsValid)
            {
                if (document.Status != DocumentStatus.Registered)
                {
                    await _observableRepository.Remove(document, observableId, true);   
                }
                else
                {
                    await _observableRepository.Remove(document, observableId);   
                }
                
                _logger.Log(LogLevel.Information,
                    EventIDs.CreateObservableSuccess,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully removed reference to '{observableId}' from document '{documentId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("observable.id", observableId)
                        .AddProperty("document.id", documentId),
                    null,
                    LogEvent.Formatter);

                return Ok();
            }

            return BadRequest(ModelState);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.CreateObservableFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to dereference '{observableId}' from document '{documentId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("observable.id", observableId)
                    .AddProperty("document.id", documentId),
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
                EventIDs.CreateObservableFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to dereference a new observable with an invalid model.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("observable.id", observableId)
                    .AddProperty("document.id", documentId),
                null,
                LogEvent.Formatter);

            return BadRequest(ModelState);
        }
    }
}