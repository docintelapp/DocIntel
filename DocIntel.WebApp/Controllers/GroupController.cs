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
using System.Linq;
using System.Threading.Tasks;
using DocIntel.Core.Authentication;
using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Helpers;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;
using DocIntel.WebApp.Helpers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DocIntel.WebApp.Controllers
{
    /// <summary>
    ///     Provides functionalities for managing groups. Groups can be assigned to
    ///     users (a user can have multiple groups). A user is also a member of all
    ///     the parent groups of groups he is a member.
    /// </summary>
    public class GroupController : BaseController
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IAppAuthorizationService _appAuthorizationService;
        private readonly ApplicationSettings _appSettings;
        private readonly IGroupRepository _groupRepository;
        private readonly ILogger _logger;
        private readonly IUserRepository _userRepository;

        public GroupController(IAppAuthorizationService appAuthorizationService,
            IGroupRepository groupRepository,
            UserManager<AppUser> userManager,
            ApplicationSettings configuration,
            ILogger<GroupController> logger,
            DocIntelContext context,
            IUserRepository userRepository,
            IAuthorizationService authorizationService,
            IHttpContextAccessor accessor, ApplicationSettings appSettings)
            : base(context,
                userManager,
                configuration,
                authorizationService)
        {
            _logger = logger;
            _appAuthorizationService = appAuthorizationService;
            _groupRepository = groupRepository;
            _userRepository = userRepository;
            _accessor = accessor;
            _appSettings = appSettings;
        }

        /// <summary>
        ///     Provides the listing of groups
        /// </summary>
        /// <returns>
        ///     A view with the listing of the groups. A "Unauthorized" response
        ///     if the user is not allowed to view the results.
        /// </returns>
        public async Task<IActionResult> Index()
        {
            var currentUser = await GetCurrentUser();
            try
            {
                _logger.Log(LogLevel.Information,
                    EventIDs.ListGroupSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully list the groups.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                ViewBag.DefaultGroup = _groupRepository.GetDefaultGroups(AmbientContext);

                return View(_groupRepository
                    .GetAllAsync(AmbientContext, includeRelatedData: new[] {"Members", "ParentGroup"}).ToEnumerable());
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.ListGroupFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to list groups without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
        }

        [HttpGet("Group/Details/{id}")]
        public async Task<IActionResult> Details(Guid id)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var group = await _groupRepository.GetAsync(AmbientContext,
                    id,
                    new[] {"Members", "Members.User", "ParentGroup"});

                _logger.Log(LogLevel.Information,
                    EventIDs.DetailsGroupSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully viewed details of '{group.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddGroup(group),
                    null,
                    LogEvent.Formatter);

                ViewBag.AllUsers = _userManager.Users.Except(group.Members.Select(_ => _.User), _ => _.Id).ToList();

                return View(group);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DetailsGroupFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view details of group '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("group.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DetailsGroupFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view details of a non-existing group '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("group.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        public async Task<IActionResult> Create()
        {
            var currentUser = await GetCurrentUser();

            if (!await _appAuthorizationService.CanAddGroup(User, null))
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.CreateGroupFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new group without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            ViewBag.AllGroups = await _groupRepository.GetAllAsync(AmbientContext).ToListAsync();

            return View(new Group());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name", "ParentGroupId", "Description", "Hidden", "Default")]
            Group viewModel)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                if (ModelState.IsValid)
                {
                    var group = new Group
                    {
                        Name = viewModel.Name,
                        ParentGroupId = viewModel.ParentGroupId,
                        Hidden = viewModel.Hidden,
                        Description = viewModel.Description,
                        Default = viewModel.Default
                    };

                    await _groupRepository.AddAsync(AmbientContext, group);
                    await _context.SaveChangesAsync();

                    _logger.Log(LogLevel.Information,
                        EventIDs.CreateGroupSuccessful,
                        new LogEvent(
                                $"User '{currentUser.UserName}' successfully created a new group '{viewModel.Name}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddGroup(group),
                        null,
                        LogEvent.Formatter);

                    return RedirectToAction(nameof(Index));
                }

                throw new InvalidArgumentException(ModelState);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.CreateGroupFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new group '{viewModel.Name}' without legitimate rights.")
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
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new group '{viewModel.Name}' with an invalid model.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddGroup(viewModel),
                    null,
                    LogEvent.Formatter);

                ViewBag.AllGroups = await _groupRepository.GetAllAsync(AmbientContext).ToListAsync();
                return View(viewModel);
            }
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var group = await _groupRepository.GetAsync(AmbientContext,
                    id,
                    new[] {"Members", "Members.User"});

                _logger.Log(LogLevel.Information,
                    EventIDs.EditGroupSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully displayed form for editing '{group.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                ViewBag.AllGroups = await _groupRepository.GetAllAsync(AmbientContext).Where(_ => _.GroupId != id)
                    .ToListAsync();

                return View(group);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditGroupFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to display form for editing group '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("group.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditGroupFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to display form for editing a non-existing group '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("group.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            [Bind("GroupId", "Name", "ParentGroupId", "Description", "Hidden", "Default")]
            Group viewModel)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var group = await _groupRepository.GetAsync(AmbientContext,
                    viewModel.GroupId);

                if (ModelState.IsValid)
                {
                    group.Name = viewModel.Name;
                    group.ParentGroupId = viewModel.ParentGroupId;
                    group.Description = viewModel.Description;
                    group.Hidden = viewModel.Hidden;
                    group.Default = viewModel.Default;

                    await _groupRepository.UpdateAsync(AmbientContext, group);
                    await _context.SaveChangesAsync();

                    _logger.Log(LogLevel.Information,
                        EventIDs.EditGroupSuccessful,
                        new LogEvent($"User '{currentUser.UserName}' successfully edited group '{viewModel.GroupId}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddGroup(group),
                        null,
                        LogEvent.Formatter);

                    return RedirectToAction(nameof(Index));
                }

                throw new InvalidArgumentException(ModelState);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditGroupFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit group '{viewModel.GroupId}' without legitimate rights.")
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
                    EventIDs.EditGroupFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit group '{viewModel.GroupId}' with an invalid model.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddGroup(viewModel),
                    null,
                    LogEvent.Formatter);
                ViewBag.AllGroups = await _groupRepository.GetAllAsync(AmbientContext)
                    .Where(_ => _.GroupId != viewModel.GroupId).ToListAsync();

                return View(viewModel);
            }
        }

        [HttpGet("Group/Delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var group = await _groupRepository.GetAsync(AmbientContext,
                    id,
                    new[] {"Members", "Members.User"});

                _logger.Log(LogLevel.Information,
                    EventIDs.DeleteGroupSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully requested to delete '{group.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return View(group);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DeleteGroupFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to request delete group '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("group.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DeleteGroupFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to request delete a non-existing group '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("group.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpPost("Group/Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, [Bind("GroupId")] Group viewModel)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var group = await _groupRepository.GetAsync(AmbientContext,
                    id);

                await _groupRepository.RemoveAsync(AmbientContext, id);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.DeleteGroupSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully created a new group '{viewModel.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddGroup(group),
                    null,
                    LogEvent.Formatter);

                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DeleteGroupFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to delete group '{viewModel.GroupId}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("group.id", viewModel.GroupId),
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
                    EventIDs.DeleteGroupFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to delete group '{viewModel.GroupId}' with an invalid model.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("group.id", viewModel.GroupId),
                    null,
                    LogEvent.Formatter);

                return View(viewModel);
            }
        }

        [HttpPost("/Group/AddMember")]
        public async Task<IActionResult> AddMember(Guid groupId, string userId)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var user = await _userRepository.GetById(AmbientContext,
                    userId,
                    new[] {nameof(AppUser.Memberships), nameof(AppUser.Memberships) + "." + nameof(Member.Group)});
                var group = await _groupRepository.GetAsync(AmbientContext,
                    groupId);
                if (user == default || group == default)
                    return NotFound();

                if (user.Memberships.Any(_ => _.GroupId == groupId)) return RedirectToAction();

                await _groupRepository.AddUserToGroupAsync(AmbientContext, user.Id, group.GroupId);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.AddGroupUserSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully added user '{user.UserName}' to  group '{group.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddGroup(group),
                    null,
                    LogEvent.Formatter);

                return RedirectToAction(nameof(Details), new {id = groupId});
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.AddGroupUserFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to add user '{userId}' to  group '{groupId}' without legitimate rights.")
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
                    EventIDs.AddGroupUserFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to add user '{userId}' to  group '{groupId}' with an invalid model.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("group.id", groupId),
                    null,
                    LogEvent.Formatter);

                return View();
            }
        }

        [HttpGet("/Group/RemoveMember")]
        public async Task<IActionResult> RemoveMember(Guid groupId, string userId)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var user = await _userRepository.GetById(AmbientContext,
                    userId,
                    new[] {nameof(AppUser.Memberships), nameof(AppUser.Memberships) + "." + nameof(Member.Group)});
                var group = await _groupRepository.GetAsync(AmbientContext,
                    groupId);
                if (user == default || group == default)
                    return NotFound();

                if (user.Memberships.All(_ => _.GroupId != groupId)) return RedirectToAction();

                await _groupRepository.RemoveUserFromGroupAsync(AmbientContext, user.Id, group.GroupId);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.CreateGroupSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully removed user '{user.UserName}' to  group '{group.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddGroup(group),
                    null,
                    LogEvent.Formatter);

                return RedirectToAction(nameof(Details), new {id = groupId});
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.CreateGroupFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to add user '{userId}' to  group '{groupId}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("group.id", groupId),
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
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to add user '{userId}' to  group '{groupId}' with an invalid model.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("group.id", groupId),
                    null,
                    LogEvent.Formatter);

                return View();
            }
        }
    }
}