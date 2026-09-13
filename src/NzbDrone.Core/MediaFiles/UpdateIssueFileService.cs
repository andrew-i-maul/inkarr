using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.MediaFiles
{
    public interface IUpdateIssueFileService
    {
        void ChangeFileDateForFile(IssueFile issueFile, Volume volume, Issue issue);
    }

    public class UpdateIssueFileService : IUpdateIssueFileService,
                                            IHandle<VolumeScannedEvent>
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IIssueService _issueService;
        private readonly IConfigService _configService;
        private readonly Logger _logger;
        private static readonly DateTime EpochTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public UpdateIssueFileService(IDiskProvider diskProvider,
                                      IConfigService configService,
                                      IIssueService issueService,
                                      Logger logger)
        {
            _diskProvider = diskProvider;
            _configService = configService;
            _issueService = issueService;
            _logger = logger;
        }

        public void ChangeFileDateForFile(IssueFile issueFile, Volume volume, Issue issue)
        {
            ChangeFileDate(issueFile, issue);
        }

        private bool ChangeFileDate(IssueFile issueFile, Issue issue)
        {
            var issueFilePath = issueFile.Path;

            switch (_configService.FileDate)
            {
                case FileDateType.IssueReleaseDate:
                    {
                        if (!issue.ReleaseDate.HasValue)
                        {
                            _logger.Debug("Could not create valid date to change file [{0}]", issueFilePath);
                            return false;
                        }

                        var relDate = issue.ReleaseDate.Value;

                        // avoiding false +ve checks and set date skewing by not using UTC (Windows)
                        var oldDateTime = _diskProvider.FileGetLastWrite(issueFilePath);

                        if (OsInfo.IsNotWindows && relDate < EpochTime)
                        {
                            _logger.Debug("Setting date of file to 1970-01-01 as actual airdate is before that time and will not be set properly");
                            relDate = EpochTime;
                        }

                        if (!DateTime.Equals(relDate, oldDateTime))
                        {
                            try
                            {
                                _diskProvider.FileSetLastWriteTime(issueFilePath, relDate);
                                _logger.Debug("Date of file [{0}] changed from '{1}' to '{2}'", issueFilePath, oldDateTime, relDate);

                                return true;
                            }
                            catch (Exception ex)
                            {
                                _logger.Warn(ex, "Unable to set date of file [" + issueFilePath + "]");
                            }
                        }

                        return false;
                    }
            }

            return false;
        }

        public void Handle(VolumeScannedEvent message)
        {
            if (_configService.FileDate == FileDateType.None)
            {
                return;
            }

            var issues = _issueService.GetVolumeIssuesWithFiles(message.Volume);

            var issueFiles = new List<IssueFile>();
            var updated = new List<IssueFile>();

            foreach (var issue in issues)
            {
                var files = issue.IssueFiles.Value;
                foreach (var file in files)
                {
                    issueFiles.Add(file);
                    if (ChangeFileDate(file, issue))
                    {
                        updated.Add(file);
                    }
                }
            }

            if (updated.Any())
            {
                _logger.ProgressDebug("Changed file date for {0} files of {1} in {2}", updated.Count, issueFiles.Count, message.Volume.Name);
            }
            else
            {
                _logger.ProgressDebug("No file dates changed for {0}", message.Volume.Name);
            }
        }
    }
}
