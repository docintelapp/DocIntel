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

using System.Collections.Generic;

namespace DocIntel.Core.Authentication
{
    /// <summary>
    ///     Provides an API for accessing user account via LDAP service.
    /// </summary>
    public interface ILdapService
    {
        /// <summary>
        ///     Returns the accounts associated with the provided e-mail
        ///     address.
        /// </summary>
        /// <param name="emailAddress">
        ///     The email address to search for.
        /// </param>
        /// <returns>
        ///     A list of accounts with email address matching the provided one.
        /// </returns>
        ICollection<LdapUser> GetUsersByEmailAddress(string emailAddress);

        /// <summary>
        ///     Returns the account associated with the provided user name.
        /// </summary>
        /// <param name="userName">
        ///     The user name to search for.
        /// </param>
        /// <returns>
        ///     A user account with user name matching the provided one.
        /// </returns>
        LdapUser GetUserByUserName(string userName);

        /// <summary>
        ///     Attempts to authenticate with the provided user name
        ///     and password.
        /// </summary>
        /// <param name="userName">The user name.</param>
        /// <param name="password">The password.</param>
        /// <returns>
        ///     <c>True</c> if the user was successfuly authenticated,
        ///     <c>False</c> otherwise.
        /// </returns>
        bool Authenticate(string userName, string password);
    }
}