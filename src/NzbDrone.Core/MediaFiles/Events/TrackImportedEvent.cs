using System.Collections.Generic;
using NzbDrone.Common.Messaging;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class TrackImportedEvent : IEvent
    {
        public LocalIssue IssueInfo { get; private set; }
        public IssueFile ImportedIssue { get; private set; }
        public List<IssueFile> OldFiles { get; private set; }
        public bool NewDownload { get; private set; }
        public DownloadClientItemClientInfo DownloadClientInfo { get; set; }
        public string DownloadId { get; private set; }

        public TrackImportedEvent(LocalIssue issueInfo, IssueFile importedIssue, List<IssueFile> oldFiles, bool newDownload, DownloadClientItem downloadClientItem)
        {
            IssueInfo = issueInfo;
            ImportedIssue = importedIssue;
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
