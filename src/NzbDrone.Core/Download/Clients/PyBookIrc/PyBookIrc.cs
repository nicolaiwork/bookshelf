using System.Collections.Generic;
using System.Threading.Tasks;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.PyBookIrc;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RemotePathMappings;

namespace NzbDrone.Core.Download.Clients.PyBookIrc
{
    public class PyBookIrc : DownloadClientBase<PyBookIrcSettings>
    {
        private readonly IPyBookIrcProxy _proxy;

        public PyBookIrc(IPyBookIrcProxy proxy,
                         IHttpClient httpClient,
                         IConfigService configService,
                         IDiskProvider diskProvider,
                         IRemotePathMappingService remotePathMappingService,
                         Logger logger)
            : base(configService, diskProvider, remotePathMappingService, logger)
        {
            _proxy = proxy;
        }

        public override string Name => "PyBookIrc";
        public override DownloadProtocol Protocol => DownloadProtocol.Irc;

        public override Task<string> Download(RemoteBook remoteBook, IIndexer indexer)
        {
            var url = remoteBook?.Release?.DownloadUrl;
            if (!PyBookIrcRelease.TryDecode(url, out var command))
            {
                throw new DownloadClientException(
                    "pybookirc: release DownloadUrl '{0}' is not a pybookirc:// URL — wrong indexer?",
                    url ?? "(null)");
            }

            var jobId = _proxy.EnqueueDownload(command, Settings);
            _logger.Info("pybookirc: enqueued '{0}' as job {1}", remoteBook.Release.Title, jobId);
            return Task.FromResult(jobId);
        }

        public override IEnumerable<DownloadClientItem> GetItems()
        {
            // TODO: poll daemon for tracked jobs. Next implementation step — requires
            // remembering job_ids across Bookshelf restarts (probably via Bookshelf's
            // own download history), since the daemon's /status only surfaces recent
            // completions, not every ever-seen job_id.
            return new List<DownloadClientItem>();
        }

        public override void RemoveItem(DownloadClientItem item, bool deleteData)
        {
            if (item?.DownloadId != null)
            {
                _proxy.RemoveJob(item.DownloadId, Settings);
            }
        }

        public override DownloadClientInfo GetStatus()
        {
            return new DownloadClientInfo
            {
                IsLocalhost = false,
                OutputRootFolders = new List<OsPath>()
            };
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            if (!_proxy.HealthCheck(Settings))
            {
                failures.Add(new ValidationFailure(string.Empty, "pybookirc daemon /health check failed — verify the URL and auth token"));
            }
        }
    }
}
