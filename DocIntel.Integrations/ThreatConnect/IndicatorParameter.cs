namespace DocIntel.Integrations.ThreatConnect
{
    public class IndicatorParameter
    {
        public string Owner { get; set; }
        public int Start { get; set; }
        public int Limit { get; set; }
        public bool Active { get; set; }
        public string Summary { get; set; }
        public string ModifiedSince { get; set; }
        public string DateAdded { get; set; }
        public string Rating { get; set; }
        public string Confidence { get; set; }
        public string ThreatAssessScore { get; set; }
        public string ThreatAssessRating { get; set; }
        public string ThreatAssessConfidence { get; set; }
        public string FalsePositive { get; set; }
        public string City { get; set; }
        public string CountryCode { get; set; }
        public string CountryName { get; set; }
        public string Organization { get; set; }
        public string State { get; set; }
        public string Timezone { get; set; }
        public string Asn { get; set; }
        public bool? WhoisActive { get; set; }
        public bool? DnsActive { get; set; }
        public bool IncludeAdditional { get; set; } = false;
    }

    public class GroupParameter
    {
        public string Owner { get; set; }
        public int Start { get; set; }
        public int Limit { get; set; }
        public string Name { get; set; }
        public string DateAdded { get; set; }
    }
}