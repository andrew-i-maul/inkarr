using System.Collections.Generic;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications
{
    public interface INotification : IProvider
    {
        string Link { get; }

        void OnGrab(GrabMessage grabMessage);
        void OnReleaseImport(IssueDownloadMessage message);
        void OnRename(Volume volume, List<RenamedIssueFile> renamedFiles);
        void OnVolumeAdded(Volume volume);
        void OnVolumeDelete(VolumeDeleteMessage deleteMessage);
        void OnIssueDelete(IssueDeleteMessage deleteMessage);
        void OnIssueFileDelete(IssueFileDeleteMessage deleteMessage);
        void OnHealthIssue(HealthCheck.HealthCheck healthCheck);
        void OnApplicationUpdate(ApplicationUpdateMessage updateMessage);
        void OnDownloadFailure(DownloadFailedMessage message);
        void OnImportFailure(IssueDownloadMessage message);
        void OnIssueRetag(IssueRetagMessage message);
        void ProcessQueue();
        bool SupportsOnGrab { get; }
        bool SupportsOnReleaseImport { get; }
        bool SupportsOnUpgrade { get; }
        bool SupportsOnRename { get; }
        bool SupportsOnVolumeAdded { get; }
        bool SupportsOnVolumeDelete { get; }
        bool SupportsOnIssueDelete { get; }
        bool SupportsOnIssueFileDelete { get; }
        bool SupportsOnIssueFileDeleteForUpgrade { get; }
        bool SupportsOnHealthIssue { get; }
        bool SupportsOnApplicationUpdate { get; }
        bool SupportsOnDownloadFailure { get; }
        bool SupportsOnImportFailure { get; }
        bool SupportsOnIssueRetag { get; }
    }
}
