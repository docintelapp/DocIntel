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
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DocIntel.Core.Authentication;
using DocIntel.Core.Authorization;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils;
using DocIntel.WebApp.Helpers;
using DocIntel.WebApp.ViewModels.Account;
using DocIntel.WebApp.ViewModels.UserViewModel;

using Google.Authenticator;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

using TimeZoneNames;

using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace DocIntel.WebApp.Controllers
{
    /// <summary>
    ///     Provides the functionality related to own account management.
    /// </summary>
    public class AccountController : BaseController
    {
        private readonly IHttpContextAccessor _accessor;

        private readonly IAppAuthorizationService _appAuthorizationService;
        private readonly IDocumentRepository _documentRepository;
        private readonly ILogger _logger;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ISourceRepository _sourceRepository;
        private readonly ITagRepository _tagRepository;
        private readonly IUserRepository _userRepository;
        private readonly MailKitEmailSender _emailSender;
        private readonly ApplicationSettings _settings;

        public AccountController(
            DocIntelContext context,
            AppUserManager userManager,
            SignInManager<AppUser> signInManager,
            ApplicationSettings configuration,
            ILogger<AccountController> logger,
            IDocumentRepository documentRepository,
            ITagRepository tagRepository,
            ISourceRepository sourceRepository,
            IAuthorizationService authorizationService,
            IAppAuthorizationService appAuthorizationService,
            IHttpContextAccessor accessor, IUserRepository userRepository, MailKitEmailSender emailSender, ApplicationSettings settings)
            : base(context,
                userManager,
                configuration,
                authorizationService)
        {
            _signInManager = signInManager;
            _logger = logger;
            _accessor = accessor;

            _documentRepository = documentRepository;
            _tagRepository = tagRepository;
            _sourceRepository = sourceRepository;

            _appAuthorizationService = appAuthorizationService;
            _userRepository = userRepository;
            _emailSender = emailSender;
            _settings = settings;
        }

        /// <summary>
        ///     Provides the login page.
        /// </summary>
        /// <param name="returnUrl">
        ///     The URL to redirect the user after successful login.
        /// </param>
        /// <returns>
        ///     The view for login.
        /// </returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync();
            ViewData["ReturnUrl"] = returnUrl;
            return View(new SigninViewModel
            {
                RememberMe = true
            });
        }

        /// <summary>
        ///     Logs the specified user in, after validating credentials. The user
        ///     is then redirected to specified return url, or to the main index.
        /// </summary>
        /// <param name="model">
        ///     The credentials of the user to login.
        /// </param>
        /// <param name="returnUrl">
        ///     The URL to redirect the user after succesful login.
        /// </param>
        /// <returns>
        ///     The view if the user cannot login, a redirection upon succesfull
        ///     login.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(SigninViewModel model,
            string returnUrl = null)
        {
            _logger.Log(LogLevel.Information,
                EventIDs.LoginAttempt,
                new LogEvent($"Login attempt for user '{model.UserName}'.")
                    .AddProperty("user.name", model.UserName)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
                try
                {
                    // _signInManager.SignInWithClaimsAsync();

                    var result = await _signInManager.PasswordSignInAsync(model.UserName,
                        model.Password,
                        model.RememberMe,
                        false);

                    if (result.RequiresTwoFactor)
                    {
                        return RedirectToAction("LoginTwoStep", new { returnUrl = returnUrl });
                    }

                    if (result.Succeeded)
                    {
                        var currentUser = await _userManager.FindByNameAsync(model.UserName);
                        currentUser.LastLogin = DateTime.Now;
                        await _userManager.UpdateAsync(currentUser);

                        _logger.Log(LogLevel.Information,
                            EventIDs.LoginSuccessful,
                            new LogEvent($"Login successful for user '{model.UserName}'.")
                                .AddUser(currentUser)
                                .AddHttpContext(_accessor.HttpContext),
                            null,
                            LogEvent.Formatter);

                        returnUrl ??= "/";
                        return RedirectToLocal(returnUrl);
                    }

                    _logger.Log(LogLevel.Warning,
                        EventIDs.LoginFailed,
                        new LogEvent($"Login failed for user '{model.UserName}'.")
                            .AddProperty("user.name", model.UserName)
                            .AddHttpContext(_accessor.HttpContext),
                        null,
                        LogEvent.Formatter);

                    Response.StatusCode = (int) HttpStatusCode.Unauthorized;
                    TempData["ErrorMessage"] =
                        "The provided credentials are incorrect or you don't have the right to log in.";

                    return View(model);
                }
                catch (Exception e)
                {
                    _logger.Log(LogLevel.Error,
                        EventIDs.LoginError,
                        new LogEvent($"An error occured while login user '{model.UserName}' (Exception: {e.Message}).")
                            .AddException(e)
                            .AddProperty("user.name", model.UserName)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddException(e),
                        e,
                        LogEvent.Formatter);

                    TempData["ErrorMessage"] = "Something bad happened while logging in...";

                    return View(model);
                }

            _logger.Log(LogLevel.Warning,
                EventIDs.LoginFailed,
                new LogEvent($"Login failed for user '{model.UserName}'.")
                    .AddProperty("user.name", model.UserName)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return View(model);
        }
        
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LoginTwoStep(string returnUrl)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
                
            _logger.Log(LogLevel.Information,
                EventIDs.LoginAttempt,
                new LogEvent($"Login second-step attempt for user '{user.UserName}'.")
                    .AddProperty("user.name", user.UserName)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);
            
            return View(new LoginTwoStepModel { ReturnUrl = returnUrl});
        }
        
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> LoginTwoStep(LoginTwoStepModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
                if (user != null)
                {
                    _logger.Log(LogLevel.Information,
                        EventIDs.LoginAttempt,
                        new LogEvent($"Login second-step attempt for user '{user.UserName}'.")
                            .AddProperty("user.name", user.UserName)
                            .AddHttpContext(_accessor.HttpContext),
                        null,
                        LogEvent.Formatter);
                    
                    var verificationCode = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
                    var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(verificationCode, true, model.RememberMe);
                    if (result.Succeeded)
                    {
                        _logger.Log(LogLevel.Information,
                            EventIDs.LoginSuccessful,
                            new LogEvent($"Login second-step successful for user '{user.UserName}'.")
                                .AddProperty("user.name", user.UserName)
                                .AddHttpContext(_accessor.HttpContext),
                            null,
                            LogEvent.Formatter);
                        
                        var returnUrl = model.ReturnUrl ?? "/";
                        return RedirectToLocal(returnUrl);
                    }
                    
                    _logger.Log(LogLevel.Warning,
                        EventIDs.LoginFailed,
                        new LogEvent($"Login second-step failed for user '{user.UserName}'.")
                            .AddProperty("user.name", user.UserName)
                            .AddHttpContext(_accessor.HttpContext),
                        null,
                        LogEvent.Formatter);
                    
                    ModelState.AddModelError("", "Your multi-factor authentication code could not be verified.");
                    return View(model);
                }
                else
                {
                    _logger.Log(LogLevel.Warning,
                        EventIDs.LoginError,
                        new LogEvent($"Login second-step attempt for non-existing user.")
                            .AddHttpContext(_accessor.HttpContext),
                        null,
                        LogEvent.Formatter);
                }
            }
            // show the error to user
            ModelState.AddModelError("", "General Error");
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword(string username)
        {
            _logger.Log(LogLevel.Information,
                EventIDs.ForgotPasswordRequest,
                new LogEvent(!string.IsNullOrEmpty(username) ? $"Forgot password page requested for {username}." : $"Forgot password page requested.")
                    .AddProperty("user.name", username)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);
            
            return View(new ForgotPasswordViewModel() { UserName = username });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                
                var user = await _userManager.FindByNameAsync(model.UserName);
                if (user == null)
                {
                    _logger.Log(LogLevel.Error,
                        EventIDs.ForgotPasswordFailed,
                        new LogEvent($"Forgot password requested for non-existing user '{model.UserName}'.")
                            .AddProperty("user.name", model.UserName)
                            .AddHttpContext(_accessor.HttpContext),
                        null,
                        LogEvent.Formatter);
                    
                    return View("ForgotPasswordConfirmation");
                }

                if (!(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // The user does not have a confirmed email. Request a confirmation first. The user will have to 
                    // request a new password a second time, with a confirmed email.
                    var tokenConfirmation = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var callbackConfirmationUrl = Url.Action(
                        "ConfirmEmail", "Account", 
                        new { userId = user.Id, code = tokenConfirmation }, 
                        protocol: Request.Scheme);
                    await _emailSender.SendEmailConfirmation(user, callbackConfirmationUrl, reset: true);
                    
                    _logger.Log(LogLevel.Warning,
                        EventIDs.ForgotPasswordFailed,
                        new LogEvent($"Forgot password requested for '{model.UserName}' without confirmed email.")
                            .AddProperty("user.name", model.UserName)
                            .AddHttpContext(_accessor.HttpContext),
                        null,
                        LogEvent.Formatter);
                    
                    return View("ForgotPasswordConfirmation");
                }
                
                _logger.Log(LogLevel.Information,
                    EventIDs.ForgotPasswordSuccess,
                    new LogEvent($"Forgot password requested for '{model.UserName}'.")
                        .AddProperty("user.name", model.UserName)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.Action("ResetPassword", "Account", 
                    new { UserId = user.Id, code = code }, protocol: Request.Scheme);
                await _emailSender.SendPasswordReset(user, callbackUrl);
                return View("ForgotPasswordConfirmation");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> SendConfirmationLink()
        {
            var user = await GetCurrentUser();
            if (user is {EmailConfirmed: false})
            {
                _logger.Log(LogLevel.Information,
                    EventIDs.EmailConfirmationRequest,
                    new LogEvent($"Confirmation link requested for '{user.UserName}'.")
                        .AddProperty("user.name", user.UserName)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);
                
                var tokenConfirmation = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var callbackConfirmationUrl = Url.Action(
                    "ConfirmEmail", "Account", 
                    new { userId = user.Id, code = tokenConfirmation }, 
                    protocol: Request.Scheme);
                await _emailSender.SendEmailConfirmation(user, callbackConfirmationUrl);
            }
            else
            {
                _logger.Log(LogLevel.Error,
                    EventIDs.EmailConfirmationRequest,
                    new LogEvent($"Confirmation link requested for non existing user.")
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);
            }

            return View("EmailConfirmation");
        }

        /// <summary>
        ///     Provides the registration page.
        /// </summary>
        /// <param name="returnUrl">
        ///     The URL to which the user should be redirected when registration
        ///     is successful and complete.
        /// </param>
        /// <returns>
        ///     The view for user registration if registrations are open, an
        ///     unauthorized response otherwise.
        /// </returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Register(string returnUrl = null)
        {
            if (!_configuration.OpenRegistration)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.RegistrationClosed,
                    new LogEvent("Registration are closed.")
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);
                return Unauthorized();
            }

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync();
            ViewBag.PageName = "register";
            ViewData["ReturnUrl"] = returnUrl;
            return View(new RegisterViewModel());
        }

        /// <summary>
        ///     Registers the specified user and redirects to specified URL if any.
        /// </summary>
        /// <param name="model">
        ///     The user to register.
        /// </param>
        /// <param name="returnUrl">
        ///     The URL to redirect the user to, if registration is successful.
        /// </param>
        /// <returns>
        ///     The view if the model is invalid, a redirection to the specified URL
        ///     if the registration is successful, or an unauthorized response if
        ///     registrations are not open.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel model,
            string returnUrl = null)
        {
            if (!_configuration.OpenRegistration)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.RegistrationClosed,
                    new LogEvent("Registration are closed.")
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);
                return Unauthorized();
            }

            ViewData["ReturnUrl"] = returnUrl;

            if (model.Password != model.PasswordConfirmation)
                ModelState.AddModelError(nameof(model.PasswordConfirmation),
                    "Passwords do not match");

            if (ModelState.IsValid)
                try
                {
                    var user = new AppUser
                    {
                        UserName = model.UserName,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email
                    };
                    var result = await _userManager.CreateAsync(user,
                        model.Password);

                    if (result.Succeeded)
                    {
                        var currentUser = await _userManager.FindByNameAsync(model.UserName);
                        await _userManager.UpdateAsync(currentUser);
                        
                        var tokenConfirmation = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        var callbackConfirmationUrl = Url.Action(
                            "ConfirmEmail", "Account", 
                            new { userId = user.Id, code = tokenConfirmation }, 
                            protocol: Request.Scheme);
                        await _emailSender.SendEmailConfirmation(user, callbackConfirmationUrl);

                        _logger.Log(LogLevel.Information,
                            EventIDs.RegistrationSuccessful,
                            new LogEvent($"User '{model.UserName}' successfully registered.")
                                .AddUser(currentUser)
                                .AddHttpContext(_accessor.HttpContext),
                            null,
                            LogEvent.Formatter);

                        returnUrl ??= "/";
                        return RedirectToLocal(returnUrl);
                    }

                    _logger.Log(LogLevel.Warning,
                        EventIDs.RegistrationFailed,
                        new LogEvent($"Registration failed for user '{model.UserName}'.")
                            .AddHttpContext(_accessor.HttpContext),
                        null,
                        LogEvent.Formatter);

                    foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);

                    return View(model);
                }
                catch (Exception e)
                {
                    _logger.Log(LogLevel.Error,
                        EventIDs.RegistrationError,
                        new LogEvent(
                                $"An error occured while registering user '{model.UserName}' (Exception: {e.Message}).")
                            .AddException(e)
                            .AddProperty("user.name", model.UserName)
                            .AddProperty("user.email", model.Email)
                            .AddProperty("user.fullname", model.FirstName + " " + model.LastName)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddException(e),
                        e,
                        LogEvent.Formatter);

                    TempData["ErrorMessage"] = "Something bad happened while creating your account...";

                    return View(model);
                }

            _logger.Log(LogLevel.Warning,
                EventIDs.RegistrationFailed,
                new LogEvent($"Registration failed for user '{model.UserName}'.")
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return View(model);
        }

        /// <summary>
        ///     Logs the user out of the application.
        /// </summary>
        /// <returns>Redirection to index page.</returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            var currentUser = await GetCurrentUser();
            if (currentUser != null)
                _logger.Log(LogLevel.Information,
                    EventIDs.LogoutSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully logged out.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

            return RedirectToAction(nameof(Login), "Account");
        }

        /// <summary>
        ///     Provides the page to edit the user's own profile.
        /// </summary>
        /// <returns>
        ///     The view for editing own profile, an unauthorized response if the
        ///     user does not have the right to edit its own profile.
        /// </returns>
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var currentUser = await GetCurrentUser();
            if (!await _appAuthorizationService.CanEditUser(User, currentUser))
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempt to edit own profile without rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);
                return Unauthorized();
            }

            ViewBag.TimeZones = TZNames.GetDisplayNames("en-US", true).OrderBy(_ => _.Value)
                .Select(_ => new SelectListItem(_.Value, _.Key));

            return View(currentUser);
        }

        /// <summary>
        ///     Edits the user's own profile.
        /// </summary>
        /// <param name="viewModel">
        ///     The updated user profile
        /// </param>
        /// <returns>
        ///     The view if the view model is invalid, an unauthorized response
        ///     if the user does not have the rights to edit own profile, or
        ///     a redirect to own user profile when successful.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AppUser viewModel)
        {
            var currentUser = await GetCurrentUser();

            if (viewModel.UserName == currentUser.UserName)
            {
                if (!await _appAuthorizationService.CanEditUser(User, currentUser))
                {
                    _logger.Log(LogLevel.Warning,
                        EventIDs.EditFailed,
                        new LogEvent($"User '{currentUser.UserName}' attempt to edit own profile without rights.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext),
                        null,
                        LogEvent.Formatter);
                    return Unauthorized();
                }
            }
            else
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempt to edit other profile '{viewModel.UserName}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);
                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                currentUser.LastName = viewModel.LastName;
                currentUser.FirstName = viewModel.FirstName;
                currentUser.Function = viewModel.Function;

                if (currentUser.Email != viewModel.Email)
                {
                    currentUser.EmailConfirmed = false;
                }
                
                currentUser.Email = viewModel.Email;
                
                if (currentUser.Preferences == null)
                    currentUser.Preferences = new UserPreferences();

                if (currentUser.Preferences.Digest == null)
                    currentUser.Preferences.Digest = new UserPreferences.DigestPreferences();
                currentUser.Preferences.Digest.Frequency = viewModel.Preferences.Digest.Frequency;

                if (currentUser.Preferences.UI == null)
                    currentUser.Preferences.UI = new UserPreferences.UIPreferences();
                currentUser.Preferences.UI.Theme = viewModel.Preferences.UI.Theme;
                currentUser.Preferences.UI.FontSize = viewModel.Preferences.UI.FontSize;
                currentUser.Preferences.UI.BiggerContentFont = viewModel.Preferences.UI.BiggerContentFont;
                currentUser.Preferences.UI.HighContrastText = viewModel.Preferences.UI.HighContrastText;
                currentUser.Preferences.UI.TimeZone = viewModel.Preferences.UI.TimeZone;

                _logger.LogDebug(viewModel.Preferences.UI.Theme.ToString());
                _logger.LogDebug(viewModel.Preferences.UI.TimeZone);
                _logger.LogDebug(currentUser.Preferences.UI.TimeZone);

                _logger.Log(LogLevel.Information,
                    EventIDs.EditSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully edited own profile.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                await _userManager.UpdateAsync(currentUser);
                await AmbientContext.DatabaseContext.SaveChangesAsync();
                return RedirectToAction("Profile",
                    "User",
                    new {username = currentUser.UserName});
            }

            _logger.LogWarning("Model for login is invalid.");

            ViewBag.TimeZones = TZNames.GetDisplayNames("en-US", true).OrderBy(_ => _.Value)
                .Select(_ => new SelectListItem(_.Value, _.Key));

            return View(viewModel);
        }

        /// <summary>
        ///     Provides the page for user's subscriptions.
        /// </summary>
        /// <returns>
        ///     The view for user's subscriptions.
        /// </returns>
        [HttpGet("Account/Subscriptions")]
        public IActionResult Subscriptions()
        {
            return View(new SubscriptionViewModel
            {
                DocumentSubscriptions = _documentRepository.GetSubscriptionsAsync(AmbientContext, 1, -1).ToEnumerable(),
                TagSubscriptions = _tagRepository
                    .GetSubscriptionsAsync(AmbientContext, AmbientContext.CurrentUser, 1, -1).ToEnumerable(),
                SourceSubscriptions = _sourceRepository
                    .GetSubscriptionsAsync(AmbientContext, AmbientContext.CurrentUser, 1, -1).ToEnumerable(),
                MutedTags = _tagRepository
                    .GetMutedTagsAsync(AmbientContext, AmbientContext.CurrentUser).ToEnumerable(),
                MutedSources = _sourceRepository
                    .GetMutedSourcesAsync(AmbientContext, AmbientContext.CurrentUser).ToEnumerable()
            });
        }

        /// <summary>
        ///     Uploads and update the profile picture for the provided user.
        /// </summary>
        /// <param name="userName">
        ///     The username of the user for which the profile picture needs be
        ///     updated.
        /// </param>
        /// <param name="profilePicture">
        ///     The profile picture. It must be a JPG or a PNG file.
        /// </param>
        /// <returns>
        ///     A redirect to the user profile if successful. An Unauthorized
        ///     response if the user attempts to update a different profile.
        /// </returns>
        [HttpPost]
        public async Task<IActionResult> UploadProfilePicture(string userName,
            IFormFile profilePicture)
        {
            var currentUser = await GetCurrentUser();
            if (userName != currentUser.UserName)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.ProfilePictureFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to update the profile picture of a different user ({userName}).")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            if (profilePicture == null) ModelState.AddModelError("", "Select a profile picture.");

            if (profilePicture != null
                && !(profilePicture.ContentType == "image/png"
                     || profilePicture.ContentType == "image/jpg"
                     || profilePicture.ContentType == "image/jpeg"))
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.ProfilePictureFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' upload an illegal file type ({profilePicture.ContentType}) for the profile picture.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                ModelState.AddModelError("",
                    "Please select a PNG or JPG file for your profile picture.");
            }

            string extension;
            if (profilePicture.ContentType == "image/png")
                extension = ".png";
            else if ((profilePicture.ContentType == "image/jpg")
                     | (profilePicture.ContentType == "image/jpeg"))
                extension = ".jpg";
            else
                // This case will never happen, as the ContentType is 
                // checked before
                throw new NotImplementedException();

            var imageFolder = Path.Combine(_configuration.DocFolder, "images", "users");
            if (!Directory.Exists(imageFolder))
                Directory.CreateDirectory(imageFolder);

            var imageName = currentUser.UserName + extension;
            var filePath = Path.Combine(imageFolder, imageName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                profilePicture.CopyTo(stream);
            }

            Resize(filePath);
            currentUser.ProfilePicture = imageName;
            await _userManager.UpdateAsync(currentUser);

            _logger.Log(LogLevel.Information,
                EventIDs.ProfilePictureSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully uploaded a profile picture.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return RedirectToAction("Edit", "Account");
        }

        /// <summary>
        ///     Returns the profile picture of the specified user.
        /// </summary>
        /// <param name="userName">
        ///     The user name of which the profile picture must be retreived.
        /// </param>
        /// <returns>
        ///     The profile picture of the user if it exists, a "Not Found"
        ///     response otherwise.
        /// </returns>
        public async Task<IActionResult> ProfilePicture(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
                return NotFound();

            var file = Path.Combine(_configuration.DocFolder, "images", "users", user.ProfilePicture);
            if (file.EndsWith(".jpg") | file.EndsWith(".jpeg"))
                return PhysicalFile(file, "image/jpg");
            if (file.EndsWith(".png"))
                return PhysicalFile(file, "image/png");
            return NotFound();
        }

        #region Helpers

        /// <summary>
        ///     Redirects the user to the specified URL if the URL is local,
        ///     redirects to the index page of the home controller if not.
        ///     This prevents a malicious actor to redirect the user to an
        ///     arbitrary page after login.
        /// </summary>
        /// <param name="returnUrl">
        ///     The URL to redirect the user to.
        /// </param>
        /// <returns>
        ///     A redirection to the URL if local, to the index of the home
        ///     controller otherwise.
        /// </returns>
        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction(nameof(HomeController.Index),
                "Home");
        }

        #endregion

        private void Resize(string filePath)
        {
            var size = 250;
            using var image = Image.Load(filePath);
            image.Mutate(x => x
                .Resize(new ResizeOptions
                    {
                        Size = new Size(size, size),
                        Mode = ResizeMode.Max
                    }
                ));
            image.Save(filePath);
        }

        [HttpGet("Account/APIKeys")]
        public async Task<IActionResult> APIKeys()
        {
            var currentUser = await GetCurrentUser();
            var apiKeys =
                (await _userRepository.GetByUserName(AmbientContext, currentUser.UserName,
                    new[] {nameof(AppUser.APIKeys)}))?.APIKeys ?? Enumerable.Empty<APIKey>();
            return View(apiKeys);
        }

        [HttpGet("Account/APIKeys/Create")]
        public IActionResult CreateAPIKey()
        {
            return View();
        }

        [HttpPost("Account/APIKeys/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAPIKey([Bind("Name", "Description")] APIKey apiKey)
        {
            apiKey.UserId = (await GetCurrentUser()).Id;

            await _userRepository.AddAPIKeyAsync(AmbientContext, apiKey);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(APIKeys));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(string userid, string code)
        {
            var user = await _userManager.FindByIdAsync(userid);
            if (user == null)
            {
                _logger.Log(LogLevel.Information,
                    EventIDs.ResetPasswordRequest,
                    new LogEvent($"Failed to request a reset password for '{userid}'.")
                        .AddProperty("user.id", userid)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);
                
                return View("InvalidConfirmationEmail");
            }

            _logger.Log(LogLevel.Information,
                EventIDs.ResetPasswordRequest,
                new LogEvent($"Request password reset for '{user.UserName}'.")
                    .AddProperty("user.name", user.UserName)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);
            
            return View(new ResetPasswordViewModel()
            {
                UserId = userid,
                User = user,
                Code = code
            });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                _logger.Log(LogLevel.Information,
                    EventIDs.ResetPasswordFailed,
                    new LogEvent($"Request password reset failed for '{model.UserId}'.")
                        .AddProperty("user.id", model.UserId)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);
                
                return View("InvalidConfirmationEmail");
            }
            
            var passwordChangeResult
                = await _userManager.ResetPasswordAsync(user,
                    model.Code,
                    model.Password);
            await _context.SaveChangesAsync();

            if (passwordChangeResult.Succeeded)
            {
                _logger.Log(LogLevel.Information,
                    EventIDs.ResetPasswordSuccess,
                    new LogEvent($"Request password reset successful for '{user.UserName}'.")
                        .AddUser(user)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);
                
                return RedirectToAction(nameof(Login));    
            }
            
            _logger.Log(LogLevel.Information,
                EventIDs.ResetPasswordFailed,
                new LogEvent($"Request password reset failed for '{user.UserName}'.")
                    .AddUser(user)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);
            
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EmailConfirmationFailed,
                    new LogEvent($"Email confirmation failed for '{userId}' and '{code}'.")
                        .AddProperty("user.id", userId)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);
                return View("InvalidConfirmationEmail");
            }
            
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.Log(LogLevel.Error,
                    EventIDs.EmailConfirmationFailed,
                    new LogEvent($"Email confirmation for non-existing user '{userId}'.")
                        .AddProperty("user.id", userId)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);
            }
            else
            {
                var result = await _userManager.ConfirmEmailAsync(user, code);
                            if (result.Succeeded)
                            {
                                _logger.Log(LogLevel.Information,
                                    EventIDs.EmailConfirmationSuccess,
                                    new LogEvent($"Email confirmed for user '{user.UserName}'.")
                                        .AddUser(user)
                                        .AddHttpContext(_accessor.HttpContext),
                                    null,
                                    LogEvent.Formatter);
                                return View("ConfirmedEmail");
                            }
                            else
                            {
                                _logger.Log(LogLevel.Warning,
                                    EventIDs.EmailConfirmationFailed,
                                    new LogEvent($"Email confirmation invalid for user '{user.UserName}'.")
                                        .AddUser(user)
                                        .AddHttpContext(_accessor.HttpContext),
                                    null,
                                    LogEvent.Formatter);
                            }
            }
            
            
            
            return View("InvalidConfirmationEmail");
        }
        
        [HttpGet]
        public async Task<IActionResult> SetupTwoFactorAuthentication()
        {
            var user = await GetCurrentUser();
            
                var authenticatorKey = await _userManager.GetAuthenticatorKeyAsync(user);
                if (string.IsNullOrEmpty(authenticatorKey))
                {
                    await _userManager.ResetAuthenticatorKeyAsync(user);
                    authenticatorKey = await _userManager.GetAuthenticatorKeyAsync(user);
                }
            
                _logger.Log(LogLevel.Information,
                    EventIDs.Enable2FARequest,
                    new LogEvent($"Two-Factor authentication setup request.")
                        .AddUser(user)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);
                
                return View(new ConfigureTwoFactorAuthenticatorModel
                {
                    AuthenticatorKey = authenticatorKey,
                    AuthenticatorUri = GenerateQrCodeUri(user.Email, authenticatorKey)
                });
        }
        
        [HttpGet]
        public async Task<IActionResult> DisableTwoFactorAuthentication()
        {
            var user = await GetCurrentUser();
            _logger.Log(LogLevel.Error,
                EventIDs.Disable2FARequest,
                new LogEvent($"Disable two-factor authentication requested for '{user.UserName}'.")
                    .AddUser(user)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);
            
            return View();
        }
        
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> DisableTwoFactorAuthentication(DisableTwoFactorAuthenticationModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await GetCurrentUser();
                if(user!= null)
                {
                    var verificationCode = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
                    var result = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);
                    if (result)
                    {
                        _logger.Log(LogLevel.Information,
                            EventIDs.Disable2FASuccess,
                            new LogEvent($"Two-Factor authentication successfully disabled for '{user.UserName}'.")
                                .AddUser(user)
                                .AddHttpContext(_accessor.HttpContext),
                            null,
                            LogEvent.Formatter);
                        
                        user.TwoFactorEnabled = false;
                        await _context.SaveChangesAsync();
                        return RedirectToAction(nameof(HomeController.Index), "Home");
                    }
                    _logger.Log(LogLevel.Error,
                        EventIDs.Disable2FAFailed,
                        new LogEvent($"Two-Factor authentication failed to disable for '{user.UserName}'.")
                            .AddUser(user)
                            .AddHttpContext(_accessor.HttpContext),
                        null,
                        LogEvent.Formatter);
                    // show the error to user
                    ModelState.AddModelError("", "General Error");
                    return View(model);
                }
            }
            // show the error to user
            ModelState.AddModelError("", "General Error");
            return View(model);
        }
        
        private string GenerateQrCodeUri(string email, string unformattedKey)
        {
            const string authenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";
            return string.Format(
                authenticatorUriFormat,
                WebUtility.UrlEncode("DocIntel"),
                WebUtility.UrlEncode(email),
                unformattedKey);
        }
        
        [HttpPost]
        public async Task<IActionResult> VerifyAuthenticator(ConfigureTwoFactorAuthenticatorModel model)
        {
            var user = await GetCurrentUser();
            var verificationCode = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
 
            var is2FaTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

            if (is2FaTokenValid)
            {
                await _userManager.SetTwoFactorEnabledAsync(user, true);

                var result = new VerifyAuthenticatorResult
                {
                    Success = true,
                    Message = "Your authenticator app has been verified",
                };
            
                _logger.Log(LogLevel.Information,
                    EventIDs.Enable2FASuccess,
                    new LogEvent($"Two-Factor authentication successfully enabled for '{user.UserName}'.")
                        .AddUser(user)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                if (await _userManager.CountRecoveryCodesAsync(user) != 0) return View(result);
            
                _logger.Log(LogLevel.Information,
                    EventIDs.RecoveryCodeGenerated,
                    new LogEvent($"Recovery codes generated for user '{user.UserName}'.")
                        .AddUser(user)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
                result.Data = recoveryCodes;
                return View(result);
            }
            
            _logger.Log(LogLevel.Error,
                EventIDs.Enable2FAFailed,
                new LogEvent($"Two-Factor authentication failed to enable for '{user.UserName}'.")
                    .AddUser(user)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);
            
            ModelState.AddModelError(nameof(model.Code), "Your code appears to be invalid.");
            return View(model);
        }
    }
}