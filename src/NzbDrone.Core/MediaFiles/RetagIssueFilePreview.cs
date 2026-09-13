using System;
using System.Collections.Generic;

namespace NzbDrone.Core.MediaFiles
{
    public class RetagIssueFilePreview
    {
        public int VolumeId { get; set; }
        public int IssueId { get; set; }
        public List<int> TrackNumbers { get; set; } = new List<int>();
        public int IssueFileId { get; set; }
        public string Path { get; set; }
        public Dictionary<string, Tuple<string, string>> Changes { get; set; }
    }
}
