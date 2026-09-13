using NzbDrone.Common.Messaging;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class TrackFolderCreatedEvent : IEvent
    {
        public Volume Volume { get; private set; }
        public IssueFile IssueFile { get; private set; }
        public string VolumeFolder { get; set; }
        public string IssueFolder { get; set; }
        public string TrackFolder { get; set; }

        public TrackFolderCreatedEvent(Volume volume, IssueFile issueFile)
        {
            Volume = volume;
            IssueFile = issueFile;
        }
    }
}
