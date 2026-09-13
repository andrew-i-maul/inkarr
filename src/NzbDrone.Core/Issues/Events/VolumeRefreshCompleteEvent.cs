using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Issues.Events
{
    public class VolumeRefreshCompleteEvent : IEvent
    {
        public Volume Volume { get; set; }

        public VolumeRefreshCompleteEvent(Volume volume)
        {
            Volume = volume;
        }
    }
}
