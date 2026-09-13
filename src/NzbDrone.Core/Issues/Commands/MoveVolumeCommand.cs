using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Issues.Commands
{
    public class MoveVolumeCommand : Command
    {
        public int VolumeId { get; set; }
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }

        public override bool SendUpdatesToClient => true;
        public override bool RequiresDiskAccess => true;
    }
}
