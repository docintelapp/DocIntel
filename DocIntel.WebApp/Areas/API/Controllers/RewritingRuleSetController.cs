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
using System.Linq;
using System.Threading.Tasks;

using AutoMapper;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.WebApp.Areas.API.Models;
using DocIntel.WebApp.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DocIntel.WebApp.Areas.API.Controllers;

/// <summary>
/// Rewriting rule sets in DocIntel organize the rewriting rules into coherent and
/// manageable sets of rewriting rules.
///
/// ## Document Attributes
///
/// | Attribute   | Description        |
/// | ----------- | ------------------ |
/// | RuleSetId   | The set identifier |
/// | Name        | The name           |
/// | Description | The description    |
/// | Position    | The position       |
/// 
/// ## Document Relationships
///
/// | Relationship | Description          |
/// | ------------ | -------------------- |
/// | Rules        | The associated rules |
/// 
/// </summary>
[Area("API")]
[Route("API/RewritingRuleSet")]
[ApiController]
public class RewritingRuleSetController : DocIntelAPIControllerBase
{
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IImportRuleRepository _importRuleRepository;

    public RewritingRuleSetController(UserManager<AppUser> userManager,
        DocIntelContext context,
        ILogger<RewritingRuleSetController> logger,
        IImportRuleRepository importRuleRepository,
        IHttpContextAccessor accessor,
        IMapper mapper)
        : base(userManager, context)
    {
        _logger = logger;
        _importRuleRepository = importRuleRepository;
        _accessor = accessor;
        _mapper = mapper;
    }

    /// <summary>
    /// Get rule sets
    /// </summary>
    /// <remarks>
    /// Get the rewriting rule sets
    ///
    /// For example, with cURL
    ///
    ///     curl --request GET \
    ///       --url http://localhost:5001/API/RewritingRuleSet \
    ///       --header 'Authorization: Bearer $TOKEN'
    ///
    /// </remarks>
    /// <returns>The rule sets</returns>
    /// <response code="200">Returns the rule sets</response>
    [HttpGet("")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ApiRewritingRuleSetDetails>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Produces("application/json")]
    public async Task<IActionResult> Index()
    {
        var currentUser = await GetCurrentUser();

        try
        {
            return Ok(_mapper.Map<IEnumerable<ApiRewritingRuleSetDetails>>(
                _importRuleRepository.GetAllSets(AmbientContext)
                ));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.ListImportRuleSetFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to list importRule without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
    }

    /// <summary>
    /// Get a rule set
    /// </summary>
    /// <remarks>
    /// Gets the details of the rewriting rule set.
    ///
    /// For example, with cURL
    ///
    ///     curl --request GET \
    ///       --url http://localhost:5001/API/RewritingRuleSet/640afad4-0a3d-416a-b6f0-22cb85e0d638 \
    ///       --header 'Authorization: Bearer $TOKEN'
    /// 
    /// </remarks>
    /// <param name="setId" example="1ee4eac9-6d56-4665-bb78-6986dd6bf7a2">The set identifier</param>
    /// <returns>The set</returns>
    /// <response code="200">Returns the set</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="404">Set does not exists</response>
    [HttpGet("{setId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiRewritingRuleSetDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Details([FromRoute] Guid setId)
    {
        var currentUser = await GetCurrentUser();
        try
        {
            var importRule = _importRuleRepository.GetSet(AmbientContext, setId);
            return Ok(_mapper.Map<ApiRewritingRuleSetDetails>(importRule));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DetailsImportRuleSetFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to view details of import rule set '{setId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("importRule.id", setId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DetailsImportRuleSetFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to view details of a non-existing import rule set '{setId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("importRule.id", setId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }


    /// <summary>
    /// Create a rule set
    /// </summary>
    /// <remarks>
    /// Creates a rewriting rule set.
    ///
    /// For example, with cURL
    ///
    ///     curl --request POST \
    ///       --url http://localhost:5001/API/RewritingRuleSet/ \
    ///       --header 'Authorization: Bearer $TOKEN' \
    ///       --header 'Content-Type: application/json' \
    ///       --data '{
    ///       "title": "My set",
    ///       "description": "<p>My set description</p>",
    ///       "position": 0
    ///     }'
    /// 
    /// </remarks>
    /// <param name="submittedRewritingRule">The set</param>
    /// <returns>The newly created set, as recorded</returns>
    /// <response code="200">Returns the newly created set</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="400">Submitted value is invalid</response>
    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(ApiRewritingRuleSetDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> Create([FromBody] APIRewritingRuleSet submittedRewritingRule)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            if (ModelState.IsValid)
            {
                var importRule = _mapper.Map<ImportRuleSet>(submittedRewritingRule);
                
                await _importRuleRepository.Create(AmbientContext, importRule);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information, EventIDs.CreateImportRuleSetSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully created a new import rule set '{submittedRewritingRule.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddImportRuleSet(importRule),
                    null,
                    LogEvent.Formatter);

                return Ok(_mapper.Map<ApiRewritingRuleSetDetails>(importRule));
            }

            return BadRequest(ModelState);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.CreateImportRuleSetFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to create a new import rule set '{submittedRewritingRule.Name}' without legitimate rights.")
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
                EventIDs.CreateImportRuleSetFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to create a new importRule with an invalid model.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return BadRequest(ModelState);
        }
    }

    /// <summary>
    /// Update a rule set
    /// </summary>
    /// <remarks>
    /// Updates a rewriting rule set
    ///
    /// For example, with cURL
    ///
    ///     curl --request PATCH \
    ///       --url http://localhost:5001/API/RewritingRuleSet/1ee4eac9-6d56-4665-bb78-6986dd6bf7a2/Tags \
    ///       --header 'Authorization: Bearer $TOKEN' \
    ///       --header 'Content-Type: application/json' \
    ///       --data '{
    ///       "title": "My (updated) set",
    ///       "description": "<p>My set description</p>",
    ///       "position": 0
    ///     }'
    /// 
    /// </remarks>
    /// <param name="setId" example="640afad4-0a3d-416a-b6f0-22cb85e0d638">The set identifier</param>
    /// <param name="submittedSet">The updated set</param>
    /// <returns>The set facet</returns>
    /// <response code="200">Returns the newly updated set</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="404">The set does not exist</response>
    [HttpPatch("{setId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(ApiRewritingRuleSetDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Edit([FromRoute] Guid setId, [FromBody] APIRewritingRuleSet submittedSet)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var ruleSet = _importRuleRepository.GetSet(AmbientContext, setId);
            
            if (ModelState.IsValid)
            {
                ruleSet = _mapper.Map(submittedSet, ruleSet);
                
                await _importRuleRepository.Update(AmbientContext, ruleSet);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information, EventIDs.UpdateImportRuleSetSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully edited import rule set '{ruleSet.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddImportRuleSet(ruleSet),
                    null,
                    LogEvent.Formatter);

                return Ok(_mapper.Map<APIRewritingRuleSet>(ruleSet));
            }

            throw new InvalidArgumentException(ModelState);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.UpdateImportRuleSetFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit import rule set '{setId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("importRule.id", setId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.UpdateImportRuleSetFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit a non-existing import rule set '{setId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("importRule.id", setId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
        catch (InvalidArgumentException e)
        {
            ModelState.Clear();
            foreach (var kv in e.Errors)
            foreach (var errorMessage in kv.Value)
                ModelState.AddModelError(kv.Key, errorMessage);

            _logger.Log(LogLevel.Information,
                EventIDs.UpdateImportRuleSetFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit import rule set '{setId}' with an invalid model.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("importRule.id", setId),
                null,
                LogEvent.Formatter);

            return BadRequest(ModelState);
        }
    }

    /// <summary>
    /// Deletes a rule set
    /// </summary>
    /// <remarks>
    /// Deletes the specified rewriting rule set,
    ///
    /// For example, with cURL
    ///
    ///     curl --request DELETE \
    ///       --url http://localhost:5001/API/RewritingRuleSet/6e7635a0-27bb-495d-a218-15b54cb938fd \
    ///       --header 'Authorization: Bearer $TOKEN'
    ///
    /// </remarks>
    /// <param name="setId" example="6e7635a0-27bb-495d-a218-15b54cb938fd">The set identifier</param>
    /// <response code="200">The set is deleted</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="404">The set does not exist</response>
    [HttpDelete("{setId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Delete(Guid setId)
    {
        var currentUser = await GetCurrentUser();
        try
        {
            await _importRuleRepository.RemoveSet(AmbientContext, setId);
            await _context.SaveChangesAsync();

            _logger.Log(LogLevel.Information,
                EventIDs.DeleteImportRuleSetSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully deleted import rule set '{setId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("importRule.id", setId),
                null,
                LogEvent.Formatter);

            return Ok();
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DeleteImportRuleSetFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to delete a new import rule set '{setId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("importRule.id", setId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DeleteImportRuleSetFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to delete a non-existing import rule set '{setId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("importRule.id", setId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }
}