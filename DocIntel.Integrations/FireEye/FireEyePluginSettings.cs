using System;

namespace DocIntel.Integrations.FireEye
{
    public class FireEyePluginSettings
    {
        public string ApiKey { get; set; }
        public string Secret { get; set; }
        public string Proxy { get; set; }
        public DateTime LastUpdate { get; set; }
        public string LastReferencePulled { get; set; }
    }
}