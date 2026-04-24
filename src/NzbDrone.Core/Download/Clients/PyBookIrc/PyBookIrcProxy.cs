using System;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Download.Clients.PyBookIrc
{
    public interface IPyBookIrcProxy
    {
        string EnqueueDownload(string command, PyBookIrcSettings settings);
        PyBookIrcJob GetJob(string jobId, PyBookIrcSettings settings);
        System.Collections.Generic.IList<PyBookIrcJob> ListJobs(PyBookIrcSettings settings);
        bool RemoveJob(string jobId, PyBookIrcSettings settings);
        bool HealthCheck(PyBookIrcSettings settings);
    }

    public class PyBookIrcProxy : IPyBookIrcProxy
    {
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public PyBookIrcProxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public string EnqueueDownload(string command, PyBookIrcSettings settings)
        {
            var builder = Build(settings, "/download").Post();
            builder.Headers.ContentType = "application/json";
            var httpRequest = builder.Build();
            httpRequest.SetContent(Json.ToJson(new { command }));

            var response = Execute(httpRequest, settings);
            var accepted = Json.Deserialize<DownloadAcceptedDto>(response.Content);
            if (accepted == null || accepted.JobId.IsNullOrWhiteSpace())
            {
                throw new DownloadClientException("pybookirc: daemon accepted POST /download but returned no job_id");
            }

            return accepted.JobId;
        }

        public PyBookIrcJob GetJob(string jobId, PyBookIrcSettings settings)
        {
            var request = Build(settings, $"/download/{jobId}").Build();
            HttpResponse response;
            try
            {
                response = _httpClient.Execute(request);
            }
            catch (HttpException ex) when (ex.Response?.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            return Json.Deserialize<PyBookIrcJob>(response.Content);
        }

        public System.Collections.Generic.IList<PyBookIrcJob> ListJobs(PyBookIrcSettings settings)
        {
            var request = Build(settings, "/downloads").Build();
            var response = Execute(request, settings);
            var jobs = JsonConvert.DeserializeObject<System.Collections.Generic.List<PyBookIrcJob>>(response.Content);
            return jobs ?? new System.Collections.Generic.List<PyBookIrcJob>();
        }

        public bool RemoveJob(string jobId, PyBookIrcSettings settings)
        {
            var request = Build(settings, $"/download/{jobId}");
            request.Method = HttpMethod.Delete;
            try
            {
                _httpClient.Execute(request.Build());
                return true;
            }
            catch (HttpException ex) when (ex.Response?.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public bool HealthCheck(PyBookIrcSettings settings)
        {
            try
            {
                var response = _httpClient.Execute(Build(settings, "/health").Build());
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return false;
                }

                var health = Json.Deserialize<HealthDto>(response.Content);
                return health != null && string.Equals(health.Status, "ok", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "pybookirc health check failed");
                return false;
            }
        }

        private HttpRequestBuilder Build(PyBookIrcSettings settings, string resource)
        {
            var builder = new HttpRequestBuilder(settings.BaseUrl.TrimEnd('/'))
                .Resource(resource)
                .Accept(HttpAccept.Json);

            builder.SetHeader("Authorization", $"Bearer {settings.AuthToken}");
            return builder;
        }

        private HttpResponse Execute(HttpRequest request, PyBookIrcSettings settings)
        {
            try
            {
                return _httpClient.Execute(request);
            }
            catch (HttpException ex)
            {
                throw new DownloadClientException("pybookirc: HTTP {0} from daemon", ex, (int)(ex.Response?.StatusCode ?? 0));
            }
            catch (HttpRequestException ex)
            {
                throw new DownloadClientUnavailableException("pybookirc: unable to reach daemon at {0}", ex, settings.BaseUrl);
            }
            catch (WebException ex)
            {
                throw new DownloadClientUnavailableException("pybookirc: unable to reach daemon at {0}", ex, settings.BaseUrl);
            }
        }
    }

    public class PyBookIrcJob
    {
        [JsonProperty("job_id")]
        public string JobId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("command")]
        public string Command { get; set; }

        [JsonProperty("filename")]
        public string Filename { get; set; }

        [JsonProperty("size_bytes_total")]
        public long? SizeBytesTotal { get; set; }

        [JsonProperty("size_bytes_received")]
        public long? SizeBytesReceived { get; set; }

        [JsonProperty("percent")]
        public double? Percent { get; set; }

        [JsonProperty("output_path")]
        public string OutputPath { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("error_detail")]
        public string ErrorDetail { get; set; }
    }

    internal class DownloadAcceptedDto
    {
        [JsonProperty("job_id")]
        public string JobId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }

    internal class HealthDto
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("irc_connected")]
        public bool IrcConnected { get; set; }
    }
}
