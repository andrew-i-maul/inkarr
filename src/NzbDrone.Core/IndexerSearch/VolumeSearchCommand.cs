using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.IndexerSearch
{
    public class VolumeSearchCommand : Command
    {
        public int VolumeId { get; set; }

        public override bool SendUpdatesToClient => true;
    }
}
