using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;

using DocIntel.Integrations.ThreatConnect.Model;

namespace DocIntel.Integrations.ThreatConnect
{
    public class IndicatorAPIClient : APISubclient
    {
        private readonly APIClient _apiClient;

        public IndicatorAPIClient(APIClient apiClient)
        {
            _apiClient = apiClient;
        }
        
        public Task<Response> GetIndicators(IndicatorParameter filter = null)
        {
            var parameters = BuildParameters(filter);
            return _apiClient.Query<Response>($"/v2/indicators", parameters: parameters);
        }
        
        public Task<Response> GetIndicatorTypes(IndicatorParameter filter = null)
        {
            var parameters = BuildParameters(filter);
            return _apiClient.Query<Response>($"/v2/types/indicatorTypes", parameters: parameters);
        }
        
        public Task<Response> GetIndicators(IndicatorBuiltinType type, IndicatorParameter filter = null)
        {
            var parameters = BuildParameters(filter);
            return _apiClient.Query<Response>($"/v2/indicators/{IndicatorTypeStr(type)}", parameters: parameters);
        }
        
        public Task<SingleResponse> GetIndicator(IndicatorBuiltinType type, string id, IndicatorParameter filter = null)
        {
            var parameters = BuildParameters(filter);
            return _apiClient.Query<SingleResponse>($"/v2/indicators/{IndicatorTypeStr(type)}/{HttpUtility.UrlEncode(id)}", parameters: parameters);
        }
        
        public Task<Response> GetIndicatorTags(IndicatorBuiltinType type, string id, IndicatorParameter filter = null)
        {
            var parameters = BuildParameters(filter);
            return _apiClient.Query<Response>($"/v2/indicators/{IndicatorTypeStr(type)}/{HttpUtility.UrlEncode(id)}/tags", parameters: parameters);
        }
        
        public Task<Response> GetIndicatorObservations(IndicatorBuiltinType type, string id, IndicatorParameter filter = null)
        {
            var parameters = BuildParameters(filter);
            return _apiClient.Query<Response>($"/v2/indicators/{IndicatorTypeStr(type)}/{HttpUtility.UrlEncode(id)}/observations", parameters: parameters);
        }

        public Task<Response> GetIndicatorAttributes(IndicatorBuiltinType type, string id, IndicatorParameter filter = null)
        {
            var parameters = BuildParameters(filter);
            return _apiClient.Query<Response>($"/v2/indicators/{IndicatorTypeStr(type)}/{HttpUtility.UrlEncode(id)}/attributes", parameters: parameters);
        }
        public Task<Response> GetIndicatorAttributesSecurityLabels(IndicatorBuiltinType type, string id, string attrId, IndicatorParameter filter = null)
        {
            var parameters = BuildParameters(filter);
            return _apiClient.Query<Response>($"/v2/indicators/{IndicatorTypeStr(type)}/{HttpUtility.UrlEncode(id)}/attributes/{attrId}/securityLabels", parameters: parameters);
        }
        public Task<Response> GetAddressDNSResolutions(string id, IndicatorParameter filter = null)
        {
            var parameters = BuildParameters(filter);
            return _apiClient.Query<Response>($"/v2/indicators/addresses/{HttpUtility.UrlEncode(id)}/dnsResolutions", parameters: parameters);
        }
        public Task<Response> GetFileOccurrences(string id, IndicatorParameter filter = null)
        {
            var parameters = BuildParameters(filter);
            return _apiClient.Query<Response>($"/v2/indicators/files/{HttpUtility.UrlEncode(id)}/fileOccurrences", parameters: parameters);
        }
        public Task<Response> GetHostDNSResolutions(string id, IndicatorParameter filter = null)
        {
            var parameters = BuildParameters(filter);
            return _apiClient.Query<Response>($"/v2/indicators/hosts/{HttpUtility.UrlEncode(id)}/dnsResolutions", parameters: parameters);
        }

        public Task<SingleResponse> GetIndicatorYourObservations(IndicatorBuiltinType type, string id, IndicatorParameter filter = null)
        {
            var parameters = BuildParameters(filter);
            return _apiClient.Query<SingleResponse>($"/v2/indicators/{IndicatorTypeStr(type)}/{HttpUtility.UrlEncode(id)}/observationCount", parameters: parameters);
        }
        
        public Task<Response> GetIndicatorSecurityLabels(IndicatorBuiltinType type, string id, IndicatorParameter filter = null)
        {
            var parameters = BuildParameters(filter);
            return _apiClient.Query<Response>($"/v2/indicators/{IndicatorTypeStr(type)}/{HttpUtility.UrlEncode(id)}/securityLabels", parameters: parameters);
        }
        
        public Task<Response> GetIndicatorObserved(ObservationParameter filter = null)
        {
            var parameters = BuildParameters(filter);
            return _apiClient.Query<Response>($"/v2/indicators/observed", parameters: parameters);
        }

        public Task<Response> GetGroupAssociations(IndicatorBuiltinType type, string id, IndicatorParameter filter = null)
        {
            var parameters = BuildParameters(filter);
            return _apiClient.Query<Response>($"/v2/indicators/{IndicatorTypeStr(type)}/{HttpUtility.UrlEncode(id)}/groups", parameters: parameters);
        }

        public Task<Response> GetIndicatorAssociations(IndicatorBuiltinType type, string id, IndicatorParameter filter = null)
        {
            var parameters = BuildParameters(filter);
            return _apiClient.Query<Response>($"/v2/indicators/{IndicatorTypeStr(type)}/{HttpUtility.UrlEncode(id)}/indicators", parameters: parameters);
        }

        public Task<Response> GetVictimAssetAssociations(IndicatorBuiltinType type, string id, IndicatorParameter filter = null)
        {
            var parameters = BuildParameters(filter);
            return _apiClient.Query<Response>($"/v2/indicators/{IndicatorTypeStr(type)}/{HttpUtility.UrlEncode(id)}/victimAssets", parameters: parameters);
        }

        public Task<Response> GetVictimAssociations(IndicatorBuiltinType type, string id, IndicatorParameter filter = null)
        {
            var parameters = BuildParameters(filter);
            return _apiClient.Query<Response>($"/v2/indicators/{IndicatorTypeStr(type)}/{HttpUtility.UrlEncode(id)}/victims", parameters: parameters);
        }

        public Task<SingleResponse> GetIndicator(string customType, string id, IndicatorParameter filter = null)
        {
            var parameters = BuildParameters(filter);
            return _apiClient.Query<SingleResponse>($"/v2/indicators/{customType}/{id}", parameters: parameters);
        }
        public Task<Response> GetIndicatorOwners(string customType, string id, IndicatorParameter filter = null)
        {
            var parameters = BuildParameters(filter);
            return _apiClient.Query<Response>($"/v2/indicators/{customType}/{id}/owners", parameters: parameters);
        }
        
        private static string IndicatorTypeStr(IndicatorBuiltinType indicatorType)
        {
            var groupTypeStr = indicatorType switch
            {
                IndicatorBuiltinType.Address => "addresses",
                IndicatorBuiltinType.Asn => "asns",
                IndicatorBuiltinType.File => "files",
                IndicatorBuiltinType.Host => "hosts",
                IndicatorBuiltinType.Mutex => "mutexes",
                IndicatorBuiltinType.Url => "urls",
                IndicatorBuiltinType.CidrBlock=> "cidrBlocks",
                IndicatorBuiltinType.RegistryKey => "registryKeys",
                IndicatorBuiltinType.EmailAddress => "emailAddresses",
                IndicatorBuiltinType.UserAgent => "userAgents",
                _ => throw new NotImplementedException()
            };
            return groupTypeStr;
        }

        private Dictionary<string, string> BuildParameters(ObservationParameter filter)
        {
            var parameters = new Dictionary<string, string>();
            if (filter == null)
                return parameters;

            if (filter.DateObserved != null)
            {
                parameters.Add("dateObserved", $"{filter.DateObserved:yyyy-MM-ddTHH:mm:ssZ}");
            }

            return parameters;
        }

        private Dictionary<string, string> BuildParameters(IndicatorParameter filter)
        {
            var parameters = new Dictionary<string, string>();
            if (filter == null)
                return parameters;
            
            if (!string.IsNullOrEmpty(filter.Owner)) parameters.Add("owner", filter.Owner);
            if (filter.Start > 0) parameters.Add("resultStart", filter.Start.ToString());
            if (filter.Limit > 0) parameters.Add("resultLimit", filter.Limit.ToString());
            if (filter.IncludeAdditional) parameters.Add("includeAdditional", filter.IncludeAdditional ? "true" : "false");
            
            var filters = new List<string>();

            if (!filter.Active)
            {
                filters.Add("active=false");
            }
            AddStringFilter(filters, filter.Summary, "summary");
            AddStringFilter(filters, filter.ModifiedSince, "modifiedSince");
            AddDateTimeFilter(filters, filter.DateAdded, "dateAdded");
            AddIntFilter(filters, filter.Rating, "rating");
            AddIntFilter(filters, filter.Confidence, "confidence");
            AddIntFilter(filters, filter.ThreatAssessScore, "threatAssessScore");
            AddDoubleFilter(filters, filter.ThreatAssessRating, "threatAssessRating");
            AddDoubleFilter(filters, filter.ThreatAssessConfidence, "threatAssessConfidence");
            AddIntFilter(filters, filter.FalsePositive, "falsePositive");
            
            // Address Specific Filters
            AddStringFilter(filters, filter.City, "city");
            AddStringFilter(filters, filter.CountryCode, "countryCode");
            AddStringFilter(filters, filter.CountryName, "countryName");
            AddStringFilter(filters, filter.Organization, "organization");
            AddStringFilter(filters, filter.State, "state");
            AddStringFilter(filters, filter.Timezone, "timezone");
            AddStringFilter(filters, filter.Asn, "asn");
                
            // Host Specific Filters    
            if (filter.WhoisActive != null)
                filters.Add($"whoisActive={((bool) filter.WhoisActive ? "true" : "false")}");
            if (filter.DnsActive != null)
                filters.Add($"dnsActive={((bool) filter.DnsActive ? "true" : "false")}");
            
            parameters.Add("filters",string.Join(",", filters));
            
            return parameters;
        }
    }
}