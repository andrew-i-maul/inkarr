using NzbDrone.Core.VolumeStats;

namespace Inkarr.Api.V1.Volume
{
    public class VolumeStatisticsResource
    {
        public int IssueFileCount { get; set; }
        public int IssueCount { get; set; }
        public int AvailableIssueCount { get; set; }
        public int TotalIssueCount { get; set; }
        public long SizeOnDisk { get; set; }

        public decimal PercentOfIssues
        {
            get
            {
                if (IssueCount == 0)
                {
                    return 0;
                }

                return AvailableIssueCount / (decimal)IssueCount * 100;
            }
        }
    }

    public static class VolumeStatisticsResourceMapper
    {
        public static VolumeStatisticsResource ToResource(this VolumeStatistics model)
        {
            if (model == null)
            {
                return null;
            }

            return new VolumeStatisticsResource
            {
                IssueFileCount = model.IssueFileCount,
                IssueCount = model.IssueCount,
                AvailableIssueCount = model.AvailableIssueCount,
                TotalIssueCount = model.TotalIssueCount,
                SizeOnDisk = model.SizeOnDisk
            };
        }
    }
}
