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

namespace DocIntel.Core.Settings
{
    /// <summary>
    ///     Represents the credentials for authenticating with LDAP service.
    /// </summary>
    public class LdapCredentials
    {
        /// <summary>
        ///     The user name.
        /// </summary>
        public string DomainUserName { get; set; }

        /// <summary>
        ///     The password.
        /// </summary>
        public string Password { get; set; }
    }

    /// <summary>
    ///     Represents the settings for interacting with LDAP service.
    /// </summary>
    public class LdapSettings
    {
        /// <summary>
        ///     The server domain name.
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        ///     The server port.
        /// </summary>
        public int ServerPort { get; set; }

        /// <summary>
        ///     Whether SSL should be used for authentication.
        /// </summary>
        public bool UseSSL { get; set; }

        /// <summary>
        ///     The search base, e.g. "CN=users,DC=subdomain,DC=domain,DC=be"
        /// </summary>
        public string SearchBase { get; set; }

        /// <summary>
        ///     The name of the container.
        /// </summary>
        /// <value></value>
        public string ContainerName { get; set; }

        /// <summary>
        ///     The name of the domain, e.g. "subdomain"
        /// </summary>
        /// <value></value>
        public string DomainName { get; set; }

        // ReSharper disable once CommentTypo
        /// <summary>
        ///     The distinguished name for the domain, e.g. "SUBDOMAIN\\ldapquery"
        /// </summary>
        /// <value></value>
        public string DomainDistinguishedName { get; set; }

        /// <summary>
        ///     The credentials for the authentication
        /// </summary>
        public LdapCredentials Credentials { get; set; }

        /// <summary>
        ///     The group that account must be member of to be able to logon
        ///     DocIntel.
        /// </summary>
        public string Group { get; set; }
    }
}