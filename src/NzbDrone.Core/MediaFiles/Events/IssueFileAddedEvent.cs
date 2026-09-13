using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class IssueFileAddedEvent : IEvent
    {
        public IssueFile IssueFile { get; private set; }

        public IssueFileAddedEvent(IssueFile issueFile)
        {
            IssueFile = issueFile;
        }
    }
}
