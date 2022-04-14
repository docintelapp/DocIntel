using System;

namespace DocIntel.Integrations.FireEye
{
    public class ReportParameters
    {
        public DateTime? since { get; set; }
        public string sinceReport { get; set; }
        public string sinceReportVersion { get; set; }
        public string threatScape { get; set; }
        public string pubType { get; set; }
        public string intelligenceType { get; set; }
        public int limit { get; set; } = 1000;
        public int offset { get; set; } = 0;
        public DateTime? startDate { get; set; }
        public DateTime? endDate { get; set; }
        public string audience { get; set; }
        public string reportType { get; set; }
        public string sortBy { get; set; }
    }
}