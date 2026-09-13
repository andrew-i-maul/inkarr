using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Issues.Commands
{
    public class RefreshVolumeCommand : Command
    {
        public int? VolumeId { get; set; }
        public bool IsNewVolume { get; set; }

        public RefreshVolumeCommand()
        {
        }

        public RefreshVolumeCommand(int? volumeId, bool isNewVolume = false)
        {
            VolumeId = volumeId;
            IsNewVolume = isNewVolume;
        }

        public override bool SendUpdatesToClient => true;

        public override bool UpdateScheduledTask => !VolumeId.HasValue;

        public override string CompletionMessage => "Completed";
    }
}
