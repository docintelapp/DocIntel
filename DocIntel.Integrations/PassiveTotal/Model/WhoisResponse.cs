using System;
using System.Collections.Generic;

namespace DocIntel.Integrations.PassiveTotal.Model
{
    public class WhoisResponse
    {
        public string Domain { get; set; }
        public WhoisContact Registrant { get; set; }
        public WhoisContact Admin { get; set; }
        public WhoisContact Tech { get; set; }
        
        public string Name { get; set; }
        public string Organization { get; set; }
        public string ContactEmail { get; set; }
        public string Telephone { get; set; }
        public string WhoisServer { get; set; }
        public string Registrar { get; set; }

        public DateTime Registered { get; set; }
        public DateTime RegistryUpdatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime LastLoadedAt { get; set; }
        public IEnumerable<string> NameServers { get; set; }
        // zone
        // billing
    }
}