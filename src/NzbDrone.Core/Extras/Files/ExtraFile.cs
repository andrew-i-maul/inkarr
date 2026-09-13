using System;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Extras.Files
{
    public abstract class ExtraFile : ModelBase
    {
        public int VolumeId { get; set; }
        public int? IssueFileId { get; set; }
        public int? IssueId { get; set; }
        public string RelativePath { get; set; }
        public DateTime Added { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Extension { get; set; }

        public override string ToString()
        {
            return $"[{Id}] {RelativePath}";
        }
    }
}
