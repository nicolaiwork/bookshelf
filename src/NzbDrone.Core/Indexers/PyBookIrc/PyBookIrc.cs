using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentValidation.Results;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.PyBookIrc
{
    public class PyBookIrcIndexer : HttpIndexerBase<PyBookIrcSettings>
    {
        public override string Name => "PyBookIrc";
        public override DownloadProtocol Protocol => DownloadProtocol.Irc;
        public override bool SupportsRss => false;
        public override TimeSpan RateLimit => TimeSpan.FromSeconds(30);

        public PyBookIrcIndexer(IHttpClient httpClient,
                         IIndexerStatusService indexerStatusService,
                         IConfigService configService,
                         IParsingService parsingService,
                         Logger logger)
            : base(httpClient, indexerStatusService, configService, parsingService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new PyBookIrcRequestGenerator(Settings);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new PyBookIrcParser(Settings);
        }

        // IRC has no RSS feed, so the default TestConnection (which calls
        // GetRecentRequests) is meaningless. Ping the daemon's /health
        // endpoint instead — it tells us the daemon is reachable AND that
        // the IRC connection underneath is up.
        public override async Task<ValidationFailure> TestConnection()
        {
            try
            {
                var builder = new HttpRequestBuilder(Settings.BaseUrl.TrimEnd('/'))
                    .Resource("/health")
                    .Accept(HttpAccept.Json);
                builder.SetHeader("Authorization", $"Bearer {Settings.AuthToken}");

                var response = await _httpClient.ExecuteAsync(builder.Build()).ConfigureAwait(false);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return new ValidationFailure(string.Empty, $"pybookirc daemon returned HTTP {(int)response.StatusCode} — IRC connection may be down");
                }
            }
            catch (HttpException ex) when (ex.Response?.StatusCode == HttpStatusCode.Unauthorized)
            {
                return new ValidationFailure("AuthToken", "Invalid bearer token");
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "pybookirc daemon unreachable at {0}", Settings.BaseUrl);
                return new ValidationFailure("BaseUrl", $"Unable to reach pybookirc daemon: {ex.Message}");
            }

            return null;
        }
    }

    public class PyBookIrcParser : IParseIndexerResponse
    {
        private readonly PyBookIrcSettings _settings;

        public PyBookIrcParser(PyBookIrcSettings settings)
        {
            _settings = settings;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releases = new List<ReleaseInfo>();
            if (string.IsNullOrWhiteSpace(indexerResponse.Content))
            {
                return releases;
            }

            var response = JsonConvert.DeserializeObject<SearchResponseDto>(indexerResponse.Content);
            if (response?.Results == null)
            {
                return releases;
            }

            var publishedAt = DateTime.UtcNow;
            foreach (var r in response.Results)
            {
                if (string.IsNullOrWhiteSpace(r.FullCommand))
                {
                    continue;
                }

                var downloadUrl = PyBookIrcRelease.Encode(r.FullCommand);
                var title = BuildTitle(r);

                releases.Add(new ReleaseInfo
                {
                    Title = title,
                    DownloadUrl = downloadUrl,
                    InfoUrl = downloadUrl,
                    Guid = $"pybookirc:{r.ServerName}:{r.FullCommand.GetHashCode():X8}",
                    Size = r.SizeBytes ?? 0,
                    PublishDate = publishedAt,
                    DownloadProtocol = DownloadProtocol.Irc,
                });
            }

            return releases;
        }

        private static string BuildTitle(SearchResultDto r)
        {
            var authorTitle = string.Join(" - ",
                new[] { r.Author, r.Title }.Where(s => !string.IsNullOrWhiteSpace(s)));
            return string.IsNullOrWhiteSpace(r.Format) ? authorTitle : $"{authorTitle} [{r.Format}]";
        }
    }

    internal class SearchResponseDto
    {
        [JsonProperty("search_id")]
        public string SearchId { get; set; }

        [JsonProperty("results")]
        public List<SearchResultDto> Results { get; set; }
    }

    internal class SearchResultDto
    {
        [JsonProperty("server_name")]
        public string ServerName { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("format")]
        public string Format { get; set; }

        [JsonProperty("size_bytes")]
        public long? SizeBytes { get; set; }

        [JsonProperty("full_command")]
        public string FullCommand { get; set; }
    }
}
