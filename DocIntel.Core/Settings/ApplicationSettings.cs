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
using System.Xml;
using Synsharp;

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
        public SolrSettings Solr { get; set; } = new ();
        public LdapSettings LDAP { get; set; } = new();
        public SynapseSettings Synapse { get; set; } = new();
        public EmailSettings Email { get; set; } = new();
        public RabbitMQSettings RabbitMQ { get; set; } = new();

        public string AuthenticationMethod { get; set; }
        public string LockFolder { get; set; }
        public SecuritySettings Security { get; set; } = new();
        public ScheduleSettings Schedule { get; set; } = new();
    }

    public class ScheduleSettings
    {
        /// <summary>
        /// The maximum delay, in minutes, before a source, a document, or a tag is picked up for indexing. 
        /// </summary>
        public double MaxIndexingDelay { get; set; } = 30;

        /// <summary>
        /// Delay, in minutes, between two check of missed messages for the document analyzer.
        /// </summary>
        public double AnalyzerFrequencyCheck { get; set; } = 5;

        /// <summary>
        /// Delay, in minutes, between two check of missed messages for the indexers..
        /// </summary>
        public double IndexingFrequencyCheck { get; set; } = 15;

        /// <summary>
        /// Delay, in minutes, between two check of importers.
        /// </summary>
        public double ImporterFrequencyCheck { get; set; } = 5;
    }

    public class SecuritySettings
    {
        public DtdProcessing DtdProcessing { get; set; } = DtdProcessing.Prohibit;
    }

    public class MFASettings
    {
        public string TwoFactorSecretCode { get; set; }
    }

    public class RabbitMQSettings
    {
        public string Host { get; set; } = "localhost";
        public string VirtualHost { get; set; } = "/";
        public string Username { get; set; } = "guest";
        public string Password { get; set; } = "guest";
    }

    public class SolrSettings
    {
        public string Uri { get; set; } = "http://localhost:8983";
    }
}