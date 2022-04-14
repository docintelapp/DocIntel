using System;
using System.Collections.Generic;

using DocIntel.Integrations.ThreatMatch.Model;

namespace DocIntel.Integrations.ThreatMatch
{
    public class ProfileFilter
    {
        public IEnumerable<ProfileTypeEnum> Types { get; set; }
        public IEnumerable<ReadStatus> Status { get; set; }
        public IEnumerable<int> Sectors { get; set; }
        public IEnumerable<Relevance> Relevance { get; set; }
        public IEnumerable<string> Keywords { get; set; }
        public DateTime? DateTo { get; set; }
        public DateTime? DateFrom { get; set; }
        public IEnumerable<string> Tags { get; set; }
        public IEnumerable<ProfileCapability> Capability { get; set; }
    }

    public class ReportFilter
    {
        public IEnumerable<int> Types { get; set; }
        public IEnumerable<ReadStatus> Status { get; set; }
        public IEnumerable<int> Sectors { get; set; }
        public IEnumerable<Relevance> Relevance { get; set; }
        public string Keywords { get; set; }
        public DateTime? DateTo { get; set; }
        public DateTime? DateFrom { get; set; }
    }

    public enum ProfileCapability
    {
        VeryLow,
        Low,
        Medium,
        High,
        VeryHigh
    }
}