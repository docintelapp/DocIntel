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
using System.Threading.Tasks;
using DocIntel.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocIntel.Core.Authentication
{
    /// <summary>
    ///     Provides an API for managing users via an LDAP service.
    /// </summary>
    public class LdapUserManager : UserManager<AppUser>
    {
        private readonly ILdapService _ldapService;

        /// <summary>
        ///     Creates a new instance of LdapUserManager
        /// </summary>
        /// <param name="ldapService">
        ///     The service for connecting to the LDAP service.
        /// </param>
        /// <param name="store">
        ///     The persistence store the manager will operate over.
        /// </param>
        /// <param name="optionsAccessor">
        ///     The accessor used to access the IdentityOptions.
        /// </param>
        /// <param name="passwordHasher">
        ///     The password hashing implementation to use when saving
        ///     passwords.
        /// </param>
        /// <param name="userValidators">
        ///     A collection of <see cref="IUserValidator{TUser}" /> to validate users against.
        /// </param>
        /// <param name="passwordValidators">
        ///     A collection of <see cref="IPasswordValidator{TUser}" /> to validate passwords
        ///     against.
        /// </param>
        /// <param name="keyNormalizer">
        ///     The ILookupNormalizer to use when generating index keys for
        ///     users.
        /// </param>
        /// <param name="errors">
        ///     The IdentityErrorDescriber used to provider error messages.
        /// </param>
        /// <param name="services">
        ///     The IServiceProvider used to resolve services.
        /// </param>
        /// <param name="logger">
        ///     The logger used to log messages, warnings and errors.
        /// </param>
        public LdapUserManager(
            ILdapService ldapService,
            IUserStore<AppUser> store,
            IOptions<IdentityOptions> optionsAccessor,
            IPasswordHasher<AppUser> passwordHasher,
            IEnumerable<IUserValidator<AppUser>> userValidators,
            IEnumerable<IPasswordValidator<AppUser>> passwordValidators,
            ILookupNormalizer keyNormalizer,
            IdentityErrorDescriber errors,
            IServiceProvider services,
            ILogger<LdapUserManager> logger)
            : base(
                store,
                optionsAccessor,
                passwordHasher,
                userValidators,
                passwordValidators,
                keyNormalizer,
                errors,
                services,
                logger)
        {
            _ldapService = ldapService;
        }

        /// <summary>
        ///     The LDAP service supports roles.
        /// </summary>
        /// <value>True</value>
        public override bool SupportsUserRole => true;

        /// <summary>
        ///     The LDAP service supports user claims.
        /// </summary>
        /// <value>True</value>
        public override bool SupportsUserClaim => true;

        /// <summary>
        ///     Returns a flag indicating whether the given password is valid
        ///     for the specified user.
        /// </summary>
        /// <param name="user">The user whose password should be validated.</param>
        /// <param name="password">The password to validate</param>
        /// <returns>
        ///     The Task that represents the asynchronous operation, containing
        ///     true if the specified password matches the one store for the
        ///     user, otherwise false.
        /// </returns>
        public override async Task<bool> CheckPasswordAsync(AppUser user, string password)
        {
            return await Task.Run(() => _ldapService.Authenticate(user.UserName, password));
        }
    }
}