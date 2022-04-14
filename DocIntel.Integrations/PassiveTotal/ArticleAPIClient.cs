using System.Collections.Generic;

using DocIntel.Integrations.PassiveTotal.Model;

namespace DocIntel.Integrations.PassiveTotal
{
    public class ArticleAPIClient
    {
        private readonly PassiveTotalAPI _client;

        public ArticleAPIClient(PassiveTotalAPI client)
        {
            _client = client;
        }

        public IEnumerable<Article> GetArticles(string sort = "created", string order = "desc", int page = 0)
        {
            return _client.GetObject<PassiveTotalReponse<IEnumerable<Article>>>(
                new { sort, order, page }, "articles", out var statusCode
            ).Articles;
        }

        public Article GetArticle(string id)
        {
            return _client.GetObject<Article>(
                null, "articles/" + id, out var statusCode
            );
        }
    }
}