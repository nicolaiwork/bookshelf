using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.PyBookIrc
{
    public class PyBookIrc : HttpIndexerBase<PyBookIrcSettings>
    {
        public override string Name => "PyBookIrc";
        public override DownloadProtocol Protocol => DownloadProtocol.Irc;

        public PyBookIrc(IHttpClient httpClient,
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
            // TODO: parse daemon's /search response. The daemon returns a JSON array of
            // SearchResult objects; each becomes a ReleaseInfo whose DownloadUrl encodes
            // the IRC command string (the bot name + filename) so the download client
            // can replay it against POST /download.
            return new List<ReleaseInfo>();
        }
    }
}
