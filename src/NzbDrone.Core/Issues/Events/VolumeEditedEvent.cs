using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Issues.Events
{
    public class VolumeEditedEvent : IEvent
    {
        public Volume Volume { get; private set; }
        public Volume OldVolume { get; private set; }

        public VolumeEditedEvent(Volume volume, Volume oldVolume)
        {
            Volume = volume;
            OldVolume = oldVolume;
        }
    }
}
