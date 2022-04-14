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

using System.Linq;
using System.Threading.Tasks;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Settings;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocIntel.Core.Authentication
{
    /// <summary>
    ///     Provides the API for the users to sign in DocIntel via an LDAP
    ///     service.
    /// </summary>
    public class LdapSignInManager : SignInManager<AppUser>
    {
        private readonly ILdapService _ldapService;
        private readonly LdapSettings _ldapSettings;
        private readonly ILogger<LdapSignInManager> _logger;

        /// <summary>
        ///     Creates a new instance of LdapSignInManager.
        /// </summary>
        /// <param name="ldapService">
        ///     The service for connecting to the LDAP service.
        /// </param>
        /// <param name="ldapSettings">
        ///     The settings of the LDAP service.
        /// </param>
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
        public LdapSignInManager(ILdapService ldapService,
            LdapSettings ldapSettings,
            LdapUserManager userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<AppUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<LdapSignInManager> logger,
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
            _ldapService = ldapService;
            _logger = logger;
            _ldapSettings = ldapSettings;
        }

        /// <summary>
        ///     Attempts to sign in the specified userName and password
        ///     combination as an asynchronous operation.
        /// </summary>
        /// <param name="userName">The user name to sign in.</param>
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
            string userName,
            string password,
            bool isPersistent,
            bool lockoutOnFailure)
        {
            var appUser = await UserManager.FindByNameAsync(userName);

            // Creates the user if it does not exists.
            if (appUser == null
                && (appUser = await CreateUserAsync(userName)) == null)
                return SignInResult.NotAllowed;

            return await PasswordSignInAsync(appUser,
                password,
                isPersistent,
                lockoutOnFailure);
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
                    new LogEvent($"User '{user.UserName}' attempted to authenticate with a disabled account.")
                        .AddUser(user).AddLdapSettings(_ldapSettings),
                    null,
                    LogEvent.Formatter);
                return SignInResult.LockedOut;
            }

            var ldapUser = _ldapService.GetUserByUserName(user.UserName);
            if (ldapUser == null)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.UserLogOnNotAllowed,
                    new LogEvent($"User '{user.UserName}' no longer exists in LDAP directory.")
                        .AddUser(user).AddLdapSettings(_ldapSettings),
                    null,
                    LogEvent.Formatter);
                return SignInResult.NotAllowed;
            }

            if (!string.IsNullOrEmpty(_ldapSettings.Group)
                && !ldapUser.MemberOf.Contains(_ldapSettings.Group))
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.UserLogOnNotAllowed,
                    new LogEvent($"User '{user.UserName}' no longer belong to '" + _ldapSettings.Group + "'.")
                        .AddUser(user).AddLdapSettings(_ldapSettings),
                    null,
                    LogEvent.Formatter);
                return SignInResult.NotAllowed;
            }

            if (!_ldapService.Authenticate(user.UserName, password))
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.UserLogOnFailed,
                    new LogEvent($"User '{user.UserName}' failed to authenticate.")
                        .AddUser(user).AddLdapSettings(_ldapSettings),
                    null,
                    LogEvent.Formatter);
                return SignInResult.Failed;
            }

            // does it includes the claims from factory?
            await SignInAsync(user, isPersistent);

            _logger.Log(LogLevel.Information,
                EventIDs.UserLogOnSuccess,
                new LogEvent($"User '{user.UserName}' successfully authenticated.")
                    .AddUser(user).AddLdapSettings(_ldapSettings),
                null,
                LogEvent.Formatter);

            return SignInResult.Success;
        }

        private async Task<AppUser> CreateUserAsync(string userName)
        {
            var user = _ldapService.GetUserByUserName(userName);
            if (user == null)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.UserLogOnNotAllowed,
                    new LogEvent($"User '{userName}' does not exist in LDAP directory.")
                        .AddLdapSettings(_ldapSettings),
                    null,
                    LogEvent.Formatter);
                return default;
            }

            if (string.IsNullOrEmpty(_ldapSettings.Group)
                || user.MemberOf.Contains(_ldapSettings.Group))
            {
                var appUser = new AppUser
                {
                    UserName = userName,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.EmailAddress,
                    EmailConfirmed = true,
                    Enabled = true
                };

                await UserManager.CreateAsync(appUser);

                _logger.Log(LogLevel.Information,
                    EventIDs.LocalUserCreated,
                    new LogEvent($"Local user '{appUser.UserName}' created.")
                        .AddUser(appUser).AddLdapSettings(_ldapSettings),
                    null,
                    LogEvent.Formatter);

                return appUser;
            }

            _logger.Log(LogLevel.Warning,
                EventIDs.UserLogOnNotAllowed,
                new LogEvent($"User '{userName}' does not belong to '" + _ldapSettings.Group + "'.")
                    .AddLdapSettings(_ldapSettings),
                null,
                LogEvent.Formatter);

            return default;
        }
    }
}