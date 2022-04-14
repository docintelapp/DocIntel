using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace DocIntel.Integrations.ThreatConnect.Model
{
    public class Indicator
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public string OwnerName { get; set; }
        public string Type { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime LastModified { get; set; }
        public double Rating { get; set; }
        public double Confidence { get; set; }
        public double ThreatAssessRating { get; set; }
        public double ThreatAssessConfidence { get; set; }
        public string WebLink { get; set; }
        public string Summary { get; set; }
        public Dictionary<string,string> Keys { get; set; }
        
        public int? ObservationCount { get; set; }
        public DateTime? LastObserved { get; set; }
        public int? FalsePositiveCount { get; set; }
        public int? FalsePositiveLastReported { get; set; }
        public bool? PrivateFlag { get; set; }
        public bool? Active { get; set; }
        public bool? ActiveLocked { get; set; }
        public Owner Owner { get; set; }
        public IEnumerable<UserObservation> UserObservedList { get; set; }
    }

    public class UserObservation
    {
        public string UserName { get; set; }
        public int Count { get; set; }
    }

    public class Address : Indicator
    {
        public string Ip { get; set; }
    }

    public class Asn : Indicator
    {
        [JsonProperty("AS Number")]
        public string ASNumber { get; set; }
    }
    public class CidrBlock : Indicator
    {
        public string Block { get; set; }
    }

    public class EmailAddress : Indicator
    {
        public string Address { get; set; }
    }

    public class File : Indicator
    {
        public string md5 { get; set; }
        public string sha1 { get; set; }
        public string sha256 { get; set; }
        public int Size { get; set; }

    }

    public class Host : Indicator
    {
        public string hostName { get; set; }
        public bool dnsActive { get; set; }
        public bool whoisActive { get; set; }
    }
    public class Mutex : Indicator
    {
        [JsonProperty("Mutex")]
        public string MutexName { get; set; }
    }
    public class RegistryKey : Indicator
    {
        [JsonProperty("Key Name")]
        public string KeyName { get; set; }
        [JsonProperty("Value Name")]
        public string ValueName { get; set; }
        [JsonProperty("Value Type")]
        public string ValueType { get; set; }
    }

    public class Url : Indicator
    {
        public string Text { get; set; }
    }
    public class UserAgent : Indicator
    {
        [JsonProperty("User Agent String")]
        public string UserAgentString { get; set; }
    }

}