using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace DocIntel.Integrations.ThreatConnect.Model
{
    public class Response
    {
        public string Status { get; set; }
        public Data Data { get; set; }
        public bool Anonymous { get; set; }
    }
    
    public class SingleResponse
    {
        public string Status { get; set; }
        public SingleData Data { get; set; }
    }

    public class Data
    {
        public int ResultCount { get; set; }
        public IEnumerable<Owner> Owner { get; set; }
        public IEnumerable<OwnerMetric> OwnerMetric { get; set; }
        public IEnumerable<User> User { get; set; }
        public IEnumerable<Group> Group { get; set; }
        public IEnumerable<Adversary> Adversary { get; set; }
        public IEnumerable<Campaign> Campaign { get; set; }
        public IEnumerable<Document> Document { get; set; }
        public IEnumerable<Email> Email { get; set; }
        public IEnumerable<Event> Event { get; set; }
        public IEnumerable<Incident> Incident { get; set; }
        public IEnumerable<IntrusionSet> IntrusionSet { get; set; }
        public IEnumerable<Report> Report { get; set; }
        public IEnumerable<Signature> Signature { get; set; }
        public IEnumerable<Threat> Threat { get; set; }
        public IEnumerable<GroupAttribute> Attribute { get; set; }
        public IEnumerable<SecurityLabel> SecurityLabel { get; set; }
        public IEnumerable<Tag> Tag { get; set; }
        public IEnumerable<BucketAsset> BucketAsset { get; set; }
        public IEnumerable<Indicator> Indicator { get; set; }
        public IEnumerable<VictimAsset> VictimAsset { get; set; }
        public IEnumerable<Victim> Victim { get; set; }
        public IEnumerable<IndicatorType> IndicatorType { get; set; }
        public IEnumerable<Address> Address { get; set; }
        public IEnumerable<Observation> Observation { get; set; }
        public IEnumerable<Asn> Asn { get; set; }
        public IEnumerable<CidrBlock> CIDRBlock { get; set; }
        public IEnumerable<EmailAddress> EmailAddress { get; set; }
        public IEnumerable<File> File { get; set; }
        public IEnumerable<Host> Host { get; set; }
        public IEnumerable<Mutex> Mutex { get; set; }
        public IEnumerable<RegistryKey> RegistryKey { get; set; }
        public IEnumerable<Url> URL { get; set; }
        public IEnumerable<UserAgent> UserAgent { get; set; }
        public IEnumerable<DNSResolution> DnsResolution { get; set; }
        public IEnumerable<FileOccurence> FileOccurrence { get; set; }
    }

    public class FileOccurence
    {
        public string Id { get; set; }
        public string FileName { get; set; }
        public string Path { get; set; }
        public DateTime Date { get; set; }
    }

    public class DNSResolution
    {
        public DateTime ResolutionDate { get; set; }
        public IEnumerable<Address> Addresses { get; set; }
    }

    public class Observation
    {
        public int Count { get; set; }
        public DateTime DateObserved { get; set; }
    }
    
    public  class YourObservation  {

        public int Count { get; set; }
        public DateTime LastObserved { get; set; }
    public int YourCount { get; set; }
        public DateTime YourLastObserved { get; set; }

    }

    public class IndicatorType
    {
        public string Name { get; set; }
        public bool   Custom { get; set; }
        public bool   Parsable { get; set; }
        public string ApiBranch { get; set; }
        public string ApiEntity { get; set; }
        public string CasePreference { get; set; }
        public string Value1Label { get; set; }
        public string Value1Type { get; set; }
        public string Value1Option { get; set; }
        public string Value2Label { get; set; }
        public string Value2Type { get; set; }
        public string Value2Option { get; set; }
        public string Value3Label { get; set; }
        public string Value3Type { get; set; }
        public string Value3Option { get; set; }
        public IEnumerable<string> Regexes { get; set; }
    }

    public class Victim
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Org { get; set; }
        public string WebLink { get; set; }
    }

    public class VictimAsset
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public VictimAssetType Type { get; set; }
        public string WebLink { get; set; }
    }

    public enum VictimAssetType
    {
        EmailAddresses,
        NetworkAccounts,
        PhoneNumbers,
        SocialNetworks,
        WebSites
    }

    public enum IndicatorBuiltinType
    {
        Address,
        Asn,
        CidrBlock,
        EmailAddress,
        File,
        Host,
        Mutex,
        RegistryKey,
        Url,
        UserAgent
    }

    public class BucketAsset
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public BucketAssetType Type { get; set; }
        public string WebLink { get; set; }
    }

    public enum BucketAssetType
    {
        Handle,
        PhoneNumber,
        Url,
    }

    public class Tag
    {
        public string Name { get; set; }
        public string WebLink { get; set; }
    }

    public class SecurityLabel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime DateAdded { get; set; }
    }

    public class GroupAttribute
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime LastModified { get; set; }
        public bool Displayed { get; set; }
        public string Value { get; set; }
    }
    
    public class Group
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string OwnerName { get; set; }
        public DateTime DateAdded { get; set; }
        public string WebLink { get; set; }
        public Owner Owner { get; set; }
    }

    public class Adversary : Group
    {
        
    }
    public class Threat : Group
    {
        
    }
    public class Campaign : Group
    {
        public DateTime FirstSeen { get; set; }
    }
    public class Email : Group
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Subject { get; set; }
        public string Header { get; set; }
        public string Body { get; set; }
    }
    public class Event : Group
    {
        public DateTime EventDate { get; set; }
        [JsonConverter(typeof(SpacedEnumsConverter<EventStatus>))]
        public EventStatus Status { get; set; }
    }

    public enum EventStatus
    {
        NeedsReview,
        FalsePositive,
        NoFurtherAction,
        Escalated
    }

    public class Incident : Group
    {
        public DateTime EventDate { get; set; }
        [JsonConverter(typeof(SpacedEnumsConverter<IncidentStatus>))]
        public IncidentStatus Status { get; set; }   
    }

    public enum IncidentStatus
    {
        
        New,
        Open,
        Stalled,
        ContainmentAchieved,
        RestorationAchieved,
        IncidentReported,
        Closed,
        Rejected,
        Deleted
    }
    public class IntrusionSet : Group
    {
        
    }
    public class Report : Group
    {
        public string FileName { get; set; }
        public string Status { get; set; }
        public string Format { get; set; }
    }
    public class Signature : Group
    {
        public string FileName { get; set; }
        public SignatureFileType FileType { get; set; }
        public string FileText { get; set; }
    }

    public enum SignatureFileType
    {
        Snort,
        Suricata,
        YARA,
        ClamAV,
        OpenIOC,
        CybOX,
        Bro,
        Regex,
        SPL
    }
    
    public class OwnerMetric
    {
        public DateTime MetricDate { get; set; }
        
        public int TotalIndicator { get; set; }
        public int TotalHost { get; set; }
        public int TotalAddress { get; set; }
        public int TotalEmailAddress { get; set; }
        public int TotalFile { get; set; }
        public int TotalUrl { get; set; }
        public int TotalGroup { get; set; }
        public int TotalThreat { get; set; }
        public int TotalIncident { get; set; }
        public int TotalEmail { get; set; }
        public int TotalCampaign { get; set; }
        public int TotalAdversary { get; set; }
        public int TotalSignature { get; set; }
        public int TotalTask { get; set; }
        public int TotalDocument { get; set; }
        public int TotalTag { get; set; }
        public int TotalTrack { get; set; }
        public int TotalResult { get; set; }
        public int TotalIndicatorAttribute { get; set; }
        public int TotalGroupAttribute { get; set; }
        public double AverageIndicatorRating { get; set; }
        public double AverageIndicatorConfidence { get; set; }
        public int TotalEnrichedIndicator { get; set; }
        public int TotalGroupIndicator { get; set; }
        public int TotalObservationDaily { get; set; }
        public int TotalObservationIndicator { get; set; }
        public int TotalObservationAddress { get; set; }
        public int TotalObservationEmailAddress { get; set; }
        public int TotalObservationFile { get; set; }
        public int TotalObservationHost { get; set; }
        public int TotalObservationUrl { get; set; }
        public int TotalFalsePositiveDaily { get; set; }
        public int TotalFalsePositive { get; set; }
    }

    public class SingleData
    {
        public CidrBlock CIDRBlock;
        public Owner Owner { get; set; }
        public User User { get; set; }
        public Threat Threat { get; set; }
        public Adversary Adversary { get; set; }
        public Campaign Campaign { get; set; }
        public Document Document { get; set; }
        public Email Email { get; set; }
        
        public Event Event { get; set; }
        public Incident Incident { get; set; }
        public IntrusionSet IntrusionSet { get; set; }
        public Report Report { get; set; }
        public Signature Signature { get; set; }
        
        public GroupAttribute Attribute { get; set; }
        public SecurityLabel SecurityLabel { get; set; }
        public Tag Tag { get; set; }
        public BucketAsset BucketAsset { get; set; }
        public Indicator Indicator { get; set; }
        public VictimAsset VictimAsset { get; set; }
        public Victim Victim { get; set; }
        public Address Address { get; set; }
        public YourObservation ObservationCount { get; set; }
        public UserAgent UserAgent { get; set; }
        public Asn Asn { get; set; }
        public EmailAddress EmailAddress { get; set; }
        public File File { get; set; }
        public Host Host { get; set; }
        public Mutex Mutex { get; set; }
        public RegistryKey RegistryKey { get; set; }
        public Url Url { get; set; }
    }

    public class User
    {
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Pseudonym { get; set; }
        public string Role { get; set; }
    }

    public class Owner
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }
}