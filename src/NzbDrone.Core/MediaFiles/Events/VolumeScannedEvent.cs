using NzbDrone.Common.Messaging;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class VolumeScannedEvent : IEvent
    {
        public Volume Volume { get; private set; }

        public VolumeScannedEvent(Volume volume)
        {
            Volume = volume;
        }
    }
}
