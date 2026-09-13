using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Notifications
{
    public class IssueFileDeleteMessage
    {
        public string Message { get; set; }
        public Issue Issue { get; set; }
        public IssueFile IssueFile { get; set; }

        public DeleteMediaFileReason Reason { get; set; }

        public override string ToString()
        {
            return Message;
        }
    }
}
