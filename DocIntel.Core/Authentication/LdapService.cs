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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DocIntel.Core.Logging;
using DocIntel.Core.Settings;
using Microsoft.Extensions.Logging;
using Novell.Directory.Ldap;

namespace DocIntel.Core.Authentication
{
    /// <summary>
    ///     Provides an API for accessing Active Directory users via
    ///     LDAP service.
    /// </summary>
    public class ActiveDirectoryLdapService : ILdapService
    {
        private readonly string[] _attributes =
        {
            "memberOf", "distinguishedName", "givenName", "sn", "mail"
        };

        private readonly LdapSettings _ldapSettings;
        private readonly ILogger<ActiveDirectoryLdapService> _logger;

        /// <summary>
        ///     Creates a new instance of ActiveDirectoryLdapService
        /// </summary>
        /// <param name="ldapSettings">
        ///     The settings to connect to the active directory via LDAP
        ///     service.
        /// </param>
        /// <param name="logger">
        ///     The logger used to log messages, warnings and errors.
        /// </param>
        public ActiveDirectoryLdapService(LdapSettings ldapSettings,
            ILogger<ActiveDirectoryLdapService> logger)
        {
            _ldapSettings = ldapSettings;
            _logger = logger;
        }

        /// <summary>
        ///     Returns the accounts associated with the provided e-mail
        ///     address by searching the <c>mail</c> field for object class
        ///     <c>user</c>.
        /// </summary>
        /// <param name="emailAddress">
        ///     The email address to search for.
        /// </param>
        /// <returns>
        ///     A list of accounts with email address matching the provided one.
        /// </returns>
        public ICollection<LdapUser> GetUsersByEmailAddress(string emailAddress)
        {
            var users = new Collection<LdapUser>();

            var filter = $"(&(objectClass=user)(mail={emailAddress}))";

            using var ldapConnection = GetConnection();
            _logger.Log(LogLevel.Debug,
                default,
                new LogEvent($"Search for '{emailAddress}' in the Active Directory.")
                    .AddProperty("user.email", emailAddress),
                null,
                LogEvent.Formatter);

            var search = ldapConnection.Search(
                _ldapSettings.SearchBase,
                LdapConnection.SCOPE_SUB,
                filter,
                _attributes,
                false, null, null);

            LdapMessage message;

            while ((message = search.getResponse()) != null)
            {
                if (!(message is LdapSearchResult searchResultMessage)) continue;

                users.Add(CreateUserFromAttributes(
                    _ldapSettings.SearchBase,
                    searchResultMessage.Entry.getAttributeSet()
                ));
            }

            return users;
        }

        /// <summary>
        ///     Returns the account associated with the provided user name
        ///     address by searching the <c>sAMAccountName</c> field for object
        ///     class <c>user</c>.
        /// </summary>
        /// <param name="userName">
        ///     The user name to search for.
        /// </param>
        /// <returns>
        ///     A user account with user name matching the provided one.
        /// </returns>
        public LdapUser GetUserByUserName(string userName)
        {
            LdapUser user = null;

            var filter = $"(&(objectClass=user)(sAMAccountName={userName}))";

            using var ldapConnection = GetConnection();
            _logger.Log(LogLevel.Debug,
                default,
                new LogEvent($"Search for '{userName}' in the Active Directory.")
                    .AddProperty("user.name", userName),
                null,
                LogEvent.Formatter);

            var search = ldapConnection.Search(
                _ldapSettings.SearchBase,
                LdapConnection.SCOPE_SUB,
                filter,
                _attributes,
                false,
                null,
                null);

            LdapMessage message;

            while ((message = search.getResponse()) != null)
            {
                if (!(message is LdapSearchResult searchResultMessage))
                    continue;

                user = CreateUserFromAttributes(
                    _ldapSettings.SearchBase,
                    searchResultMessage.Entry.getAttributeSet()
                );
            }

            return user;
        }

        /// <summary>
        ///     Attempts to authenticate with the provided user name
        ///     and password.
        /// </summary>
        /// <param name="userName">The user name.</param>
        /// <param name="password">The password.</param>
        /// <returns>
        ///     <c>True</c> if the user was successfully authenticated,
        ///     <c>False</c> otherwise.
        /// </returns>
        public bool Authenticate(string userName, string password)
        {
            // Avoids that the server returns true because the account exists
            // even if the provided password is incorrect.
            if (string.IsNullOrWhiteSpace(password))
            {
                _logger.Log(LogLevel.Debug,
                    default,
                    new LogEvent(
                        $"Attempts to authenticate with an empty password '{userName}' against Active Directory."),
                    null,
                    LogEvent.Formatter);
                return false;
            }

            using var ldapConnection = new LdapConnection
            {
                SecureSocketLayer = _ldapSettings.UseSSL
            };
            var ldapUser = GetUserByUserName(userName);
            if (ldapUser == null)
            {
                _logger.Log(LogLevel.Debug,
                    default,
                    new LogEvent(
                        $"Attempts to authenticate with a non-existing account '{userName}' against Active Directory."),
                    null,
                    LogEvent.Formatter);
                return false;
            }

            _logger.Log(LogLevel.Debug,
                default,
                new LogEvent($"Attempts to authenticate with '{userName}' against Active Directory."),
                null,
                LogEvent.Formatter);

            ldapConnection.Connect(_ldapSettings.ServerName,
                _ldapSettings.ServerPort);

            try
            {
                ldapConnection.Bind(ldapUser.DistinguishedName, password);
                _logger.Log(LogLevel.Debug,
                    default,
                    new LogEvent($"User '{userName}' successfully authenticated against Active Directory."),
                    null,
                    LogEvent.Formatter);
                return true;
            }
            catch (LdapException e)
            {
                if (e.ResultCode == LdapException.INVALID_CREDENTIALS)
                    _logger.Log(LogLevel.Debug,
                        default,
                        new LogEvent($"User '{userName}' failed to provide valid credentials."),
                        e,
                        LogEvent.Formatter);
                else
                    _logger.Log(LogLevel.Debug,
                        default,
                        new LogEvent("An LDAP exception occured while authenticating against Active Directory (" +
                                     e.Message + ")."),
                        e,
                        LogEvent.Formatter);

                return false;
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Debug,
                    default,
                    new LogEvent("An exception occured while authenticating against Active Directory (" +
                                 e.Message + ")."),
                    e,
                    LogEvent.Formatter);

                return false;
            }
        }

        private ILdapConnection GetConnection()
        {
            var ldapConnection = new LdapConnection
            {
                SecureSocketLayer = _ldapSettings.UseSSL
            };

            _logger.Log(LogLevel.Debug,
                default,
                new LogEvent(
                        $"Attempt to connect {_ldapSettings.ServerName}:{_ldapSettings.ServerPort} (ssl enabled: {_ldapSettings.UseSSL}).")
                    .AddProperty("server.domain", _ldapSettings.ServerName)
                    .AddProperty("server.port", _ldapSettings.ServerPort),
                null,
                LogEvent.Formatter);

            ldapConnection.Connect(_ldapSettings.ServerName,
                _ldapSettings.ServerPort);

            ldapConnection.Bind(_ldapSettings.Credentials.DomainUserName,
                _ldapSettings.Credentials.Password);

            return ldapConnection;
        }

        private LdapUser CreateUserFromAttributes(string user,
            LdapAttributeSet attributes)
        {
            var ldapUser = new LdapUser
            {
                MemberOf = attributes.getAttribute("memberOf")?.StringValueArray,
                DistinguishedName = attributes.getAttribute("distinguishedName")?.StringValue ?? user,
                FirstName = attributes.getAttribute("givenName")?.StringValue,
                LastName = attributes.getAttribute("sn")?.StringValue,
                EmailAddress = attributes.getAttribute("mail")?.StringValue
            };

            return ldapUser;
        }
    }
}