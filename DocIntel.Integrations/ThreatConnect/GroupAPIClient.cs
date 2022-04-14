using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using DocIntel.Integrations.ThreatConnect.Model;

namespace DocIntel.Integrations.ThreatConnect
{
    public class GroupAPIClient : APISubclient
    {
        private readonly APIClient _apiClient;

        public GroupAPIClient(APIClient apiClient)
        {
            _apiClient = apiClient;
        }

        public Task<Response> GetGroups(GroupParameter filter = null)
        {
            var parameters = BuildParameters(filter);

            return _apiClient.Query<Response>($"/v2/groups", parameters: parameters);
        }
        
        private Dictionary<string, string> BuildParameters(GroupParameter filter)
        {
            var parameters = new Dictionary<string, string>();
            if (filter == null)
                return parameters;
            
            if (!string.IsNullOrEmpty(filter.Owner)) parameters.Add("owner", filter.Owner);
            if (filter.Start > 0) parameters.Add("resultStart", filter.Start.ToString());
            if (filter.Limit > 0) parameters.Add("resultLimit", filter.Limit.ToString());
            
            var filters = new List<string>();
            AddDateTimeFilter(filters, filter.DateAdded, "dateAdded");
            AddStringFilter(filters, filter.Name, "name");
            
            parameters.Add("filters",string.Join(",", filters));
            
            return parameters;
        }

        private static string GroupTypeStr(GroupType groupType)
        {
            var groupTypeStr = groupType switch
            {
                GroupType.Adversaries => "adversaries",
                GroupType.Campaigns => "campaigns",
                GroupType.Documents => "documents",
                GroupType.Emails => "emails",
                GroupType.Events => "events",
                GroupType.Incidents => "incidents",
                GroupType.IntrusionSets => "intrusionSets",
                GroupType.Reports => "reports",
                GroupType.Signatures => "signatures",
                GroupType.Threats => "threats",
                _ => throw new NotImplementedException()
            };
            return groupTypeStr;
        }

        public Task<Response> GetGroups(GroupType groupType, GroupParameter parameter = null)
        {
            var parameters = BuildParameters(parameter);
            return _apiClient.Query<Response>($"/v2/groups/{GroupTypeStr(groupType)}", parameters: parameters);
        }
        public Task<Stream> DownloadGroups(GroupType groupType, string id, GroupParameter parameter = null)
        {
            var parameters = BuildParameters(parameter);
            return _apiClient.Download($"/v2/groups/{GroupTypeStr(groupType)}/{id}/download", parameters: parameters);
        }
        public Task<SingleResponse> GetGroup(GroupType groupType, string id, GroupParameter parameter = null)
        {
            var parameters = BuildParameters(parameter);
            return _apiClient.Query<SingleResponse>($"/v2/groups/{GroupTypeStr(groupType)}/{id}", parameters: parameters);
        }
        public Task<Response> GetGroupAttributes(GroupType groupType, string id, GroupParameter parameter = null)
        {
            var parameters = BuildParameters(parameter);
            return _apiClient.Query<Response>($"/v2/groups/{GroupTypeStr(groupType)}/{id}/attributes", parameters: parameters);
        }
        public Task<Response> GetGroupAttributeSecurityLabels(GroupType groupType, string id, string attrId, GroupParameter parameter = null)
        {
            var parameters = BuildParameters(parameter);
            return _apiClient.Query<Response>($"/v2/groups/{GroupTypeStr(groupType)}/{id}/attributes/{attrId}/securityLabels", parameters: parameters);
        }
        public Task<Response> GetGroupSecurityLabels(GroupType groupType, string id, GroupParameter parameter = null)
        {
            var parameters = BuildParameters(parameter);
            return _apiClient.Query<Response>($"/v2/groups/{GroupTypeStr(groupType)}/{id}/securityLabels", parameters: parameters);
        }
        public Task<Response> GetGroupTags(GroupType groupType, string id, GroupParameter parameter = null)
        {
            var parameters = BuildParameters(parameter);
            return _apiClient.Query<Response>($"/v2/groups/{GroupTypeStr(groupType)}/{id}/tags", parameters: parameters);
        }
        public Task<Response> GetGroupAssociations(GroupType groupType, string id, GroupParameter parameter = null)
        {
            var parameters = BuildParameters(parameter);
            return _apiClient.Query<Response>($"/v2/groups/{GroupTypeStr(groupType)}/{id}/groups", parameters: parameters);
        }
        public Task<Response> GetGroupIndicators(GroupType groupType, string id, GroupParameter parameter = null)
        {
            var parameters = BuildParameters(parameter);
            return _apiClient.Query<Response>($"/v2/groups/{GroupTypeStr(groupType)}/{id}/indicators", parameters: parameters);
        }
        public Task<Response> GetGroupVictimAssets(GroupType groupType, string id, GroupParameter parameter = null)
        {
            var parameters = BuildParameters(parameter);
            return _apiClient.Query<Response>($"/v2/groups/{GroupTypeStr(groupType)}/{id}/victimAssets", parameters: parameters);
        }
        public Task<Response> GetGroupVictims(GroupType groupType, string id, GroupParameter parameter = null)
        {
            var parameters = BuildParameters(parameter);
            return _apiClient.Query<Response>($"/v2/groups/{GroupTypeStr(groupType)}/{id}/victims", parameters: parameters);
        }
        public Task<Response> GetAdversaryAssets(string id, GroupParameter parameter = null)
        {
            var parameters = BuildParameters(parameter);
            return _apiClient.Query<Response>($"/v2/groups/adversaries/{id}/adversaryAssets", parameters: parameters);
        }
    }

    public enum GroupType
    {
        Adversaries,
        Campaigns,
        Documents,
        Emails,
        Events,
        Incidents,
        IntrusionSets,
        Reports,
        Signatures,
        Threats
    }
}