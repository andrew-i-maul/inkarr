using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Organizer
{
    public class SampleResult
    {
        public string FileName { get; set; }
        public Volume Volume { get; set; }
        public Issue Issue { get; set; }
        public IssueFile IssueFile { get; set; }
    }
}
