using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Notifications.Synology
{
    public class SynologyIndexer : NotificationBase<SynologyIndexerSettings>
    {
        private readonly ISynologyIndexerProxy _indexerProxy;

        public SynologyIndexer(ISynologyIndexerProxy indexerProxy)
        {
            _indexerProxy = indexerProxy;
        }

        public override string Link => "https://www.synology.com";
        public override string Name => "Synology Indexer";

        public override void OnReleaseImport(IssueDownloadMessage message)
        {
            if (Settings.UpdateLibrary)
            {
                foreach (var oldFile in message.OldFiles)
                {
                    var fullPath = oldFile.Path;

                    _indexerProxy.DeleteFile(fullPath);
                }

                foreach (var newFile in message.IssueFiles)
                {
                    var fullPath = newFile.Path;

                    _indexerProxy.AddFile(fullPath);
                }
            }
        }

        public override void OnRename(Volume volume, List<RenamedIssueFile> renamedFiles)
        {
            if (Settings.UpdateLibrary)
            {
                _indexerProxy.UpdateFolder(volume.Path);
            }
        }

        public override void OnVolumeDelete(VolumeDeleteMessage deleteMessage)
        {
            if (Settings.UpdateLibrary)
            {
                _indexerProxy.DeleteFolder(deleteMessage.Volume.Path);
            }
        }

        public override void OnIssueDelete(IssueDeleteMessage deleteMessage)
        {
            if (Settings.UpdateLibrary && deleteMessage.DeletedFiles)
            {
                foreach (var issueFile in deleteMessage.Issue.IssueFiles.Value)
                {
                    _indexerProxy.DeleteFile(issueFile.Path);
                }
            }
        }

        public override void OnIssueFileDelete(IssueFileDeleteMessage deleteMessage)
        {
            if (Settings.UpdateLibrary)
            {
                _indexerProxy.DeleteFile(deleteMessage.IssueFile.Path);
            }
        }

        public override void OnIssueRetag(IssueRetagMessage message)
        {
            if (Settings.UpdateLibrary)
            {
                _indexerProxy.UpdateFolder(message.Volume.Path);
            }
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(TestConnection());

            return new ValidationResult(failures);
        }

        protected virtual ValidationFailure TestConnection()
        {
            if (!OsInfo.IsLinux)
            {
                return new ValidationFailure(null, "Must be a Synology");
            }

            if (!_indexerProxy.Test())
            {
                return new ValidationFailure(null, "Not a Synology or synoindex not available");
            }

            return null;
        }
    }
}
