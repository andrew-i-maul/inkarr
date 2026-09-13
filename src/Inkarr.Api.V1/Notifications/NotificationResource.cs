using NzbDrone.Core.Notifications;

namespace Inkarr.Api.V1.Notifications
{
    public class NotificationResource : ProviderResource<NotificationResource>
    {
        public string Link { get; set; }
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
        public string TestCommand { get; set; }
    }

    public class NotificationResourceMapper : ProviderResourceMapper<NotificationResource, NotificationDefinition>
    {
        public override NotificationResource ToResource(NotificationDefinition definition)
        {
            if (definition == null)
            {
                return default(NotificationResource);
            }

            var resource = base.ToResource(definition);

            resource.OnGrab = definition.OnGrab;
            resource.OnReleaseImport = definition.OnReleaseImport;
            resource.OnUpgrade = definition.OnUpgrade;
            resource.OnRename = definition.OnRename;
            resource.OnVolumeAdded = definition.OnVolumeAdded;
            resource.OnVolumeDelete = definition.OnVolumeDelete;
            resource.OnIssueDelete = definition.OnIssueDelete;
            resource.OnIssueFileDelete = definition.OnIssueFileDelete;
            resource.OnIssueFileDeleteForUpgrade = definition.OnIssueFileDeleteForUpgrade;
            resource.OnHealthIssue = definition.OnHealthIssue;
            resource.OnDownloadFailure = definition.OnDownloadFailure;
            resource.OnImportFailure = definition.OnImportFailure;
            resource.OnIssueRetag = definition.OnIssueRetag;
            resource.OnApplicationUpdate = definition.OnApplicationUpdate;
            resource.SupportsOnGrab = definition.SupportsOnGrab;
            resource.SupportsOnReleaseImport = definition.SupportsOnReleaseImport;
            resource.SupportsOnUpgrade = definition.SupportsOnUpgrade;
            resource.SupportsOnRename = definition.SupportsOnRename;
            resource.SupportsOnVolumeAdded = definition.SupportsOnVolumeAdded;
            resource.SupportsOnVolumeDelete = definition.SupportsOnVolumeDelete;
            resource.SupportsOnIssueDelete = definition.SupportsOnIssueDelete;
            resource.SupportsOnIssueFileDelete = definition.SupportsOnIssueFileDelete;
            resource.SupportsOnIssueFileDeleteForUpgrade = definition.SupportsOnIssueFileDeleteForUpgrade;
            resource.SupportsOnHealthIssue = definition.SupportsOnHealthIssue;
            resource.IncludeHealthWarnings = definition.IncludeHealthWarnings;
            resource.SupportsOnDownloadFailure = definition.SupportsOnDownloadFailure;
            resource.SupportsOnImportFailure = definition.SupportsOnImportFailure;
            resource.SupportsOnIssueRetag = definition.SupportsOnIssueRetag;
            resource.SupportsOnApplicationUpdate = definition.SupportsOnApplicationUpdate;

            return resource;
        }

        public override NotificationDefinition ToModel(NotificationResource resource)
        {
            if (resource == null)
            {
                return default(NotificationDefinition);
            }

            var definition = base.ToModel(resource);

            definition.OnGrab = resource.OnGrab;
            definition.OnReleaseImport = resource.OnReleaseImport;
            definition.OnUpgrade = resource.OnUpgrade;
            definition.OnRename = resource.OnRename;
            definition.OnVolumeAdded = resource.OnVolumeAdded;
            definition.OnVolumeDelete = resource.OnVolumeDelete;
            definition.OnIssueDelete = resource.OnIssueDelete;
            definition.OnIssueFileDelete = resource.OnIssueFileDelete;
            definition.OnIssueFileDeleteForUpgrade = resource.OnIssueFileDeleteForUpgrade;
            definition.OnHealthIssue = resource.OnHealthIssue;
            definition.OnDownloadFailure = resource.OnDownloadFailure;
            definition.OnImportFailure = resource.OnImportFailure;
            definition.OnIssueRetag = resource.OnIssueRetag;
            definition.OnApplicationUpdate = resource.OnApplicationUpdate;
            definition.SupportsOnGrab = resource.SupportsOnGrab;
            definition.SupportsOnReleaseImport = resource.SupportsOnReleaseImport;
            definition.SupportsOnUpgrade = resource.SupportsOnUpgrade;
            definition.SupportsOnRename = resource.SupportsOnRename;
            definition.SupportsOnVolumeAdded = resource.SupportsOnVolumeAdded;
            definition.SupportsOnVolumeDelete = resource.SupportsOnVolumeDelete;
            definition.SupportsOnIssueDelete = resource.SupportsOnIssueDelete;
            definition.SupportsOnIssueFileDelete = resource.SupportsOnIssueFileDelete;
            definition.SupportsOnIssueFileDeleteForUpgrade = resource.SupportsOnIssueFileDeleteForUpgrade;
            definition.SupportsOnHealthIssue = resource.SupportsOnHealthIssue;
            definition.IncludeHealthWarnings = resource.IncludeHealthWarnings;
            definition.SupportsOnDownloadFailure = resource.SupportsOnDownloadFailure;
            definition.SupportsOnImportFailure = resource.SupportsOnImportFailure;
            definition.SupportsOnIssueRetag = resource.SupportsOnIssueRetag;
            definition.SupportsOnApplicationUpdate = resource.SupportsOnApplicationUpdate;

            return definition;
        }
    }
}
