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

namespace DocIntel.Core.Settings
{
    public class ApplicationSettings
    {
        public MFASettings MFA;
        public string ApplicationName { get; set; }
        public string ApplicationBaseURL { get; set; }
        
        public bool OpenRegistration { get; set; }

        public string DocFolder { get; set; }
        public string DocumentPrefix { get; set; }
        public string StaticFiles { get; set; } = "wwwroot";
        
        public string Proxy { get; set; }
        public string NoProxy { get; set; }
        
        public string AutomationAccount { get; set; } = "automation";
        public ElasticSettings ElasticSearch { get; set; } = new ElasticSettings();
        public LdapSettings LDAP { get; set; }
        public EmailSettings Email { get; set; }

        public string AuthenticationMethod { get; set; }
    }

    public class MFASettings
    {
        public string TwoFactorSecretCode { get; set; }
    }

    public class ElasticSettings
    {
        public string Uri { get; set; } = "localhost:9200";
    }
}