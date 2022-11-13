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
using System.Linq;
using System.Threading.Tasks;

using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.Search.Documents;
using DocIntel.WebApp.Helpers;
using DocIntel.WebApp.ViewModels.UserViewModel;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocIntel.WebApp.Controllers
{
    public class UserController : BaseController
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IAppAuthorizationService _appAuthorizationService;
        private readonly IGroupRepository _groupRepository;
        private readonly ILogger _logger;
        private readonly IUserRepository _userRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly IOptions<IdentityOptions> _identityOptions;

        public UserController(
            UserManager<AppUser> userManager,
            ApplicationSettings configuration,
            ILogger<UserController> logger,
            DocIntelContext context,
            IAuthorizationService authorizationService,
            IUserRepository userRepository,
            IAppAuthorizationService appAuthorizationService,
            IHttpContextAccessor accessor, IGroupRepository groupRepository, IDocumentRepository documentRepository, IOptions<IdentityOptions> identityOptions)
            : base(context,
                userManager,
                configuration,
                authorizationService)
        {
            _logger = logger;
            _userRepository = userRepository;
            _appAuthorizationService = appAuthorizationService;
            _accessor = accessor;
            _groupRepository = groupRepository;
            _documentRepository = documentRepository;
            _identityOptions = identityOptions;
        }

        public async Task<IActionResult> Index(int page = 1, int limit = 50)
        {
            var currentUser = await GetCurrentUser();

            var query = new UserQuery {Page = page, Limit = limit};
            var users = _userRepository
                .GetAllAsync(AmbientContext, query)
                .ToEnumerable()
                .OrderBy(_ => _.UserName);

            var viewModel = new IndexViewModel
            {
                Users = users,
                Page = page,
                PageCount = (int) Math.Ceiling(await _userRepository.CountAsync(AmbientContext) / (limit * 1f))
            };

            _logger.Log(LogLevel.Information,
                EventIDs.ListUserSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully listed users.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return View(viewModel);
        }

        [HttpGet("User/Profile/{username}")]
        public async Task<IActionResult> Profile(string username)
        {
            var currentUser = await GetCurrentUser();
            var user = await _context.Users.Include(_ => _.Memberships).ThenInclude(_ => _.Group).AsQueryable()
                .SingleOrDefaultAsync(m => m.NormalizedUserName == username.ToUpperInvariant());
            if (string.IsNullOrEmpty(username) || user == null)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.ProfileUserFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to view the profile of a non-existing user.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }

            if (!await _appAuthorizationService.CanViewUser(User, user))
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.ProfileUserFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view user profile '{user.FriendlyName}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddUser(user, "user_profile"),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            var documents = _documentRepository.GetAllAsync(AmbientContext,
                    new DocumentQuery() { RegisteredBy = user.Id, OrderBy = SortCriteria.DocumentDate, Limit = 10},
                    new[] {"DocumentTags", "DocumentTags.Tag", "DocumentTags.Tag.Facet", "Source"})
                .ToEnumerable();

            // TODO Remove view model
            var viewModel = new ProfileViewModel();
            viewModel.User = user;
            viewModel.RegisteredDocuments = documents;
            return View(viewModel);
        }
        
        [HttpGet("User/ResetPassword/{username}")]
        public async Task<IActionResult> ResetPassword(string username)
        {
            var currentUser = await GetCurrentUser();
            var user = await _context.Users.Include(_ => _.Memberships).ThenInclude(_ => _.Group).AsQueryable()
                .SingleOrDefaultAsync(m => m.NormalizedUserName == username.ToUpperInvariant());
            if (string.IsNullOrEmpty(username) || user == null)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditUserFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to change the password of a non-existing user.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }

            if (!await _appAuthorizationService.CanResetPassword(User, user))
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditUserFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to change the password for '{user.FriendlyName}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddUser(user, "user_password"),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            _logger.Log(LogLevel.Information,
                EventIDs.EditUserSuccessful,
                new LogEvent(
                        $"User '{currentUser.UserName}' successfully requested to change the password of the user '{user.FriendlyName}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddUser(user, "user_password"),
                null,
                LogEvent.Formatter);

            return View(new ResetPasswordViewModel()
            {
                UserId = user.Id,
                User = user,
                PasswordOptions = _identityOptions.Value.Password
            });
        }
        
        [HttpPost("User/ResetPassword/{username}")]
        public async Task<IActionResult> ResetPassword(
            string username,
            [Bind("UserId", "Password", "PasswordConfirmation")]
            ResetPasswordViewModel submittedUser)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var user = await _userRepository.GetById(AmbientContext, submittedUser.UserId);

                if (!await _appAuthorizationService.CanResetPassword(User, user))
                {
                    _logger.Log(LogLevel.Warning,
                        EventIDs.EditUserFailed,
                        new LogEvent(
                                $"User '{currentUser.UserName}' attempted to change the password for '{user.FriendlyName}' without legitimate rights.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddUser(user, "user_password"),
                        null,
                        LogEvent.Formatter);

                    return Unauthorized();
                }

                if (ModelState.IsValid)
                {
                    var resetToken = await _userManager
                        .GeneratePasswordResetTokenAsync(user);

                    var passwordChangeResult
                        = await _userManager.ResetPasswordAsync(user,
                            resetToken,
                            submittedUser.Password);
                    await _context.SaveChangesAsync();

                    if (passwordChangeResult.Succeeded)
                    {
                        _logger.Log(LogLevel.Information,
                            EventIDs.PasswordChangeSuccessful,
                            new LogEvent($"User '{currentUser.UserName}' successfully changed password for user '{user.FriendlyName}'.")
                                .AddUser(currentUser)
                                .AddHttpContext(_accessor.HttpContext)
                                .AddUser(user),
                            null,
                            LogEvent.Formatter);
                    }
                    else
                    {
                        _logger.Log(LogLevel.Information,
                            EventIDs.PasswordChangeFailed,
                            new LogEvent($"User '{currentUser.UserName}' failed to change password for user '{user.FriendlyName}'.")
                                .AddUser(currentUser)
                                .AddHttpContext(_accessor.HttpContext)
                                .AddUser(user),
                            null,
                            LogEvent.Formatter);
                    }

                    return RedirectToAction(nameof(Profile), new { username = username });
                }

                throw new InvalidArgumentException(ModelState);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditUserFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit user '{submittedUser.UserId}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("user.id", submittedUser.UserId),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditUserFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit a non-existing user '{submittedUser.UserId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("user.id", submittedUser.UserId),
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
                    EventIDs.EditUserFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit user '{submittedUser.UserId}' with an invalid model.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("user.id", submittedUser.UserId),
                    null,
                    LogEvent.Formatter);

                return View(submittedUser);
            }
        }

        [HttpGet("User/Edit/{username}")]
        public async Task<IActionResult> Edit(string username)
        {
            var currentUser = await GetCurrentUser();
            var user = await _context.Users.Include(_ => _.Memberships).ThenInclude(_ => _.Group).AsQueryable()
                .SingleOrDefaultAsync(m => m.NormalizedUserName == username.ToUpperInvariant());
            if (string.IsNullOrEmpty(username) || user == null)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditUserFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to view the profile of a non-existing user.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }

            if (!await _appAuthorizationService.CanEditUser(User, user))
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditUserFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit user '{user.FriendlyName}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddUser(user, "user_profile"),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            _logger.Log(LogLevel.Information,
                EventIDs.EditUserSuccessful,
                new LogEvent(
                        $"User '{currentUser.UserName}' successfully requested to edit user '{user.FriendlyName}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddUser(user, "user_profile"),
                null,
                LogEvent.Formatter);

            ViewBag.AllGroups = await _groupRepository.GetAllAsync(AmbientContext).ToListAsync();

            return View(user);
        }

        [HttpPost("User/Edit/{username}")]
        public async Task<IActionResult> Edit(
            string username,
            [Bind(nameof(AppUser.Id), nameof(AppUser.FirstName), nameof(AppUser.LastName), nameof(AppUser.Email),
                nameof(AppUser.UserName), nameof(AppUser.Enabled), nameof(AppUser.Bot))]
            AppUser submittedUser,
            [Bind("groups")] Guid[] groups)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var user = await _userRepository.GetById(AmbientContext, submittedUser.Id);

                if (ModelState.IsValid)
                {
                    user.UserName = submittedUser.UserName;
                    user.FirstName = submittedUser.FirstName;
                    user.LastName = submittedUser.LastName;
                    user.Email = submittedUser.Email;
                    user.Enabled = submittedUser.Enabled;
                    user.Bot = submittedUser.Bot;
                    user.Function = submittedUser.Function;

                    var filteredGroups = groups
                        .ToAsyncEnumerable()
                        .SelectAwait(async _ =>
                        {
                            try
                            {
                                return await _groupRepository.GetAsync(AmbientContext, _, currentUser);
                            }
                            catch (NotFoundEntityException)
                            {
                            }
                            catch (UnauthorizedOperationException)
                            {
                            }

                            return default;
                        })
                        .Where(_ => _ != null);

                    await _userRepository.Update(AmbientContext, user, await filteredGroups.ToArrayAsync());
                    await _context.SaveChangesAsync();

                    _logger.Log(LogLevel.Information,
                        EventIDs.EditUserSuccessful,
                        new LogEvent($"User '{currentUser.UserName}' successfully edit user '{user.FriendlyName}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddUser(user),
                        null,
                        LogEvent.Formatter);

                    return RedirectToAction(nameof(Index));
                }

                throw new InvalidArgumentException(ModelState);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditUserFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit user '{submittedUser.Id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("user.id", submittedUser.Id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditUserFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit a non-existing user '{submittedUser.Id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("user.id", submittedUser.Id),
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
                    EventIDs.EditUserFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit user '{submittedUser.Id}' with an invalid model.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("user.id", submittedUser.Id),
                    null,
                    LogEvent.Formatter);

                return View(submittedUser);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var currentUser = await GetCurrentUser();
            if (!await _appAuthorizationService.CanCreateUser(User, null))
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.ProfileUserFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new user without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            ViewBag.AllGroups = await _groupRepository.GetAllAsync(AmbientContext).ToListAsync();

            return View(new AppUser());
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [Bind("UserName", "FirstName", "LastName", "Email", "Enabled", "Bot")]
            AppUser viewModel,
            [Bind("groups")] Guid[] groups
        )
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var user = new AppUser
                {
                    UserName = viewModel.UserName,
                    FirstName = viewModel.FirstName,
                    LastName = viewModel.LastName,
                    Email = viewModel.Email,
                    Enabled = viewModel.Enabled,
                    Bot = viewModel.Bot,
                    Function = viewModel.Function
                };

                var filteredGroups = groups
                    .ToAsyncEnumerable()
                    .SelectAwait(async _ =>
                    {
                        try
                        {
                            return await _groupRepository.GetAsync(AmbientContext, _, currentUser);
                        }
                        catch (NotFoundEntityException)
                        {
                        }
                        catch (UnauthorizedOperationException)
                        {
                        }

                        return default;
                    })
                    .Where(_ => _ != null);

                await _userManager.CreateAsync(user);
                // TODO , groups: await filteredGroups.ToArrayAsync()
                await AmbientContext.DatabaseContext.SaveChangesAsync();

                return RedirectToAction(nameof(ResetPassword), new { username = user.UserName });
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.ProfileUserFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new user '{viewModel.UserName}' without legitimate rights.")
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

                return View(viewModel);
            }
        }

        public JsonResult Search(string q)
        {
            var normalizedQ = q.ToUpperInvariant();

            var users = _context.Users.AsQueryable()
                .Where(x => x.NormalizedUserName.StartsWith(normalizedQ))
                .Select(x =>
                    new
                    {
                        x.Id,
                        x.UserName,
                        DisplayName = string.IsNullOrEmpty(x.FirstName) & string.IsNullOrEmpty(x.LastName)
                            ? x.UserName
                            : string.Format("{0} {1} <span class=\"text-muted\">({2})</span>", x.FirstName, x.LastName,
                                x.UserName)
                    }
                );

            return base.Json(users);
        }
    }
}