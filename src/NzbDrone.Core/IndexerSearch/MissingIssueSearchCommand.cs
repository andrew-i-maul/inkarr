using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.IndexerSearch
{
    public class MissingIssueSearchCommand : Command
    {
        public int? VolumeId { get; set; }

        public override bool SendUpdatesToClient => true;

        public MissingIssueSearchCommand()
        {
        }

        public MissingIssueSearchCommand(int volumeId)
        {
            VolumeId = volumeId;
        }
    }
}
