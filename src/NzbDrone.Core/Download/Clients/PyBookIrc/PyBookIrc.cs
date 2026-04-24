using System.Collections.Generic;
using System.Threading.Tasks;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
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
            // TODO: pull the IRC command string out of remoteBook.Release.DownloadUrl
            // (encoded by the PyBookIrc indexer) and submit it via _proxy.EnqueueDownload.
            return Task.FromResult<string>(null);
        }

        public override IEnumerable<DownloadClientItem> GetItems()
        {
            // TODO: poll the daemon's per-job status endpoints for each tracked job
            // and map into DownloadClientItem. Initial implementation can rely on
            // GET /status for an aggregate view.
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
                failures.Add(new ValidationFailure(string.Empty, "Daemon health check failed"));
            }
        }
    }
}
