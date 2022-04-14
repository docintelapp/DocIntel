using System.Collections.Generic;
using System.Threading.Tasks;

using DocIntel.Integrations.PassiveTotal.Model;

namespace DocIntel.Integrations.PassiveTotal
{
    public class WhoisAPIClient
    {
        private readonly PassiveTotalAPI _client;

        public WhoisAPIClient(PassiveTotalAPI client)
        {
            _client = client;
        }
        

        public WhoisResponse GetWhois(string query)
        {
            return _client.GetObject<WhoisResponse>(new { query }, "whois", out var status);
        }
        

        public IEnumerable<WhoisResponse> GetWhoisHistory(string query)
        {
            return _client.GetObject<PassiveTotalReponse<IEnumerable<WhoisResponse>>>(new { query, history = true }, "whois", out var status).Results;
        }
         
        public async Task<WhoisKeywordResponse> SearchWhoisKeyword() {
            return null;
        }
        
        public async Task<WhoisSearchResponse> SearchWhois() {
            return null;
        }
    }
}