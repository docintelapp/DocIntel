using System;

namespace DocIntel.Integrations.ThreatConnect
{
    public class ObservationParameter
    {
        public DateTime? DateObserved { get; set; }
        public string Owner { get; set; }
        public int Start { get; set; }
        public int Limit { get; set; }
    }
}