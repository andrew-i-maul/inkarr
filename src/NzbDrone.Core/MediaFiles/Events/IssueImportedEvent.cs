using System.Collections.Generic;
using NzbDrone.Common.Messaging;
using NzbDrone.Core.Download;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class IssueImportedEvent : IEvent
    {
        public Volume Volume { get; private set; }
        public Issue Issue { get; private set; }
        public List<IssueFile> ImportedIssues { get; private set; }
        public List<IssueFile> OldFiles { get; private set; }
        public bool NewDownload { get; private set; }
        public DownloadClientItemClientInfo DownloadClientInfo { get; set; }
        public string DownloadId { get; private set; }

        public IssueImportedEvent(Volume volume, Issue issue, List<IssueFile> importedIssues, List<IssueFile> oldFiles, bool newDownload, DownloadClientItem downloadClientItem)
        {
            Volume = volume;
            Issue = issue;
            ImportedIssues = importedIssues;
            OldFiles = oldFiles;
            NewDownload = newDownload;

            if (downloadClientItem != null)
            {
                DownloadClientInfo = downloadClientItem.DownloadClientInfo;
                DownloadId = downloadClientItem.DownloadId;
            }
        }
    }
}
