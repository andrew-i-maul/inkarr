using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class IssueFileDeletedEvent : IEvent
    {
        public IssueFile IssueFile { get; private set; }
        public DeleteMediaFileReason Reason { get; private set; }

        public IssueFileDeletedEvent(IssueFile issueFile, DeleteMediaFileReason reason)
        {
            IssueFile = issueFile;
            Reason = reason;
        }
    }
}
