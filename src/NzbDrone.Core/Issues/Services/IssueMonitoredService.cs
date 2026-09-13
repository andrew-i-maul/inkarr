using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace NzbDrone.Core.Issues
{
    public interface IIssueMonitoredService
    {
        void SetIssueMonitoredStatus(Volume volume, MonitoringOptions monitoringOptions);
    }

    public class IssueMonitoredService : IIssueMonitoredService
    {
        private readonly IVolumeService _volumeService;
        private readonly IIssueService _issueService;
        private readonly Logger _logger;

        public IssueMonitoredService(IVolumeService volumeService, IIssueService issueService, Logger logger)
        {
            _volumeService = volumeService;
            _issueService = issueService;
            _logger = logger;
        }

        public void SetIssueMonitoredStatus(Volume volume, MonitoringOptions monitoringOptions)
        {
            if (monitoringOptions != null)
            {
                _logger.Debug("[{0}] Setting issue monitored status.", volume.Name);

                var issues = _issueService.GetIssuesByVolume(volume.Id);

                var issuesWithFiles = _issueService.GetVolumeIssuesWithFiles(volume);

                var issuesWithoutFiles = issues.Where(c => !issuesWithFiles.Select(e => e.Id).Contains(c.Id) && c.ReleaseDate <= DateTime.UtcNow).ToList();

                var monitoredIssues = monitoringOptions.IssuesToMonitor;

                // If specific issues are passed use those instead of the monitoring options.
                if (monitoredIssues.Any())
                {
                    ToggleIssuesMonitoredState(
                        issues.Where(s => monitoredIssues.Contains(s.ForeignIssueId)), true);
                    ToggleIssuesMonitoredState(
                        issues.Where(s => !monitoredIssues.Contains(s.ForeignIssueId)), false);
                }
                else
                {
                    switch (monitoringOptions.Monitor)
                    {
                        case MonitorTypes.All:
                            ToggleIssuesMonitoredState(issues, true);
                            break;
                        case MonitorTypes.Future:
                            _logger.Debug("Unmonitoring Issues with Files");
                            ToggleIssuesMonitoredState(issues.Where(e => issuesWithFiles.Select(c => c.Id).Contains(e.Id)), false);
                            _logger.Debug("Unmonitoring Issues without Files");
                            ToggleIssuesMonitoredState(issues.Where(e => issuesWithoutFiles.Select(c => c.Id).Contains(e.Id)), false);
                            break;
                        case MonitorTypes.None:
                            ToggleIssuesMonitoredState(issues, false);
                            break;
                        case MonitorTypes.Missing:
                            _logger.Debug("Unmonitoring Issues with Files");
                            ToggleIssuesMonitoredState(issues.Where(e => issuesWithFiles.Select(c => c.Id).Contains(e.Id)), false);
                            _logger.Debug("Monitoring Issues without Files");
                            ToggleIssuesMonitoredState(issues.Where(e => issuesWithoutFiles.Select(c => c.Id).Contains(e.Id)), true);
                            break;
                        case MonitorTypes.Existing:
                            _logger.Debug("Monitoring Issues with Files");
                            ToggleIssuesMonitoredState(issues.Where(e => issuesWithFiles.Select(c => c.Id).Contains(e.Id)), true);
                            _logger.Debug("Unmonitoring Issues without Files");
                            ToggleIssuesMonitoredState(issues.Where(e => issuesWithoutFiles.Select(c => c.Id).Contains(e.Id)), false);
                            break;
                        case MonitorTypes.Latest:
                            ToggleIssuesMonitoredState(issues, false);
                            ToggleIssuesMonitoredState(issues.OrderByDescending(e => e.ReleaseDate).Take(1), true);
                            break;
                        case MonitorTypes.First:
                            ToggleIssuesMonitoredState(issues, false);
                            ToggleIssuesMonitoredState(issues.OrderBy(e => e.ReleaseDate).Take(1), true);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                // Use individual update to ensure updates are sent to frontend
                foreach (var issue in issues)
                {
                    _issueService.UpdateIssue(issue);
                }
            }

            _volumeService.UpdateVolume(volume);
        }

        private void ToggleIssuesMonitoredState(IEnumerable<Issue> issues, bool monitored)
        {
            foreach (var issue in issues)
            {
                issue.Monitored = monitored;
            }
        }
    }
}
