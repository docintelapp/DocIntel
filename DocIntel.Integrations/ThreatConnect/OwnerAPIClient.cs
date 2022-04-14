using System.Threading.Tasks;

using DocIntel.Integrations.ThreatConnect.Model;

namespace DocIntel.Integrations.ThreatConnect
{
    public class OwnerAPIClient
    {
        private readonly APIClient _apiClient;

        public OwnerAPIClient(APIClient apiClient)
        {
            _apiClient = apiClient;
        }

        public Task<Response> GetOwners()
        {
            return _apiClient.Query<Response>("/v2/owners");
        }

        public Task<SingleResponse> GetOwner(string id)
        {
            return _apiClient.Query<SingleResponse>($"/v2/owners/{id}");
        }

        public Task<Response> GetOwnerMetrics()
        {
            return _apiClient.Query<Response>($"/v2/owners/metrics");
        }

        public Task<Response> GetOwnerMetrics(string id)
        {
            return _apiClient.Query<Response>($"/v2/owners/{id}/metrics");
        }

        public Task<SingleResponse> GetMine()
        {
            return _apiClient.Query<SingleResponse>($"/v2/owners/mine");
        }
        
        public Task<Response> GetMineMembers()
        {
            return _apiClient.Query<Response>($"/v2/owners/mine/members");
        }

        public Task<SingleResponse> GetWhoAmI()
        {
            return _apiClient.Query<SingleResponse>($"/v2/whoami");
        }
    }
}