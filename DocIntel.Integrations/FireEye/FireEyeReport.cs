namespace DocIntel.Integrations.FireEye
{
    public class FireEyeTMHReport
    {
        public string ReportId { get;  set; }
        public string Title { get;  set; }
        public FireEyeThreatScape ThreatScape { get;  set; }
        public string publishDate { get; set; }
        public string version { get; set; }
        public string fromMedia { get; set; }
        public string storyLink { get; set; }
        public string outlet { get; set; }
        public string isightComment { get; set; }
        public string tmhAccuracyRanking { get; set; }
        public relatedReport[] relatedReports { get; set; }
        public string copyright { get; set; }

        public override string ToString () { 
            return $"ReportId={ReportId?.ToString()}\nTitle={Title?.ToString()}\npublishDate={publishDate?.ToString()}\n"; 
        }
    }

    public class relatedReport
    {
        public string reportId { get; set; }
        public string publishDate { get; set; }
        public string title { get; set; }
    }

    public class FireEyeReport
    {
        public string threatDescription;
        public string ReportType { get;  set; }
        public FireEyeThreatScape ThreatScape { get;  set; }
        public string[] Audience { get;  set; }
        public FireEyeTagSection TagSection { get;  set; }
        public FireEyeRelations Relations { get;  set; }
        public string publishDate { get; set; }
        public string ReportId { get;  set; }
        public string Title { get;  set; }
        public string ExecSummary { get;  set; }

        public override string ToString () { 
            return $"ReportId={ReportId?.ToString()}\nTitle={Title?.ToString()}\npublishDate={publishDate?.ToString()}\n"; 
        }
    }

    public class FireEyeRelations
    {
        public FireEyeMalwareFamily[] MalwareFamilies { get; set; }
        public FireEyeActor[] Actors { get; set; }
    }

    public class FireEyeActor
    {
        public string id { get; set; }
        public string name { get; set; }
        public string[] aliases { get; set; }
    }

    public class FireEyeThreatScape
    {
        public string[] Product { get; set; }
    }

    public class FireEyeTagSection
    {
        public FireEyeMainTagSection Main { get; set; }
    }

    public class FireEyeMainTagSection
    {
        public FireEyeAffectedIndustries affectedIndustries { get; set; }
        public FireEyeOperatingSystems operatingSystems { get; set; }
        public FireEyeRoles roles { get; set; }
        public FireEyeMalwareCapabilities malwareCapabilities { get; set; }
        public FireEyeDetectionNames detectionNames { get; set; }
        public FireEyeMalwareFamilies malwareFamilies { get; set; }
        // languages
        // affectedSystems
        // impacts
        // intents
        // motivations
        // sourceGeographies
        // targetedInformations
        // ttps
        // threatSources
    }

    public class FireEyeMalwareFamilies
    {
        public FireEyeMalwareFamily[] malwareFamily { get; set; }
    }

    public class FireEyeMalwareFamily
    {
        public string id { get; set; }
        public string name { get; set; }
        public string[] aliases { get; set; }
    }

    public class FireEyeDetectionNames
    {
        public FireEyeDetectionName[] detectionName { get; set; }
    }

    public class FireEyeDetectionName
    {
        public string vendor { get; set; }
        public string name { get; set; }
    }

    public class FireEyeMalwareCapabilities
    {
        public string[] malwareCapability { get; set; }
    }

    public class FireEyeRoles
    {
        public string[] role { get; set; }
    }

    public class FireEyeOperatingSystems
    {
        public string[] operatingSystem { get; set; }
    }

    public class FireEyeAffectedIndustries
    {
        public string[] affectedIndustry { get; set; }
    }
}