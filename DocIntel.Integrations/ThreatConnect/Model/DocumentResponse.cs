namespace DocIntel.Integrations.ThreatConnect.Model
{
    public class DocumentResponse<T>
    {
        public int ResultCount { get; set; }
        public T Document { get; set; }
    }
}