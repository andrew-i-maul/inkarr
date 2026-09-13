using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications.Webhook
{
    public abstract class WebhookBase<TSettings> : NotificationBase<TSettings>
        where TSettings : IProviderConfig, new()
    {
        private readonly IConfigFileProvider _configFileProvider;

        protected WebhookBase(IConfigFileProvider configFileProvider)
            : base()
        {
            _configFileProvider = configFileProvider;
        }

        public WebhookGrabPayload BuildOnGrabPayload(GrabMessage message)
        {
            var remoteIssue = message.RemoteIssue;
            var quality = message.Quality;

            return new WebhookGrabPayload
            {
                EventType = WebhookEventType.Grab,
                InstanceName = _configFileProvider.InstanceName,
                Volume = new WebhookVolume(message.Volume),
                Issues = remoteIssue.Issues.ConvertAll(x => new WebhookIssue(x)),
                Release = new WebhookRelease(quality, remoteIssue),
                DownloadClient = message.DownloadClientName,
                DownloadClientType = message.DownloadClientType,
                DownloadId = message.DownloadId
            };
        }

        public WebhookImportPayload BuildOnReleaseImportPayload(IssueDownloadMessage message)
        {
            var trackFiles = message.IssueFiles;

            var payload = new WebhookImportPayload
            {
                EventType = WebhookEventType.Download,
                InstanceName = _configFileProvider.InstanceName,
                Volume = new WebhookVolume(message.Volume),
                Issue = new WebhookIssue(message.Issue),
                IssueFiles = trackFiles.ConvertAll(x => new WebhookIssueFile(x)),
                IsUpgrade = message.OldFiles.Any(),
                DownloadClient = message.DownloadClientInfo?.Name,
                DownloadClientType = message.DownloadClientInfo?.Type,
                DownloadId = message.DownloadId
            };

            if (message.OldFiles.Any())
            {
                payload.DeletedFiles = message.OldFiles.ConvertAll(x => new WebhookIssueFile(x));
            }

            return payload;
        }

        public WebhookRenamePayload BuildOnRenamePayload(Volume volume, List<RenamedIssueFile> renamedFiles)
        {
            return new WebhookRenamePayload
            {
                EventType = WebhookEventType.Rename,
                InstanceName = _configFileProvider.InstanceName,
                Volume = new WebhookVolume(volume),
                RenamedIssueFiles = renamedFiles.ConvertAll(x => new WebhookRenamedIssueFile(x))
            };
        }

        public WebhookRetagPayload BuildOnIssueRetagPayload(IssueRetagMessage message)
        {
            return new WebhookRetagPayload
            {
                EventType = WebhookEventType.Retag,
                InstanceName = _configFileProvider.InstanceName,
                Volume = new WebhookVolume(message.Volume),
                IssueFile = new WebhookIssueFile(message.IssueFile)
            };
        }

        public WebhookIssueDeletePayload BuildOnIssueDelete(IssueDeleteMessage deleteMessage)
        {
            return new WebhookIssueDeletePayload
            {
                EventType = WebhookEventType.IssueDelete,
                InstanceName = _configFileProvider.InstanceName,
                Volume = new WebhookVolume(deleteMessage.Issue.Volume),
                Issue = new WebhookIssue(deleteMessage.Issue),
                DeletedFiles = deleteMessage.DeletedFiles
            };
        }

        public WebhookIssueFileDeletePayload BuildOnIssueFileDelete(IssueFileDeleteMessage deleteMessage)
        {
            return new WebhookIssueFileDeletePayload
            {
                EventType = WebhookEventType.IssueFileDelete,
                InstanceName = _configFileProvider.InstanceName,
                Volume = new WebhookVolume(deleteMessage.Issue.Volume),
                Issue = new WebhookIssue(deleteMessage.Issue),
                IssueFile = new WebhookIssueFile(deleteMessage.IssueFile)
            };
        }

        public WebhookVolumeAddedPayload BuildOnVolumeAdded(Volume volume)
        {
            return new WebhookVolumeAddedPayload
            {
                EventType = WebhookEventType.VolumeAdded,
                InstanceName = _configFileProvider.InstanceName,
                Volume = new WebhookVolume(volume)
            };
        }

        public WebhookVolumeDeletePayload BuildOnVolumeDelete(VolumeDeleteMessage deleteMessage)
        {
            return new WebhookVolumeDeletePayload
            {
                EventType = WebhookEventType.VolumeDelete,
                InstanceName = _configFileProvider.InstanceName,
                Volume = new WebhookVolume(deleteMessage.Volume),
                DeletedFiles = deleteMessage.DeletedFiles
            };
        }

        protected WebhookHealthPayload BuildHealthPayload(HealthCheck.HealthCheck healthCheck)
        {
            return new WebhookHealthPayload
            {
                EventType = WebhookEventType.Health,
                InstanceName = _configFileProvider.InstanceName,
                Level = healthCheck.Type,
                Message = healthCheck.Message,
                Type = healthCheck.Source.Name,
                WikiUrl = healthCheck.WikiUrl?.ToString()
            };
        }

        protected WebhookApplicationUpdatePayload BuildApplicationUpdatePayload(ApplicationUpdateMessage updateMessage)
        {
            return new WebhookApplicationUpdatePayload
            {
                EventType = WebhookEventType.ApplicationUpdate,
                InstanceName = _configFileProvider.InstanceName,
                Message = updateMessage.Message,
                PreviousVersion = updateMessage.PreviousVersion.ToString(),
                NewVersion = updateMessage.NewVersion.ToString()
            };
        }

        protected WebhookPayload BuildTestPayload()
        {
            return new WebhookGrabPayload
            {
                EventType = WebhookEventType.Test,
                InstanceName = _configFileProvider.InstanceName,
                Volume = new WebhookVolume()
                {
                    Id = 1,
                    Name = "Test Name",
                    Path = "C:\\testpath",
                    GoodreadsId = "aaaaa-aaa-aaaa-aaaaaa"
                },
                Issues = new List<WebhookIssue>()
                    {
                            new WebhookIssue()
                            {
                                Id = 123,
                                Title = "Test title"
                            }
                    }
            };
        }
    }
}
