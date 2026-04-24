using NLog;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.Download.Clients.PyBookIrc
{
    public interface IPyBookIrcProxy
    {
        string EnqueueDownload(string command, PyBookIrcSettings settings);
        PyBookIrcJob GetJob(string jobId, PyBookIrcSettings settings);
        void RemoveJob(string jobId, PyBookIrcSettings settings);
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
            // TODO: POST /download with {"command": "..."} and return job_id.
            return null;
        }

        public PyBookIrcJob GetJob(string jobId, PyBookIrcSettings settings)
        {
            // TODO: GET /download/{jobId} and deserialize into PyBookIrcJob.
            return null;
        }

        public void RemoveJob(string jobId, PyBookIrcSettings settings)
        {
            // TODO: DELETE /download/{jobId}.
        }

        public bool HealthCheck(PyBookIrcSettings settings)
        {
            // TODO: GET /health — 200 -> true, anything else -> false.
            return false;
        }
    }

    public class PyBookIrcJob
    {
        public string JobId { get; set; }
        public string Status { get; set; }
        public string OutputPath { get; set; }
        public string Title { get; set; }
        public long? TotalBytes { get; set; }
        public long? TransferredBytes { get; set; }
        public string ErrorMessage { get; set; }
    }
}
