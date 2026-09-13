using NzbDrone.Core.Issues.Commands;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Issues
{
    public class VolumeAddedHandler : IHandle<VolumeAddedEvent>,
                                      IHandle<VolumesImportedEvent>
    {
        private readonly IManageCommandQueue _commandQueueManager;

        public VolumeAddedHandler(IManageCommandQueue commandQueueManager)
        {
            _commandQueueManager = commandQueueManager;
        }

        public void Handle(VolumeAddedEvent message)
        {
            if (message.DoRefresh)
            {
                _commandQueueManager.Push(new RefreshVolumeCommand(message.Volume.Id, true));
            }
        }

        public void Handle(VolumesImportedEvent message)
        {
            if (message.DoRefresh)
            {
                _commandQueueManager.Push(new BulkRefreshVolumeCommand(message.VolumeIds, true));
            }
        }
    }
}
