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

namespace DocIntel.Core.Authentication
{
    /// <summary>
    ///     Represents a user in the LDAP Repository.
    /// </summary>
    public class LdapUser
    {
        /// <summary>
        ///     The distinguished name for the account.
        /// </summary>
        public string DistinguishedName { get; init; }

        /// <summary>
        ///     The groups the account is member of.
        /// </summary>
        public string[] MemberOf { get; init; }

        /// <summary>
        ///     The first name of the user associated with account.
        /// </summary>
        public string FirstName { get; init; }

        /// <summary>
        ///     The last name of the user associated with account.
        /// </summary>
        public string LastName { get; init; }

        /// <summary>
        ///     The e-mail address of the user associated with account.
        /// </summary>
        public string EmailAddress { get; init; }
    }
}