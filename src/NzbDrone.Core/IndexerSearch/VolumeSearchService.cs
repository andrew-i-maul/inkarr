using NLog;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.IndexerSearch
{
    public class VolumeSearchService : IExecute<VolumeSearchCommand>
    {
        private readonly ISearchForReleases _releaseSearchService;
        private readonly IProcessDownloadDecisions _processDownloadDecisions;
        private readonly Logger _logger;

        public VolumeSearchService(ISearchForReleases releaseSearchService,
            IProcessDownloadDecisions processDownloadDecisions,
            Logger logger)
        {
            _releaseSearchService = releaseSearchService;
            _processDownloadDecisions = processDownloadDecisions;
            _logger = logger;
        }

        public void Execute(VolumeSearchCommand message)
        {
            var decisions = _releaseSearchService.VolumeSearch(message.VolumeId, false, message.Trigger == CommandTrigger.Manual, false).GetAwaiter().GetResult();
            var processed = _processDownloadDecisions.ProcessDecisions(decisions).GetAwaiter().GetResult();

            _logger.ProgressInfo("Volume search completed. {0} reports downloaded.", processed.Grabbed.Count);
        }
    }
}
