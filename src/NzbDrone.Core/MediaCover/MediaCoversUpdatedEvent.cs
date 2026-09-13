using NzbDrone.Common.Messaging;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MediaCover
{
    public class MediaCoversUpdatedEvent : IEvent
    {
        public Volume Volume { get; set; }
        public Issue Issue { get; set; }

        public MediaCoversUpdatedEvent(Volume volume)
        {
            Volume = volume;
        }

        public MediaCoversUpdatedEvent(Issue issue)
        {
            Issue = issue;
        }
    }
}
