using NzbDrone.Common.Messaging;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class IssueFileRenamedEvent : IEvent
    {
        public Volume Volume { get; private set; }
        public IssueFile IssueFile { get; private set; }
        public string OriginalPath { get; private set; }

        public IssueFileRenamedEvent(Volume volume, IssueFile issueFile, string originalPath)
        {
            Volume = volume;
            IssueFile = issueFile;
            OriginalPath = originalPath;
        }
    }
}
