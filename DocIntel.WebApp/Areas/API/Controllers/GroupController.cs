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

namespace DocIntel.WebApp.Areas.API.Controllers;

/// <summary>
/// Groups in DocIntel controls who can see the information. Users can be part of multiple groups. An instance has
/// default groups, document and files are always considered as releasable to these groups (most often, the default
/// groups correspond to the team managing the DocIntel instance). Note that users do not belong to these groups by
/// default. The groups are used to decorate documents and files with 'Releasable to' and 'Eyes Only' list of groups.
/// Groups used in 'Releasable to' are used to indicate who can view the document. For example, a document releasable
/// to 'CTI Summit 2022' can be seen by any member of the group 'CTI Summit 2022'. Because all documents are releasable
/// to default groups, the users belonging to the default groups will also see the document. If you want the document
/// to be only seen by members of 'CTI Summit 2022' but not the default groups, you can use 'Eyes only' groups.
///
/// Hidden groups are groups only visible to its member, like the fight club. Groups are hierarchical, a user member of
/// a sub-group is also a member of the parent groups.
/// 
/// ## Group Attributes
///
/// | Attribute        | Description                        |
/// |------------------|------------------------------------|
/// | GroupId          | The identifier                     |
/// | Name             | The name                           |
/// | Description      | The description                    |
/// | Default          | Whether the group is default       |
/// | Hidden           | Whether the group is hidden        |
/// | ParentGroupId    | The identifier of the parent group |
/// | CreationDate     | The creation date                  |
/// | ModificationDate | The last modification date         |
/// 
/// ## Group Relationship
///
/// | Attribute   | Description                   |
/// |-------------|-------------------------------|
/// | ParentGroup | The parent group              |
/// | Members     | The users member of the group |
/// 
/// </summary>
[Area("API")]
[Route("API/Group")]
[ApiController]
public class GroupController : DocIntelAPIControllerBase
{
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IGroupRepository _groupRepository;

    public GroupController(AppUserManager userManager,
        DocIntelContext context,
        ILogger<GroupController> logger,
        IGroupRepository groupRepository,
        IHttpContextAccessor accessor,
        IMapper mapper)
        : base(userManager, context)
    {
        _logger = logger;
        _groupRepository = groupRepository;
        _accessor = accessor;
        _mapper = mapper;
    }

    /// <summary>
    /// Get groups
    /// </summary>
    /// <remarks>
    /// Returns all groups. 
    ///
    /// For example, with cURL
    /// 
    ///     curl --request GET \
    ///         --url http://localhost:5001/API/Group \
    ///         --header 'Authorization: Bearer $TOKEN'
    /// 
    /// </remarks>
    /// <returns>The groups</returns>
    /// <response code="200">Returns the group</response>
    [HttpGet("")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<APIGroupDetails>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Produces("application/json")]
    public async Task<IActionResult> Index()
    {
        var currentUser = await GetCurrentUser();

        try
        {
            return Ok(_mapper.Map<IEnumerable<APIGroupDetails>>(
                await _groupRepository.GetAllAsync(AmbientContext).ToListAsync()
                ));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.ListGroupFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to list group without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
    }

    /// <summary>
    /// Get group details
    /// </summary>
    /// <remarks>
    /// Returns the details of a group.
    ///
    /// For example, with cURL
    /// 
    ///     curl --request GET \
    ///         --url http://localhost:5001/API/Group/04573fca-f1b1-48a4-b55b-b26b8c09bb9d \
    ///         --header 'Authorization: Bearer $TOKEN'
    /// 
    /// </remarks>
    /// <param name="groupId" example="7dd7bdd3-05c3-cc34-c560-8cc94664f810">The identifier of the group</param>
    /// <returns>The group</returns>
    /// <response code="201">Returns the group</response>
    /// <response code="401">Action is not authorized</response>
    [HttpGet("{groupId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIGroupDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Details(Guid groupId)
    {
        var currentUser = await GetCurrentUser();
        try
        {
            var group = await _groupRepository.GetAsync(AmbientContext, groupId);
            return Ok(_mapper.Map<APIGroupDetails>(group));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DetailsGroupFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to view details of group '{groupId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("group.id", groupId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DetailsGroupFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to view details of a non-existing group '{groupId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("group.id", groupId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }

    /// <summary>
    /// Create a group
    /// </summary>
    /// <remarks>
    /// Creates a new group
    ///
    /// For example, with cURL
    /// 
    ///     curl --request POST \
    ///       --url http://localhost:5001/API/Group \
    ///       --header 'Authorization: Bearer $TOKEN' \
    ///       --header 'Content-Type: application/json' \
    ///       --data '{
    ///       "name": "Fight Club",
    ///       "description": "<p>The fight club</p>",
    ///       "default": false,
    ///       "hidden": true,
    ///       "parentGroupId": null
    ///     }'
    /// 
    /// </remarks>
    /// <param name="submittedGroup">The group to create</param>
    /// <returns>The created group</returns>
    /// <response code="200">Returns the newly created group</response>
    /// <response code="400">The provided data are invalid (e.g. empty title, non-existing parent group, etc.)</response>
    /// <response code="401">Action is not authorized</response>
    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(APIGroupDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> Create([FromBody] APIGroup submittedGroup)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            if (ModelState.IsValid)
            {
                var group = new Group
                {
                    Name = submittedGroup.Name,
                    ParentGroupId = submittedGroup.ParentGroupId,
                    Hidden = submittedGroup.Hidden,
                    Description = submittedGroup.Description,
                    Default = submittedGroup.Default
                };
                
                await _groupRepository.AddAsync(AmbientContext, group);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information, EventIDs.CreateGroupSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully created a new group '{submittedGroup.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddGroup(group),
                    null,
                    LogEvent.Formatter);

                return Ok(_mapper.Map<APIGroupDetails>(group));
            }

            return BadRequest(ModelState);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.CreateGroupFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to create a new group '{submittedGroup.Name}' without legitimate rights.")
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
                EventIDs.CreateGroupFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to create a new group with an invalid model.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return BadRequest(ModelState);
        }
    }

    /// <summary>
    /// Update a group
    /// </summary>
    /// <remarks>
    /// Updates the group specified in the route with the provided body.
    ///
    /// For example, with cURL
    ///
    ///     curl --request PATCH \
    ///       --url http://localhost:5001/API/Group/f740b67b-4c2e-4d78-81e2-399f5449412e \
    ///       --header 'Authorization: Bearer $TOKEN' \
    ///       --header 'Content-Type: application/json' \
    ///       --data '{
    ///       "name": "Fight Club II",
    ///       "description": "<p>The second fight club</p>",
    ///       "default": false,
    ///       "hidden": true,
    ///       "parentGroupId": null
    ///     }'
    ///
    /// </remarks>
    /// <param name="groupId" example="f740b67b-4c2e-4d78-81e2-399f5449412e">The identifier of the group to update</param>
    /// <param name="submittedGroup">The updated group</param>
    /// <returns>The updated group</returns>
    /// <response code="200">Returns the updated group</response>
    /// <response code="400">The provided data are invalid (e.g. empty title, non-existing parent group, etc.)</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="404">The group does not exists</response>
    [HttpPatch("{groupId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(APIGroupDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Edit([FromRoute] Guid groupId, [FromBody] APIGroup submittedGroup)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var group = await _groupRepository.GetAsync(AmbientContext, groupId);
            
            if (ModelState.IsValid)
            {
                group.Name = submittedGroup.Name;
                group.ParentGroupId = submittedGroup.ParentGroupId;
                group.Description = submittedGroup.Description;
                group.Hidden = submittedGroup.Hidden;
                group.Default = submittedGroup.Default;
                
                await _groupRepository.UpdateAsync(AmbientContext, group);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information, EventIDs.EditGroupSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully edited group '{group.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddGroup(group),
                    null,
                    LogEvent.Formatter);

                return Ok(_mapper.Map<APIGroup>(group));
            }

            throw new InvalidArgumentException(ModelState);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.EditGroupFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit group '{groupId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("group.id", groupId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.EditGroupFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit a non-existing group '{groupId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("group.id", groupId),
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
                EventIDs.EditGroupFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit group '{groupId}' with an invalid model.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("group.id", groupId),
                null,
                LogEvent.Formatter);

            return BadRequest(ModelState);
        }
    }

    /// <summary>
    /// Delete a group
    /// </summary>
    /// <remarks>
    /// Deletes the group specified in the route.
    ///
    ///     curl --request DELETE \
    ///       --url http://localhost:5001/API/Group/8cdb94c2-f24e-4e04-bfa7-b6f13bdd7fe9 \
    ///       --header 'Authorization: Bearer $TOKEN' \
    /// 
    /// </remarks>
    /// <param name="groupId" example="f740b67b-4c2e-4d78-81e2-399f5449412e">The group identifier</param>
    /// <response code="200">Returns the updated group</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="404">The group does not exists</response>
    [HttpDelete("{groupId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Delete(Guid groupId)
    {
        var currentUser = await GetCurrentUser();
        try
        {
            await _groupRepository.RemoveAsync(AmbientContext, groupId);
            await _context.SaveChangesAsync();

            _logger.Log(LogLevel.Information,
                EventIDs.DeleteGroupSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully deleted group '{groupId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("group.id", groupId),
                null,
                LogEvent.Formatter);

            return Ok();
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DeleteGroupFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to delete a new group '{groupId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("group.id", groupId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DeleteGroupFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to delete a non-existing group '{groupId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("group.id", groupId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }
}