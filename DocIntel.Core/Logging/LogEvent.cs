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
using System.Collections;
using System.Collections.Generic;
using DocIntel.Core.Models;
using DocIntel.Core.Settings;

namespace DocIntel.Core.Logging
{
    public class LogEvent : IEnumerable<KeyValuePair<string, object>>
    {
        private readonly List<KeyValuePair<string, object>> _properties = new();
        private readonly string _message;

        public LogEvent(string message)
        {
            _message = message;
        }

        public static Func<LogEvent, Exception, string> Formatter { get; } = (l, _) => l._message;

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _properties.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public LogEvent AddProperty(string name, object value)
        {
            _properties.Add(new KeyValuePair<string, object>(name, value));
            return this;
        }

        public LogEvent AddUser(AppUser user, string prefix = "user")
        {
            if (string.IsNullOrEmpty(prefix))
                throw new ArgumentException("Prefix cannot be null or empty.",
                    nameof(prefix));

            AddProperty(prefix + ".name", user.UserName);
            AddProperty(prefix + ".email", user.Email);
            AddProperty(prefix + ".full_name", $"{user.FirstName} {user.LastName}");
            AddProperty(prefix + ".id", user.Id);
            return this;
        }

        public LogEvent AddLdapSettings(LdapSettings settings)
        {
            AddProperty("user.domain", settings.DomainName);
            return this;
        }

        public LogEvent AddException(Exception e)
        {
            AddProperty("error.message", e.Message);
            AddProperty("error.stack_trace", e.StackTrace);
            AddProperty("error.type", e.GetType().FullName);
            return this;
        }
    }
}