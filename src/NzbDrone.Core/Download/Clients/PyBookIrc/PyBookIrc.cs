using System;
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
            var clientInfo = DownloadClientItemClientInfo.FromDownloadClient(this, false);

            foreach (var job in _proxy.ListJobs(Settings))
            {
                var mapped = MapJob(job, clientInfo);
                if (mapped != null)
                {
                    yield return mapped;
                }
            }
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

        private DownloadClientItem MapJob(PyBookIrcJob job, DownloadClientItemClientInfo clientInfo)
        {
            if (string.IsNullOrWhiteSpace(job?.JobId))
            {
                return null;
            }

            var item = new DownloadClientItem
            {
                DownloadClientInfo = clientInfo,
                DownloadId = job.JobId,
                Title = job.Filename ?? job.Command ?? job.JobId,
                Status = MapStatus(job.Status),
                Message = job.Error,
                CanBeRemoved = true,
                CanMoveFiles = true,
                TotalSize = job.SizeBytesTotal ?? 0,
                RemainingSize = Math.Max(0, (job.SizeBytesTotal ?? 0) - (job.SizeBytesReceived ?? 0)),
            };

            if (!string.IsNullOrWhiteSpace(job.OutputPath))
            {
                var mapped = _remotePathMappingService.RemapRemoteToLocal(Settings.BaseUrl, new OsPath(job.OutputPath));
                item.OutputPath = mapped;
            }

            return item;
        }

        private static DownloadItemStatus MapStatus(string status) =>
            status switch
            {
                "queued" => DownloadItemStatus.Queued,
                "downloading" => DownloadItemStatus.Downloading,
                "completed" => DownloadItemStatus.Completed,
                "failed" or "timeout" or "cancelled" => DownloadItemStatus.Failed,
                _ => DownloadItemStatus.Warning,
            };
    }
}
