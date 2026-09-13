namespace NzbDrone.Core.MediaFiles
{
    public class RenamedIssueFile
    {
        public IssueFile IssueFile { get; set; }
        public string PreviousPath { get; set; }
    }
}
