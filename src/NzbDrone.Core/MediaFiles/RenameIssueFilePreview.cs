using System.Collections.Generic;

namespace NzbDrone.Core.MediaFiles
{
    public class RenameIssueFilePreview
    {
        public int VolumeId { get; set; }
        public int IssueId { get; set; }
        public List<int> TrackNumbers { get; set; }
        public int IssueFileId { get; set; }
        public string ExistingPath { get; set; }
        public string NewPath { get; set; }
    }
}
