using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.VolumeStats
{
    public class IssueStatistics : ResultSet
    {
        public int VolumeId { get; set; }
        public int IssueId { get; set; }
        public int IssueFileCount { get; set; }
        public int IssueCount { get; set; }
        public int AvailableIssueCount { get; set; }
        public int TotalIssueCount { get; set; }
        public long SizeOnDisk { get; set; }
    }
}
