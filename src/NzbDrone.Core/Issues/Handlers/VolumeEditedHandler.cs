using NzbDrone.Core.Issues.Commands;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Issues
{
    public class VolumeEditedService : IHandle<VolumeEditedEvent>
    {
        private readonly IManageCommandQueue _commandQueueManager;

        public VolumeEditedService(IManageCommandQueue commandQueueManager)
        {
            _commandQueueManager = commandQueueManager;
        }

        public void Handle(VolumeEditedEvent message)
        {
            // Refresh Volume is we change IssueType Preferences
            if (message.Volume.MetadataProfileId != message.OldVolume.MetadataProfileId)
            {
                _commandQueueManager.Push(new RefreshVolumeCommand(message.Volume.Id, false));
            }
        }
    }
}
