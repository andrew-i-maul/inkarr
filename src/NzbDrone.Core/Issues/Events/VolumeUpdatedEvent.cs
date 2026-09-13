using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Issues.Events
{
    public class VolumeUpdatedEvent : IEvent
    {
        public Volume Volume { get; private set; }

        public VolumeUpdatedEvent(Volume volume)
        {
            Volume = volume;
        }
    }
}
