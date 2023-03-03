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
using DocIntel.WebApp.Areas.API.Models;
using DocIntel.WebApp.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;

namespace DocIntel.WebApp.Areas.API.Controllers;

/// <summary>
/// Rewriting rules in DocIntel allow tags to be rewritten.
///
/// ## Document Attributes
///
/// | Attribute     | Description                              |
/// | ------------- | ---------------------------------------- |
/// | RuleId        | The rule identifier                      |
/// | RuleSetId     | The set identifier                       |
/// | Name          | The name                                 |
/// | Description   | The description                          |
/// | Position      | The position within the set              |
/// | SearchPattern | The regular expression pattern to search |
/// | Replacement   | The replacement expression               |
/// 
/// ## Document Relationships
///
/// | Relationship  | Description             |
/// | ------------- | ----------------------- |
/// | ImportRuleSet | The associated rule set |
/// 
/// </summary>
[Area("API")]
[Route("API/RewritingRule")]
[ApiController]
public class RewritingRuleController : DocIntelAPIControllerBase
{
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IImportRuleRepository _importRuleRepository;

    public RewritingRuleController(AppUserManager userManager,
        DocIntelContext context,
        ILogger<RewritingRuleController> logger,
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
    /// Get the rules of a set 
    /// </summary>
    /// <remarks>
    /// Get the rules of a rule set
    ///
    /// For example, with cURL
    ///
    ///     curl --request GET \
    ///       --url http://localhost:5001/API/RewritingRuleSet/1ee4eac9-6d56-4665-bb78-6986dd6bf7a2/Rules \
    ///       --header 'Authorization: Bearer $TOKEN'
    ///
    /// </remarks>
    /// <param name="setId" example="1ee4eac9-6d56-4665-bb78-6986dd6bf7a2">The et identifier</param>
    /// <returns>The rules</returns>
    /// <response code="200">Returns the rules</response>
    /// <response code="404">Set does not exists</response>
    [HttpGet("/API/RewritingRuleSet/{setId}/Rules")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<APIRewritingRuleDetails>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [SwaggerOperation(Tags=new [] { "RewritingRule", "RewritingRuleSet" })]
    [Produces("application/json")]
    public async Task<IActionResult> Index([FromRoute] Guid setId)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            return Ok(_mapper.Map<IEnumerable<APIRewritingRuleDetails>>(
                _importRuleRepository.GetAll(AmbientContext, setId)
                ));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.ListImportRuleFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to list importRule without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
    }

    /// <summary>
    /// Get a rule
    /// </summary>
    /// <remarks>
    /// Gets the details of the rewriting rule.
    ///
    /// For example, with cURL
    ///
    ///     curl --request GET \
    ///       --url http://localhost:5001/API/RewritingRule/640afad4-0a3d-416a-b6f0-22cb85e0d638 \
    ///       --header 'Authorization: Bearer $TOKEN'
    /// 
    /// </remarks>
    /// <param name="ruleId" example="1ee4eac9-6d56-4665-bb78-6986dd6bf7a2">The rule identifier</param>
    /// <returns>The rule</returns>
    /// <response code="200">Returns the rule</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="404">Rule does not exists</response>
    [HttpGet("{ruleId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIRewritingRuleDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Details(Guid ruleId)
    {
        var currentUser = await GetCurrentUser();
        try
        {
            var importRule = _importRuleRepository.Get(AmbientContext, ruleId);
            return Ok(_mapper.Map<APIRewritingRuleDetails>(importRule));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DetailsImportRuleFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to view details of import rule '{ruleId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("importRule.id", ruleId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DetailsImportRuleFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to view details of a non-existing import rule '{ruleId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("importRule.id", ruleId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }


    /// <summary>
    /// Create a rule
    /// </summary>
    /// <remarks>
    /// Creates a new rule for a ruleset.
    ///
    /// For example, with cURL
    ///
    ///     curl --request POST \
    ///       --url http://localhost:5001/API/RewritingRuleSet/1ee4eac9-6d56-4665-bb78-6986dd6bf7a2/Rules \
    ///       --header 'Authorization: Bearer $TOKEN' \
    ///       --header 'Content-Type: application/json' \
    ///       --data '{
    ///       "name": "TLP white to clear",
    ///       "description": "<p>Ensure that latest version of TLP is used.</p>",
    ///       "position": 0,
    ///       "search_pattern": "tlp:white",
    ///       "replacement": "tlp:clear"
    ///     }'
    /// 
    /// </remarks>
    /// <param name="setId" example="1ee4eac9-6d56-4665-bb78-6986dd6bf7a2">The facet identifier</param>
    /// <param name="submittedRewritingRule">The rule</param>
    /// <returns>The newly created rule, as recorded</returns>
    /// <response code="200">Returns the newly created rule</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="400">Submitted value is invalid</response>
    [HttpPost("/API/RewritingRuleSet/{setId}/Rules")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(APIRewritingRuleDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> Create([FromRoute] Guid setId, [FromBody] APIRewritingRule submittedRewritingRule)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            if (ModelState.IsValid)
            {

                var importRule = _mapper.Map<ImportRule>(submittedRewritingRule);
                importRule.ImportRuleSetId = setId;
                
                await _importRuleRepository.Create(AmbientContext, importRule);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information, EventIDs.CreateImportRuleSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully created a new import rule '{submittedRewritingRule.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddImportRule(importRule),
                    null,
                    LogEvent.Formatter);

                return Ok(_mapper.Map<APIRewritingRuleDetails>(importRule));
            }

            return BadRequest(ModelState);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.CreateImportRuleFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to create a new import rule '{submittedRewritingRule.Name}' without legitimate rights.")
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
                EventIDs.CreateImportRuleFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to create a new importRule with an invalid model.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return BadRequest(ModelState);
        }
    }

    /// <summary>
    /// Update a rule
    /// </summary>
    /// <remarks>
    /// Updates a rewriting rule,
    ///
    /// For example, with cURL
    ///
    ///     curl --request PATCH \
    ///       --url http://localhost:5001/API/RewritingRule/1ee4eac9-6d56-4665-bb78-6986dd6bf7a2 \
    ///       --header 'Authorization: Bearer $TOKEN' \
    ///       --header 'Content-Type: application/json' \
    ///       --data '{
    ///       "name": "TLP white to clear",
    ///       "description": "<p>Ensure that latest version of TLP is used.</p>",
    ///       "position": 0,
    ///       "search_pattern": "tlp:white",
    ///       "replacement": "tlp:clear"
    ///     }'
    /// 
    /// </remarks>
    /// <param name="ruleId" example="640afad4-0a3d-416a-b6f0-22cb85e0d638">The rule identifier</param>
    /// <param name="submittedRewritingRule">The updated rule</param>
    /// <returns>The rule</returns>
    /// <response code="200">Returns the newly updated rule</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="404">The rule does not exist</response>
    [HttpPatch("{ruleId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(APIRewritingRuleDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Edit([FromRoute] Guid ruleId, [FromBody] APIRewritingRule submittedRewritingRule)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var importRule = _importRuleRepository.Get(AmbientContext, ruleId);
            
            if (ModelState.IsValid)
            {
                importRule = _mapper.Map(submittedRewritingRule, importRule);

                await _importRuleRepository.Update(AmbientContext, importRule);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information, EventIDs.UpdateImportRuleSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully edited import rule '{importRule.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddImportRule(importRule),
                    null,
                    LogEvent.Formatter);

                return Ok(_mapper.Map<APIRewritingRule>(importRule));
            }

            throw new InvalidArgumentException(ModelState);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.UpdateImportRuleFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit import rule '{ruleId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("importRule.id", ruleId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.UpdateImportRuleFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit a non-existing import rule '{ruleId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("importRule.id", ruleId),
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
                EventIDs.UpdateImportRuleFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit import rule '{ruleId}' with an invalid model.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("importRule.id", ruleId),
                null,
                LogEvent.Formatter);

            return BadRequest(ModelState);
        }
    }

    /// <summary>
    /// Deletes a rule
    /// </summary>
    /// <remarks>
    /// Deletes the specified rule,
    ///
    /// For example, with cURL
    ///
    ///     curl --request DELETE \
    ///       --url http://localhost:5001/API/RewritingRule/6e7635a0-27bb-495d-a218-15b54cb938fd \
    ///       --header 'Authorization: Bearer $TOKEN'
    ///
    /// </remarks>
    /// <param name="ruleId" example="6e7635a0-27bb-495d-a218-15b54cb938fd">The rule identifier</param>
    /// <response code="200">The rule is deleted</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="404">The rule does not exist</response>
    [HttpDelete("{ruleId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Delete(Guid ruleId)
    {
        var currentUser = await GetCurrentUser();
        try
        {
            await _importRuleRepository.Remove(AmbientContext, ruleId);
            await _context.SaveChangesAsync();

            _logger.Log(LogLevel.Information,
                EventIDs.DeleteImportRuleSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully deleted import rule '{ruleId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("importRule.id", ruleId),
                null,
                LogEvent.Formatter);

            return Ok();
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DeleteImportRuleFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to delete a new import rule '{ruleId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("importRule.id", ruleId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DeleteImportRuleFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to delete a non-existing import rule '{ruleId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("importRule.id", ruleId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }
}