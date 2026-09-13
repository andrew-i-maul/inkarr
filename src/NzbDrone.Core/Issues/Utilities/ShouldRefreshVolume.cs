using System;
using System.Linq;
using NLog;

namespace NzbDrone.Core.Issues
{
    public interface ICheckIfVolumeShouldBeRefreshed
    {
        bool ShouldRefresh(Volume volume);
    }

    public class ShouldRefreshVolume : ICheckIfVolumeShouldBeRefreshed
    {
        private readonly IIssueService _issueService;
        private readonly Logger _logger;

        public ShouldRefreshVolume(IIssueService issueService, Logger logger)
        {
            _issueService = issueService;
            _logger = logger;
        }

        public bool ShouldRefresh(Volume volume)
        {
            if (volume.LastInfoSync < DateTime.UtcNow.AddDays(-30))
            {
                _logger.Trace("Volume {0} last updated more than 30 days ago, should refresh.", volume.Name);
                return true;
            }

            if (volume.LastInfoSync >= DateTime.UtcNow.AddHours(-12))
            {
                _logger.Trace("Volume {0} last updated less than 12 hours ago, should not be refreshed.", volume.Name);
                return false;
            }

            if (volume.Metadata.Value.Status == VolumeStatusType.Continuing && volume.LastInfoSync < DateTime.UtcNow.AddDays(-2))
            {
                _logger.Trace("Volume {0} is continuing and has not been refreshed in 2 days, should refresh.", volume.Name);
                return true;
            }

            var lastIssue = _issueService.GetIssuesByVolume(volume.Id).MaxBy(e => e.ReleaseDate);

            if (lastIssue != null && lastIssue.ReleaseDate > DateTime.UtcNow.AddDays(-30))
            {
                _logger.Trace("Last issue in {0} released less than 30 days ago, should refresh.", volume.Name);
                return true;
            }

            _logger.Trace("Volume {0} ended long ago, should not be refreshed.", volume.Name);
            return false;
        }
    }
}
