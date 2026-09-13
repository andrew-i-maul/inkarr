using System.Collections.Generic;

namespace NzbDrone.Core.MediaFiles
{
    public class IssueFileMoveResult
    {
        public IssueFileMoveResult()
        {
            OldFiles = new List<IssueFile>();
        }

        public IssueFile IssueFile { get; set; }
        public List<IssueFile> OldFiles { get; set; }
    }
}
