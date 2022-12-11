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
using DocIntel.Core.Authentication;
using DocIntel.Core.Authorization;
using DocIntel.Core.Authorization.Operations;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;
using DocIntel.WebApp.Helpers;
using DocIntel.WebApp.ViewModels.RoleViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DocIntel.WebApp.Controllers
{
    /// <summary>
    ///     Provides functionalities for managing roles. Roles can be assigned to
    ///     users (a user can have multiple roles) and are "containers" for
    ///     permissions. The permissions of a user are then the union of all
    ///     permissions of the roles he is assigned.
    /// </summary>
    public class RoleController : BaseController
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IAppAuthorizationService _appAuthorizationService;
        private readonly ILogger _logger;
        private readonly AppRoleManager _roleManager;
        private readonly IUserRepository _userRepository;

        public RoleController(IAppAuthorizationService appAuthorizationService,
            AppRoleManager roleManager,
            AppUserManager userManager,
            ApplicationSettings configuration,
            ILogger<RoleController> logger,
            DocIntelContext context,
            IUserRepository userRepository,
            IAuthorizationService authorizationService,
            IHttpContextAccessor accessor)
            : base(context,
                userManager,
                configuration,
                authorizationService)
        {
            _logger = logger;
            _appAuthorizationService = appAuthorizationService;
            _roleManager = roleManager;
            _userRepository = userRepository;
            _accessor = accessor;
        }

        /// <summary>
        ///     Provides the listing of roles
        /// </summary>
        /// <returns>
        ///     A view with the listing of the roles. A "Unauthorized" response
        ///     if the user is not allowed to view the results.
        /// </returns>
        public async Task<IActionResult> Index()
        {
            var currentUser = await GetCurrentUser();
            try
            {
                _logger.Log(LogLevel.Information, EventIDs.ListRoleSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully list the roles.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return View(_roleManager.Roles.ToList());
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.ListRoleFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to list roles without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
        }

        [HttpGet("Role/Details/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var role = await _roleManager.FindByIdAsync(id);
                if (role == null)
                {
                    _logger.Log(LogLevel.Warning, EventIDs.DetailsRoleFailed,
                        new LogEvent(
                                $"User '{currentUser.UserName}' attempted to view details of a non-existing role '{id}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddProperty("role.id", id),
                        null,
                        LogEvent.Formatter);

                    return NotFound();
                }

                var viewModel = new DetailsViewModel
                {
                    Role = role,
                    Users = await _userManager.GetUsersInRoleAsync(role.Name),
                    Permissions = await _roleManager.GetPermissionsAsync(role),
                    AllUsers = _userRepository.GetAllAsync(AmbientContext).ToEnumerable()
                };
                
                _logger.Log(LogLevel.Information, EventIDs.DetailsRoleSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully viewed details of '{role.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);
        
                await SetupViewBag(role);

                return View(viewModel);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.DetailsRoleFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view details of role '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("role.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
        }

        private async Task SetupViewBag(AppRole role)
        {
            var userRoles = (await _userManager.GetUsersInRoleAsync(role.Name)).Select(user => user.Id).ToArray();
            ViewBag.AllUsers = await _userRepository.GetAllAsync(AmbientContext)
                .Where(_ => !userRoles.Contains(_.Id))
                .ToArrayAsync();
        }

        public async Task<IActionResult> Create()
        {
            var currentUser = await GetCurrentUser();

            if (!await _appAuthorizationService.CanCreateRole(User, null))
            {
                _logger.Log(LogLevel.Warning, EventIDs.CreateRoleFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new role without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            return View(new AppRole(""));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name", "Description")] AppRole viewModel,
            [Bind(Prefix = "permissions")] string[] submittedPermissions)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                if (ModelState.IsValid)
                {
                    var role = new AppRole
                    {
                        Name = viewModel.Name,
                        Description = viewModel.Description,
                        CreatedById = currentUser.Id,
                        LastModifiedById = currentUser.Id
                    };

                    await _roleManager.CreateAsync(role);
                    var roleOperation = await _roleManager.CreateAsync(role);
                    if (roleOperation.Succeeded)
                    {
                        await _roleManager.SetPermissionAsync(role, submittedPermissions);
                        
                        _logger.Log(LogLevel.Information, EventIDs.CreateRoleSuccessful,
                            new LogEvent(
                                    $"User '{currentUser.UserName}' successfully created a new role '{viewModel.Name}'.")
                                .AddUser(currentUser)
                                .AddHttpContext(_accessor.HttpContext)
                                .AddRole(role),
                            null,
                            LogEvent.Formatter);

                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        _logger.Log(LogLevel.Information, EventIDs.CreateRoleFailed,
                            new LogEvent(
                                    $"User '{currentUser.UserName}' attempted to create a new role '{viewModel.Name}' with an invalid model.")
                                .AddUser(currentUser)
                                .AddHttpContext(_accessor.HttpContext)
                                .AddRole(viewModel),
                            null,
                            LogEvent.Formatter);

                        ViewBag.Permissions = submittedPermissions;
                        return View(viewModel);
                    }
                }

                throw new InvalidArgumentException(ModelState);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.CreateRoleFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new role '{viewModel.Name}' without legitimate rights.")
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

                _logger.Log(LogLevel.Information, EventIDs.CreateRoleFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new role '{viewModel.Name}' with an invalid model.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddRole(viewModel),
                    null,
                    LogEvent.Formatter);

                ViewBag.Permissions = submittedPermissions;
                return View(viewModel);
            }
        }

        public async Task<IActionResult> Edit(string id)
        {
            var currentUser = await GetCurrentUser();
            AppRole role;
            if (string.IsNullOrEmpty(id) ||
                (role = await _roleManager.FindByIdAsync(id)) == null)
            {
                _logger.Log(LogLevel.Warning, EventIDs.EditRoleFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to edit a non-existing role '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }

            if (!await _appAuthorizationService.CanEditRole(User, role))
            {
                _logger.Log(LogLevel.Warning, EventIDs.EditRoleFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit role '{role.Name}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddRole(role),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            _logger.Log(LogLevel.Information, EventIDs.EditRoleSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully requested to edit role '{role.Name}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddRole(role),
                null,
                LogEvent.Formatter);

            ViewBag.Permissions = await _roleManager.GetPermissionsAsync(role);
            return View(role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            [Bind("Id", "Name", "Description")] AppRole viewModel,
            [Bind(Prefix = "permissions")] string[] submittedPermissions)
        {
            var currentUser = await GetCurrentUser();
            AppRole role;
            if (string.IsNullOrEmpty(viewModel.Id) ||
                (role = await _roleManager.FindByIdAsync(viewModel.Id)) == null)
            {
                _logger.Log(LogLevel.Warning, EventIDs.EditRoleFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit a non-existing role '{viewModel.Id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }

            if (!await _appAuthorizationService.CanEditRole(User, role))
            {
                _logger.Log(LogLevel.Warning, EventIDs.EditRoleFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit role '{role.Name}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddRole(role),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                role.Name = viewModel.Name;
                role.Description = viewModel.Description;
                role.CreatedById = currentUser.Id;
                role.LastModifiedById = currentUser.Id;
                await _roleManager.UpdateAsync(role);
                await _roleManager.SetPermissionAsync(role, submittedPermissions);

                _logger.Log(LogLevel.Information, EventIDs.EditRoleSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully edited role '{role.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddRole(role),
                    null,
                    LogEvent.Formatter);

                return RedirectToAction(nameof(Details), new {id = role.Id});
            }

            _logger.Log(LogLevel.Information, EventIDs.EditRoleFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit role '{role.Name}' with an invalid model.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddRole(role),
                null,
                LogEvent.Formatter);

            ViewBag.Permissions = submittedPermissions;
            return View(viewModel);
        }

        [HttpGet("Role/Delete/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var currentUser = await GetCurrentUser();
            AppRole role;
            if (string.IsNullOrEmpty(id) ||
                (role = await _roleManager.FindByIdAsync(id)) == null)
            {
                _logger.Log(LogLevel.Warning, EventIDs.EditRoleFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to delete a non-existing role '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }

            if (!await _appAuthorizationService.CanDeleteRole(User, role))
            {
                _logger.Log(LogLevel.Warning, EventIDs.EditRoleFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to delete role '{role.Name}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddRole(role),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            _logger.Log(LogLevel.Information, EventIDs.DeleteRoleSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully requested to delete '{role.Name}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddRole(role),
                null,
                LogEvent.Formatter);

            return View(role);
        }

        [HttpPost("Role/Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id, [Bind("Id")] AppRole viewModel)
        {
            var currentUser = await GetCurrentUser();
            AppRole role;
            if (string.IsNullOrEmpty(id) ||
                (role = await _roleManager.FindByIdAsync(id)) == null)
            {
                _logger.Log(LogLevel.Warning, EventIDs.EditRoleFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to delete a non-existing role '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }

            if (!await _appAuthorizationService.CanDeleteRole(User, role))
            {
                _logger.Log(LogLevel.Warning, EventIDs.EditRoleFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to delete role '{role.Name}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddRole(role),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            _logger.Log(LogLevel.Information, EventIDs.DeleteRoleSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully deleted '{role.Name}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddRole(role),
                null,
                LogEvent.Formatter);

            await _roleManager.DeleteAsync(role);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> AddRole(string roleId, string userId)
        {
            var currentUser = await GetCurrentUser();
            AppRole role;
            if (string.IsNullOrEmpty(roleId) ||
                (role = await _roleManager.FindByIdAsync(roleId)) == null)
            {
                _logger.Log(LogLevel.Warning, EventIDs.AddRoleUserFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to add a non-existing role '{roleId}' to user '{userId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }

            AppUser user;
            if (string.IsNullOrEmpty(userId) ||
                (user = await _userRepository.GetById(AmbientContext, userId)) == null)
            {
                _logger.Log(LogLevel.Warning, EventIDs.AddRoleUserFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to add role '{role.Name}' to a non-existing user '{userId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }

            if (!await _appAuthorizationService.CanAddUserRole(User, user, role))
            {
                _logger.Log(LogLevel.Warning, EventIDs.AddRoleUserFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to add role '{role.Name}' to user '{userId}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddRole(role)
                        .AddUser(user, "user_for_role"),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            await _userManager.AddToRoleAsync(user, role.Name);
            await _context.SaveChangesAsync();

            _logger.Log(LogLevel.Information, EventIDs.AddRoleUserSuccessful,
                new LogEvent($"User '{currentUser.UserName}' added role '{role.Name}' to user '{userId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddRole(role)
                    .AddUser(user, "user_for_role"),
                null,
                LogEvent.Formatter);

            return RedirectToAction(nameof(Details), new {id = role.Id});
        }

        [HttpGet("/Role/RemoveUser/{roleId}/{userId}")]
        public async Task<IActionResult> RemoveRole(string roleId, string userId)
        {
            var currentUser = await GetCurrentUser();
            AppRole role;
            if (string.IsNullOrEmpty(roleId) ||
                (role = await _roleManager.FindByIdAsync(roleId)) == null)
            {
                _logger.Log(LogLevel.Warning, EventIDs.AddRoleUserFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to remove a non-existing role '{roleId}' to user '{userId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }

            AppUser user;
            if (string.IsNullOrEmpty(userId) ||
                (user = await _userRepository.GetById(AmbientContext, userId)) == null)
            {
                _logger.Log(LogLevel.Warning, EventIDs.AddRoleUserFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to remove role '{role.Name}' to a non-existing user '{userId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }

            if (!await _appAuthorizationService.CanRemoveUserRole(User, user, role))
            {
                _logger.Log(LogLevel.Warning, EventIDs.AddRoleUserFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to remove role '{role.Name}' to user '{userId}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddRole(role)
                        .AddUser(user, "user_for_role"),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            await _userManager.RemoveFromRoleAsync(user, role.Name);
            await _context.SaveChangesAsync();

            _logger.Log(LogLevel.Information, EventIDs.AddRoleUserSuccessful,
                new LogEvent($"User '{currentUser.UserName}' removed role '{role.Name}' to user '{userId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddRole(role)
                    .AddUser(user, "user_for_role"),
                null,
                LogEvent.Formatter);

            return RedirectToAction(nameof(Details), new {id = role.Id});
        }
    }
}