using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications
{
    public class NotificationDefinition : ProviderDefinition
    {
        public bool OnGrab { get; set; }
        public bool OnReleaseImport { get; set; }
        public bool OnUpgrade { get; set; }
        public bool OnRename { get; set; }
        public bool OnVolumeAdded { get; set; }
        public bool OnVolumeDelete { get; set; }
        public bool OnIssueDelete { get; set; }
        public bool OnIssueFileDelete { get; set; }
        public bool OnIssueFileDeleteForUpgrade { get; set; }
        public bool OnHealthIssue { get; set; }
        public bool OnDownloadFailure { get; set; }
        public bool OnImportFailure { get; set; }
        public bool OnIssueRetag { get; set; }
        public bool OnApplicationUpdate { get; set; }
        public bool SupportsOnGrab { get; set; }
        public bool SupportsOnReleaseImport { get; set; }
        public bool SupportsOnUpgrade { get; set; }
        public bool SupportsOnRename { get; set; }
        public bool SupportsOnVolumeAdded { get; set; }
        public bool SupportsOnVolumeDelete { get; set; }
        public bool SupportsOnIssueDelete { get; set; }
        public bool SupportsOnIssueFileDelete { get; set; }
        public bool SupportsOnIssueFileDeleteForUpgrade { get; set; }
        public bool SupportsOnHealthIssue { get; set; }
        public bool IncludeHealthWarnings { get; set; }
        public bool SupportsOnDownloadFailure { get; set; }
        public bool SupportsOnImportFailure { get; set; }
        public bool SupportsOnIssueRetag { get; set; }
        public bool SupportsOnApplicationUpdate { get; set; }

        public override bool Enable => OnGrab || OnReleaseImport || (OnReleaseImport && OnUpgrade) || OnRename || OnVolumeAdded || OnVolumeDelete || OnIssueDelete || OnIssueFileDelete || OnIssueFileDeleteForUpgrade || OnHealthIssue || OnDownloadFailure || OnImportFailure || OnIssueRetag || OnApplicationUpdate;
    }
}
