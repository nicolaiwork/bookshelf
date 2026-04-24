using System;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.PyBookIrc
{
    public class PyBookIrcRequestGenerator : IIndexerRequestGenerator
    {
        private readonly PyBookIrcSettings _settings;

        public PyBookIrcRequestGenerator(PyBookIrcSettings settings)
        {
            _settings = settings;
        }

        public IndexerPageableRequestChain GetRecentRequests()
        {
            // IRC has no RSS/recent concept — return empty chain.
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return BuildSearchChain(BuildQuery(searchCriteria));
        }

        public IndexerPageableRequestChain GetSearchRequests(AuthorSearchCriteria searchCriteria)
        {
            return BuildSearchChain(BuildQuery(searchCriteria));
        }

        private IndexerPageableRequestChain BuildSearchChain(string query)
        {
            var chain = new IndexerPageableRequestChain();

            if (string.IsNullOrWhiteSpace(query))
            {
                return chain;
            }

            var url = new HttpRequestBuilder(_settings.BaseUrl.Trim('/'))
                .Resource("/search")
                .Post()
                .Accept(HttpAccept.Json)
                .SetHeader("Authorization", $"Bearer {_settings.AuthToken}")
                .Build();

            url.SetContent($"{{\"query\":\"{EscapeJson(query)}\"}}");
            url.Headers.ContentType = "application/json";

            chain.Add(new[] { new IndexerRequest(url) });
            return chain;
        }

        private static string BuildQuery(BookSearchCriteria c)
        {
            var parts = new[] { c?.AuthorQuery, c?.BookQuery };
            return string.Join(" ", Array.FindAll(parts, p => !string.IsNullOrWhiteSpace(p)));
        }

        private static string BuildQuery(AuthorSearchCriteria c)
        {
            return c?.AuthorQuery ?? string.Empty;
        }

        private static string EscapeJson(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
