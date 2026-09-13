using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.IndexerSearch
{
    public class CutoffUnmetIssueSearchCommand : Command
    {
        public int? VolumeId { get; set; }

        public override bool SendUpdatesToClient => true;

        public CutoffUnmetIssueSearchCommand()
        {
        }

        public CutoffUnmetIssueSearchCommand(int volumeId)
        {
            VolumeId = volumeId;
        }
    }
}
