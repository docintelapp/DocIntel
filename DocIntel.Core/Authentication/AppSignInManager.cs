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

using System.Threading.Tasks;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocIntel.Core.Authentication
{
    /// <summary>
    ///     Provides the API for the users to sign in DocIntel with the local
    ///     database.
    /// </summary>
    public class AppSignInManager : SignInManager<AppUser>
    {
        private readonly ILogger<AppSignInManager> _logger;

        /// <summary>
        ///     Creates a new instance of AppSignInManager.
        /// </summary>
        /// <param name="userManager">
        ///     An instance of
        ///     Microsoft.AspNetCore.Identity.SignInManager`1.UserManager
        ///     used to retrieve users from and persist users.
        /// </param>
        /// <param name="contextAccessor">
        ///     The accessor used to access the
        ///     Microsoft.AspNetCore.Http.HttpContext.
        /// </param>
        /// <param name="claimsFactory">
        ///     The factory to use to create claims principals for a user.
        /// </param>
        /// <param name="optionsAccessor">
        ///     The accessor used to access the
        ///     Microsoft.AspNetCore.Identity.IdentityOptions.
        /// </param>
        /// <param name="logger">
        ///     The logger used to log messages, warnings and errors.
        /// </param>
        /// <param name="schemes">
        ///     The scheme provider that is used enumerate the authentication
        ///     schemes.
        /// </param>
        /// <param name="confirmation">
        ///     The Microsoft.AspNetCore.Identity.IUserConfirmation`1 used check
        ///     whether a user account is confirmed.
        /// </param>
        public AppSignInManager(UserManager<AppUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<AppUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<AppSignInManager> logger,
            IAuthenticationSchemeProvider schemes,
            IUserConfirmation<AppUser> confirmation)
            : base(
                userManager,
                contextAccessor,
                claimsFactory,
                optionsAccessor,
                logger,
                schemes,
                confirmation)
        {
            _logger = logger;
        }

        /// <summary>
        ///     Attempts to sign in the specified user and password combination
        ///     as an asynchronous operation.
        /// </summary>
        /// <param name="user">The user to sign in.</param>
        /// <param name="password">
        ///     The password to attempt to sign in with.
        /// </param>
        /// <param name="isPersistent">
        ///     Flag indicating whether the sign-in cookie should persist after
        ///     the browser is closed.
        /// </param>
        /// <param name="lockoutOnFailure">
        ///     Flag indicating if the user account should be locked if the sign
        ///     in fails.
        /// </param>
        /// <returns>
        ///     The task object representing the asynchronous operation
        ///     containing the SignInResult for the sign-in attempt.
        /// </returns>
        public override async Task<SignInResult> PasswordSignInAsync(
            AppUser user,
            string password,
            bool isPersistent,
            bool lockoutOnFailure)
        {
            if (!user.Enabled)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.UserLogOnDisabled,
                    new LogEvent($"User '{user.UserName}' attempted to log on with a disabled account.")
                        .AddUser(user),
                    null,
                    LogEvent.Formatter);

                return SignInResult.NotAllowed;
            }

            var signInResult
                = await base.PasswordSignInAsync(user,
                    password,
                    isPersistent,
                    lockoutOnFailure);

            if (signInResult == SignInResult.Success)
                _logger.Log(LogLevel.Information,
                    EventIDs.UserLogOnSuccess,
                    new LogEvent($"User '{user.UserName}' successfully logged on.")
                        .AddUser(user),
                    null,
                    LogEvent.Formatter);
            else if (signInResult == SignInResult.Failed)
                _logger.Log(LogLevel.Information,
                    EventIDs.UserLogOnFailed,
                    new LogEvent($"User '{user.UserName}' failed to logon.")
                        .AddUser(user),
                    null,
                    LogEvent.Formatter);
            else if (signInResult == SignInResult.NotAllowed)
                _logger.Log(LogLevel.Information,
                    EventIDs.UserLogOnNotAllowed,
                    new LogEvent($"User '{user.UserName}' is not allowed to logon.")
                        .AddUser(user),
                    null,
                    LogEvent.Formatter);

            return signInResult;
        }
    }
}