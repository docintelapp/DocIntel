namespace DocIntel.Integrations.ThreatConnect.Model
{
    public class Document : Group
    {
        public string FileName { get; set; }
        public string Password { get; set; }
        public bool Malware { get; set; }
        public DocumentStatus Status { get; set; }
    }

    public enum DocumentStatus
    {
        Success,
        AwaitingUpload,
        InProgress,
        Failed,
        None
    }
}