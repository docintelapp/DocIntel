namespace DocIntel.Integrations.PassiveTotal.Model
{
    public class PassiveTotalReponse<T>
    {
        public bool Success { get; set; }
        public T Results { get; set; }
        public T Articles { get; set; }
        public int TotalRecords { get; set; }
    }
}