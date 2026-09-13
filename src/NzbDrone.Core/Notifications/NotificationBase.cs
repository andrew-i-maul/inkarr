using System;
using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications
{
    public abstract class NotificationBase<TSettings> : INotification
        where TSettings : IProviderConfig, new()
    {
        protected const string ISSUE_GRABBED_TITLE = "Issue Grabbed";
        protected const string ISSUE_DOWNLOADED_TITLE = "Issue Downloaded";
        protected const string VOLUME_ADDED_TITLE = "Volume Added";
        protected const string VOLUME_DELETED_TITLE = "Volume Deleted";
        protected const string ISSUE_DELETED_TITLE = "Issue Deleted";
        protected const string ISSUE_FILE_DELETED_TITLE = "Issue File Deleted";
        protected const string HEALTH_ISSUE_TITLE = "Health Check Failure";
        protected const string DOWNLOAD_FAILURE_TITLE = "Download Failed";
        protected const string IMPORT_FAILURE_TITLE = "Import Failed";
        protected const string ISSUE_RETAGGED_TITLE = "Issue File Tags Updated";
        protected const string APPLICATION_UPDATE_TITLE = "Application Updated";

        protected const string ISSUE_GRABBED_TITLE_BRANDED = "Inkarr - " + ISSUE_GRABBED_TITLE;
        protected const string ISSUE_DOWNLOADED_TITLE_BRANDED = "Inkarr - " + ISSUE_DOWNLOADED_TITLE;
        protected const string VOLUME_ADDED_TITLE_BRANDED = "Inkarr - " + VOLUME_ADDED_TITLE;
        protected const string VOLUME_DELETED_TITlE_BRANDED = "Inkarr - " + VOLUME_DELETED_TITLE;
        protected const string ISSUE_DELETED_TITLE_BRANDED = "Inkarr - " + ISSUE_DELETED_TITLE;
        protected const string ISSUE_FILE_DELETED_TITLE_BRANDED = "Inkarr - " + ISSUE_FILE_DELETED_TITLE;
        protected const string HEALTH_ISSUE_TITLE_BRANDED = "Inkarr - " + HEALTH_ISSUE_TITLE;
        protected const string DOWNLOAD_FAILURE_TITLE_BRANDED = "Inkarr - " + DOWNLOAD_FAILURE_TITLE;
        protected const string IMPORT_FAILURE_TITLE_BRANDED = "Inkarr - " + IMPORT_FAILURE_TITLE;
        protected const string ISSUE_RETAGGED_TITLE_BRANDED = "Inkarr - " + ISSUE_RETAGGED_TITLE;
        protected const string APPLICATION_UPDATE_TITLE_BRANDED = "Inkarr - " + APPLICATION_UPDATE_TITLE;

        public abstract string Name { get; }

        public Type ConfigContract => typeof(TSettings);

        public virtual ProviderMessage Message => null;

        public IEnumerable<ProviderDefinition> DefaultDefinitions => new List<ProviderDefinition>();

        public ProviderDefinition Definition { get; set; }
        public abstract ValidationResult Test();

        public abstract string Link { get; }

        public virtual void OnGrab(GrabMessage grabMessage)
        {
        }

        public virtual void OnReleaseImport(IssueDownloadMessage message)
        {
        }

        public virtual void OnRename(Volume volume, List<RenamedIssueFile> renamedFiles)
        {
        }

        public virtual void OnVolumeAdded(Volume volume)
        {
        }

        public virtual void OnVolumeDelete(VolumeDeleteMessage deleteMessage)
        {
        }

        public virtual void OnIssueDelete(IssueDeleteMessage deleteMessage)
        {
        }

        public virtual void OnIssueFileDelete(IssueFileDeleteMessage deleteMessage)
        {
        }

        public virtual void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
        }

        public virtual void OnDownloadFailure(DownloadFailedMessage message)
        {
        }

        public virtual void OnImportFailure(IssueDownloadMessage message)
        {
        }

        public virtual void OnIssueRetag(IssueRetagMessage message)
        {
        }

        public virtual void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
        }

        public virtual void ProcessQueue()
        {
        }

        public bool SupportsOnGrab => HasConcreteImplementation("OnGrab");
        public bool SupportsOnRename => HasConcreteImplementation("OnRename");
        public bool SupportsOnVolumeAdded => HasConcreteImplementation("OnVolumeAdded");
        public bool SupportsOnVolumeDelete => HasConcreteImplementation("OnVolumeDelete");
        public bool SupportsOnIssueDelete => HasConcreteImplementation("OnIssueDelete");
        public bool SupportsOnIssueFileDelete => HasConcreteImplementation("OnIssueFileDelete");
        public bool SupportsOnIssueFileDeleteForUpgrade => SupportsOnIssueFileDelete;
        public bool SupportsOnReleaseImport => HasConcreteImplementation("OnReleaseImport");
        public bool SupportsOnUpgrade => SupportsOnReleaseImport;
        public bool SupportsOnHealthIssue => HasConcreteImplementation("OnHealthIssue");
        public bool SupportsOnDownloadFailure => HasConcreteImplementation("OnDownloadFailure");
        public bool SupportsOnImportFailure => HasConcreteImplementation("OnImportFailure");
        public bool SupportsOnIssueRetag => HasConcreteImplementation("OnIssueRetag");
        public bool SupportsOnApplicationUpdate => HasConcreteImplementation("OnApplicationUpdate");

        protected TSettings Settings => (TSettings)Definition.Settings;

        public override string ToString()
        {
            return GetType().Name;
        }

        public virtual object RequestAction(string action, IDictionary<string, string> query)
        {
            return null;
        }

        private bool HasConcreteImplementation(string methodName)
        {
            var method = GetType().GetMethod(methodName);

            if (method == null)
            {
                throw new MissingMethodException(GetType().Name, Name);
            }

            return !method.DeclaringType.IsAbstract;
        }
    }
}
