using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.MediaFiles.Commands
{
    public class RenameVolumeCommand : Command
    {
        public List<int> VolumeIds { get; set; }

        public override bool SendUpdatesToClient => true;
        public override bool RequiresDiskAccess => true;

        public RenameVolumeCommand()
        {
            VolumeIds = new List<int>();
        }
    }
}
