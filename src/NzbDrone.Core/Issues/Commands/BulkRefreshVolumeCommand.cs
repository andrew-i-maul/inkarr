using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Issues.Commands
{
    public class BulkRefreshVolumeCommand : Command
    {
        public BulkRefreshVolumeCommand()
        {
        }

        public BulkRefreshVolumeCommand(List<int> volumeIds, bool areNewVolumes = false)
        {
            VolumeIds = volumeIds;
            AreNewVolumes = areNewVolumes;
        }

        public List<int> VolumeIds { get; set; }
        public bool AreNewVolumes { get; set; }

        public override bool SendUpdatesToClient => true;

        public override bool UpdateScheduledTask => false;
    }
}
