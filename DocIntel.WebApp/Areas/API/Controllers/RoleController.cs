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
using DocIntel.Core.Authorization.Operations;
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
/// Roles in DocIntel controls the actions a user can perform. A user can belong to many roles and his permissions
/// are the union of the permissions in the roles.
/// 
/// ## Group Attributes
///
/// | Attribute        | Description                                           |
/// |------------------|-------------------------------------------------------|
/// | RoleId           | The identifier                                        |
/// | Name             | The name                                              |
/// | Description      | The description                                       |
/// | Permissions      | An array of string indicating permissions             |
/// | Slug             | The slug generated for URLs                           |
/// | CreatedById      | The identifier of the user who created the role       |
/// | LastModifiedById | The identifier of the user who last modified the role |
/// | CreationDate     | The creation date                                     |
/// | ModificationDate | The last modification date                            |
/// 
/// ## Group Relationship
///
/// | Attribute      | Description                         |
/// |----------------|-------------------------------------|
/// | Users          | The users assigned the role         |
/// | CreatedBy      | The user who created the role       |
/// | LastModifiedBy | The user who last modified the role |
/// 
/// </summary>
[Area("API")]
[Route("API/Role")]
[ApiController]
public class RoleController : DocIntelAPIControllerBase
{
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IRoleRepository _roleRepository;

    public RoleController(UserManager<AppUser> userManager,
        DocIntelContext context,
        ILogger<RoleController> logger,
        IRoleRepository roleRepository,
        IHttpContextAccessor accessor,
        IMapper mapper)
        : base(userManager, context)
    {
        _logger = logger;
        _roleRepository = roleRepository;
        _accessor = accessor;
        _mapper = mapper;
    }

    /// <summary>
    /// Get roles
    /// </summary>
    /// <remarks>
    /// Returns all roles. 
    ///
    /// For example, with cURL
    /// 
    ///     curl --request GET \
    ///         --url http://localhost:5001/API/Role \
    ///         --header 'Authorization: Bearer $TOKEN'
    /// 
    /// </remarks>
    /// <returns>The roles</returns>
    /// <response code="200">Returns the role</response>
    [HttpGet("")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<APIRoleDetails>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Produces("application/json")]
    public async Task<IActionResult> Index()
    {
        var currentUser = await GetCurrentUser();

        try
        {
            return Ok(_mapper.Map<IEnumerable<APIRoleDetails>>(
                await _roleRepository.GetAllAsync(AmbientContext).ToListAsync()
                ));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.ListRoleFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to list role without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
    }

    /// <summary>
    /// Get role details
    /// </summary>
    /// <remarks>
    /// Returns the details of a role.
    ///
    /// For example, with cURL
    /// 
    ///     curl --request GET \
    ///         --url http://localhost:5001/API/Role/04573fca-f1b1-48a4-b55b-b26b8c09bb9d \
    ///         --header 'Authorization: Bearer $TOKEN'
    /// 
    /// </remarks>
    /// <param name="roleId" example="7dd7bdd3-05c3-cc34-c560-8cc94664f810">The identifier of the role</param>
    /// <returns>The role</returns>
    /// <response code="201">Returns the role</response>
    /// <response code="401">Action is not authorized</response>
    [HttpGet("{roleId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIRoleDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Details(string roleId)
    {
        var currentUser = await GetCurrentUser();
        try
        {
            var role = await _roleRepository.GetAsync(AmbientContext, roleId);
            return Ok(_mapper.Map<APIRoleDetails>(role));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DetailsRoleFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to view details of role '{roleId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("role.id", roleId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DetailsRoleFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to view details of a non-existing role '{roleId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("role.id", roleId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }

    /// <summary>
    /// Create a role
    /// </summary>
    /// <remarks>
    /// Creates a new role
    ///
    /// For example, with cURL
    /// 
    ///     curl --request POST \
    ///       --url http://localhost:5001/API/Group \
    ///       --header 'Authorization: Bearer $TOKEN' \
    ///       --header 'Content-Type: application/json' \
    ///       --data '{
    ///           "name": "Fight Club",
    ///           "description": "<p>The fight club</p>",
    ///           "default": false,
    ///           "hidden": true,
    ///           "parentGroupId": null
    ///         }'
    /// 
    /// </remarks>
    /// <param name="submittedRole">The role to create</param>
    /// <returns>The created role</returns>
    /// <response code="200">Returns the newly created role</response>
    /// <response code="400">The provided data are invalid (e.g. empty title, non-existing parent role, etc.)</response>
    /// <response code="401">Action is not authorized</response>
    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(APIRoleDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> Create([FromBody] APIRole submittedRole)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            if (ModelState.IsValid)
            {
                var role = new AppRole
                {
                    Name = submittedRole.Name,
                    Description = submittedRole.Description
                };

                SetPermissions(role, submittedRole.Permissions);
                await _roleRepository.AddAsync(AmbientContext, role);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information, EventIDs.CreateRoleSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully created a new role '{submittedRole.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddRole(role),
                    null,
                    LogEvent.Formatter);

                return Ok(_mapper.Map<APIRoleDetails>(role));
            }

            return BadRequest(ModelState);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.CreateRoleFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to create a new role '{submittedRole.Name}' without legitimate rights.")
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
                EventIDs.CreateRoleFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to create a new role with an invalid model.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return BadRequest(ModelState);
        }
    }

    /// <summary>
    /// Update a role
    /// </summary>
    /// <remarks>
    /// Updates the role specified in the route with the provided body.
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
    /// <param name="roleId" example="f740b67b-4c2e-4d78-81e2-399f5449412e">The identifier of the role to update</param>
    /// <param name="submittedRole">The updated role</param>
    /// <returns>The updated role</returns>
    /// <response code="200">Returns the updated role</response>
    /// <response code="400">The provided data are invalid (e.g. empty title, non-existing parent role, etc.)</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="404">The role does not exists</response>
    [HttpPatch("{roleId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(APIRoleDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Edit([FromRoute] string roleId, [FromBody] APIRole submittedRole)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var role = await _roleRepository.GetAsync(AmbientContext, roleId);
            
            if (ModelState.IsValid)
            {
                role.Name = submittedRole.Name;
                role.Description = submittedRole.Description;
                SetPermissions(role, submittedRole.Permissions);
                await _roleRepository.UpdateAsync(AmbientContext, role);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information, EventIDs.EditRoleSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully edited role '{role.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddRole(role),
                    null,
                    LogEvent.Formatter);

                return Ok(_mapper.Map<APIRole>(role));
            }

            throw new InvalidArgumentException(ModelState);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.EditRoleFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit role '{roleId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("role.id", roleId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.EditRoleFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit a non-existing role '{roleId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("role.id", roleId),
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
                EventIDs.EditRoleFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit role '{roleId}' with an invalid model.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("role.id", roleId),
                null,
                LogEvent.Formatter);

            return BadRequest(ModelState);
        }
    }

    /// <summary>
    /// Delete a role
    /// </summary>
    /// <remarks>
    /// Deletes the role specified in the route.
    ///
    ///     curl --request DELETE \
    ///       --url http://localhost:5001/API/Role/8cdb94c2-f24e-4e04-bfa7-b6f13bdd7fe9 \
    ///       --header 'Authorization: Bearer $TOKEN' \
    /// 
    /// </remarks>
    /// <param name="roleId" example="f740b67b-4c2e-4d78-81e2-399f5449412e">The role identifier</param>
    /// <response code="200">Returns the updated role</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="404">The role does not exists</response>
    [HttpDelete("{roleId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Delete(string roleId)
    {
        var currentUser = await GetCurrentUser();
        try
        {
            await _roleRepository.RemoveAsync(AmbientContext, roleId);
            await _context.SaveChangesAsync();

            _logger.Log(LogLevel.Information,
                EventIDs.DeleteRoleSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully deleted role '{roleId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("role.id", roleId),
                null,
                LogEvent.Formatter);

            return Ok();
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DeleteRoleFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to delete a new role '{roleId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("role.id", roleId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DeleteRoleFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to delete a non-existing role '{roleId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("role.id", roleId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }
    

    private void SetPermissions(AppRole role, IEnumerable<string> permissions)
    {
        // Get possible permissions using reflection.
        var type = typeof(IOperationConstants);
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => type.IsAssignableFrom(p)).ToArray();
        var possiblePermissions = types
            .SelectMany(t => t.GetFields().Where(f => f.IsPublic).Select(x => (string) x.GetValue(null)))
            .Union(types.SelectMany(t => t.GetProperties().Select(x => (string) x.GetValue(null))));

        role.Permissions = permissions.Intersect(possiblePermissions).ToArray();
    }
}