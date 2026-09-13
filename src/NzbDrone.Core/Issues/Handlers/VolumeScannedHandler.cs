using NLog;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Issues
{
    public class VolumeScannedHandler : IHandle<VolumeScannedEvent>,
                                        IHandle<VolumeScanSkippedEvent>
    {
        private readonly IIssueMonitoredService _issueMonitoredService;
        private readonly IVolumeService _volumeService;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly IIssueAddedService _issueAddedService;
        private readonly Logger _logger;

        public VolumeScannedHandler(IIssueMonitoredService issueMonitoredService,
                                    IVolumeService volumeService,
                                    IManageCommandQueue commandQueueManager,
                                    IIssueAddedService issueAddedService,
                                    Logger logger)
        {
            _issueMonitoredService = issueMonitoredService;
            _volumeService = volumeService;
            _commandQueueManager = commandQueueManager;
            _issueAddedService = issueAddedService;
            _logger = logger;
        }

        private void HandleScanEvents(Volume volume)
        {
            if (volume.AddOptions != null)
            {
                _logger.Info("[{0}] was recently added, performing post-add actions", volume.Name);
                _issueMonitoredService.SetIssueMonitoredStatus(volume, volume.AddOptions);

                if (volume.AddOptions.SearchForMissingIssues)
                {
                    _commandQueueManager.Push(new MissingIssueSearchCommand(volume.Id));
                }

                volume.AddOptions = null;
                _volumeService.RemoveAddOptions(volume);
            }

            _issueAddedService.SearchForRecentlyAdded(volume.Id);
        }

        public void Handle(VolumeScannedEvent message)
        {
            HandleScanEvents(message.Volume);
        }

        public void Handle(VolumeScanSkippedEvent message)
        {
            HandleScanEvents(message.Volume);
        }
    }
}
