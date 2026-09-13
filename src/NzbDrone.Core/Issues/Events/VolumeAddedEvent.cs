using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Issues.Events
{
    public class VolumeAddedEvent : IEvent
    {
        public Volume Volume { get; private set; }
        public bool DoRefresh { get; private set; }

        public VolumeAddedEvent(Volume volume, bool doRefresh = true)
        {
            Volume = volume;
            DoRefresh = doRefresh;
        }
    }
}
