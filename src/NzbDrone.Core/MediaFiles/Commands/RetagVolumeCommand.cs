using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.MediaFiles.Commands
{
    public class RetagVolumeCommand : Command
    {
        public List<int> VolumeIds { get; set; }
        public bool UpdateCovers { get; set; }
        public bool EmbedMetadata { get; set; }

        public override bool SendUpdatesToClient => true;
        public override bool RequiresDiskAccess => true;

        public RetagVolumeCommand()
        {
            VolumeIds = new List<int>();
        }
    }
}
