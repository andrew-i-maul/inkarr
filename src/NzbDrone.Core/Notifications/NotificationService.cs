using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.HealthCheck;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Update.History.Events;

namespace NzbDrone.Core.Notifications
{
    public class NotificationService
        : IHandle<IssueGrabbedEvent>,
          IHandle<IssueImportedEvent>,
          IHandle<VolumeRenamedEvent>,
          IHandle<VolumeAddedEvent>,
          IHandle<VolumeDeletedEvent>,
          IHandle<IssueDeletedEvent>,
          IHandle<IssueFileDeletedEvent>,
          IHandle<HealthCheckFailedEvent>,
          IHandle<DownloadFailedEvent>,
          IHandle<IssueImportIncompleteEvent>,
          IHandle<IssueFileRetaggedEvent>,
          IHandleAsync<DeleteCompletedEvent>,
          IHandle<UpdateInstalledEvent>
    {
        private readonly INotificationFactory _notificationFactory;
        private readonly INotificationStatusService _notificationStatusService;
        private readonly Logger _logger;

        public NotificationService(INotificationFactory notificationFactory, INotificationStatusService notificationStatusService, Logger logger)
        {
            _notificationFactory = notificationFactory;
            _notificationStatusService = notificationStatusService;
            _logger = logger;
        }

        private string GetMessage(Volume volume, List<Issue> issues, QualityModel quality)
        {
            var qualityString = quality.Quality.ToString();

            if (quality.Revision.Version > 1)
            {
                qualityString += " Proper";
            }

            var issueTitles = string.Join(" + ", issues.Select(e => e.Title));

            return string.Format("{0} - {1} - [{2}]",
                                    volume.Name,
                                    issueTitles,
                                    qualityString);
        }

        private string GetIssueDownloadMessage(Volume volume, Issue issue, List<IssueFile> tracks)
        {
            return string.Format("{0} - {1} ({2} Files Imported)",
                volume.Name,
                issue.Title,
                tracks.Count);
        }

        private string GetIssueIncompleteImportMessage(string source)
        {
            return string.Format("Inkarr failed to Import all files for {0}",
                source);
        }

        private string FormatMissing(object value)
        {
            var text = value?.ToString();
            return text.IsNullOrWhiteSpace() ? "<missing>" : text;
        }

        private string GetTrackRetagMessage(Volume volume, IssueFile issueFile, Dictionary<string, Tuple<string, string>> diff)
        {
            return string.Format("{0}:\n{1}",
                                 issueFile.Path,
                                 string.Join("\n", diff.Select(x => $"{x.Key}: {FormatMissing(x.Value.Item1)} → {FormatMissing(x.Value.Item2)}")));
        }

        private bool ShouldHandleVolume(ProviderDefinition definition, Volume volume)
        {
            if (definition.Tags.Empty())
            {
                _logger.Debug("No tags set for this notification.");
                return true;
            }

            if (definition.Tags.Intersect(volume.Tags).Any())
            {
                _logger.Debug("Notification and volume have one or more intersecting tags.");
                return true;
            }

            //TODO: this message could be more clear
            _logger.Debug("{0} does not have any intersecting tags with {1}. Notification will not be sent.", definition.Name, volume.Name);
            return false;
        }

        private bool ShouldHandleHealthFailure(HealthCheck.HealthCheck healthCheck, bool includeWarnings)
        {
            if (healthCheck.Type == HealthCheckResult.Error)
            {
                return true;
            }

            if (healthCheck.Type == HealthCheckResult.Warning && includeWarnings)
            {
                return true;
            }

            return false;
        }

        public void Handle(IssueGrabbedEvent message)
        {
            var grabMessage = new GrabMessage
            {
                Message = GetMessage(message.Issue.Volume, message.Issue.Issues, message.Issue.ParsedIssueInfo.Quality),
                Volume = message.Issue.Volume,
                Quality = message.Issue.ParsedIssueInfo.Quality,
                RemoteIssue = message.Issue,
                DownloadClientName = message.DownloadClientName,
                DownloadClientType = message.DownloadClient,
                DownloadId = message.DownloadId
            };

            foreach (var notification in _notificationFactory.OnGrabEnabled())
            {
                try
                {
                    if (!ShouldHandleVolume(notification.Definition, message.Issue.Volume))
                    {
                        continue;
                    }

                    notification.OnGrab(grabMessage);
                    _notificationStatusService.RecordSuccess(notification.Definition.Id);
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Error(ex, "Unable to send OnGrab notification to {0}", notification.Definition.Name);
                }
            }
        }

        public void Handle(IssueImportedEvent message)
        {
            if (!message.NewDownload)
            {
                return;
            }

            var downloadMessage = new IssueDownloadMessage
            {
                Message = GetIssueDownloadMessage(message.Volume, message.Issue, message.ImportedIssues),
                Volume = message.Volume,
                Issue = message.Issue,
                DownloadClientInfo = message.DownloadClientInfo,
                DownloadId = message.DownloadId,
                IssueFiles = message.ImportedIssues,
                OldFiles = message.OldFiles,
            };

            foreach (var notification in _notificationFactory.OnReleaseImportEnabled())
            {
                try
                {
                    if (ShouldHandleVolume(notification.Definition, message.Volume))
                    {
                        if (downloadMessage.OldFiles.Empty() || ((NotificationDefinition)notification.Definition).OnUpgrade)
                        {
                            notification.OnReleaseImport(downloadMessage);
                            _notificationStatusService.RecordSuccess(notification.Definition.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnReleaseImport notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(VolumeRenamedEvent message)
        {
            foreach (var notification in _notificationFactory.OnRenameEnabled())
            {
                try
                {
                    if (ShouldHandleVolume(notification.Definition, message.Volume))
                    {
                        notification.OnRename(message.Volume, message.RenamedFiles);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnRename notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(VolumeAddedEvent message)
        {
            foreach (var notification in _notificationFactory.OnVolumeAddedEnabled())
            {
                try
                {
                    if (ShouldHandleVolume(notification.Definition, message.Volume))
                    {
                        notification.OnVolumeAdded(message.Volume);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnVolumeAdded notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(VolumeDeletedEvent message)
        {
            var deleteMessage = new VolumeDeleteMessage(message.Volume, message.DeleteFiles);

            foreach (var notification in _notificationFactory.OnVolumeDeleteEnabled())
            {
                try
                {
                    if (ShouldHandleVolume(notification.Definition, deleteMessage.Volume))
                    {
                        notification.OnVolumeDelete(deleteMessage);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnVolumeDelete notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(IssueDeletedEvent message)
        {
            var deleteMessage = new IssueDeleteMessage(message.Issue, message.DeleteFiles);

            foreach (var notification in _notificationFactory.OnIssueDeleteEnabled())
            {
                try
                {
                    if (ShouldHandleVolume(notification.Definition, deleteMessage.Issue.Volume))
                    {
                        notification.OnIssueDelete(deleteMessage);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnIssueDelete notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(IssueFileDeletedEvent message)
        {
            var deleteMessage = new IssueFileDeleteMessage();

            var issue = new List<Issue> { message.IssueFile.Edition.Value.Issue };

            deleteMessage.Message = GetMessage(message.IssueFile.Volume, issue, message.IssueFile.Quality);
            deleteMessage.IssueFile = message.IssueFile;
            deleteMessage.Issue = message.IssueFile.Edition.Value.Issue;
            deleteMessage.Reason = message.Reason;

            foreach (var notification in _notificationFactory.OnIssueFileDeleteEnabled())
            {
                try
                {
                    if (message.Reason != MediaFiles.DeleteMediaFileReason.Upgrade || ((NotificationDefinition)notification.Definition).OnIssueFileDeleteForUpgrade)
                    {
                        if (ShouldHandleVolume(notification.Definition, message.IssueFile.Volume))
                        {
                            notification.OnIssueFileDelete(deleteMessage);
                            _notificationStatusService.RecordSuccess(notification.Definition.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnIssueFileDelete notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(HealthCheckFailedEvent message)
        {
            // Don't send health check notifications during the start up grace period,
            // once that duration expires they they'll be retested and fired off if necessary.
            if (message.IsInStartupGracePeriod)
            {
                return;
            }

            foreach (var notification in _notificationFactory.OnHealthIssueEnabled())
            {
                try
                {
                    if (ShouldHandleHealthFailure(message.HealthCheck, ((NotificationDefinition)notification.Definition).IncludeHealthWarnings))
                    {
                        notification.OnHealthIssue(message.HealthCheck);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnHealthIssue notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(DownloadFailedEvent message)
        {
            var downloadFailedMessage = new DownloadFailedMessage
            {
                DownloadId = message.DownloadId,
                DownloadClient = message.DownloadClient,
                Quality = message.Quality,
                SourceTitle = message.SourceTitle,
                Message = message.Message
            };

            foreach (var notification in _notificationFactory.OnDownloadFailureEnabled())
            {
                try
                {
                    if (ShouldHandleVolume(notification.Definition, message.TrackedDownload.RemoteIssue.Volume))
                    {
                        notification.OnDownloadFailure(downloadFailedMessage);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnDownloadFailure notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(IssueImportIncompleteEvent message)
        {
            // TODO: Build out this message so that we can pass on what failed and what was successful
            var downloadMessage = new IssueDownloadMessage
            {
                Message = GetIssueIncompleteImportMessage(message.TrackedDownload.DownloadItem.Title)
            };

            foreach (var notification in _notificationFactory.OnImportFailureEnabled())
            {
                try
                {
                    if (ShouldHandleVolume(notification.Definition, message.TrackedDownload.RemoteIssue.Volume))
                    {
                        notification.OnImportFailure(downloadMessage);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnImportFailure notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(IssueFileRetaggedEvent message)
        {
            var retagMessage = new IssueRetagMessage
            {
                Message = GetTrackRetagMessage(message.Volume, message.IssueFile, message.Diff),
                Volume = message.Volume,
                Issue = message.IssueFile.Edition.Value.Issue.Value,
                IssueFile = message.IssueFile,
                Diff = message.Diff,
                Scrubbed = message.Scrubbed
            };

            foreach (var notification in _notificationFactory.OnIssueRetagEnabled())
            {
                try
                {
                    if (ShouldHandleVolume(notification.Definition, message.Volume))
                    {
                        notification.OnIssueRetag(retagMessage);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnIssueRetag notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(UpdateInstalledEvent message)
        {
            var updateMessage = new ApplicationUpdateMessage();
            updateMessage.Message = $"Inkarr updated from {message.PreviousVerison.ToString()} to {message.NewVersion.ToString()}";
            updateMessage.PreviousVersion = message.PreviousVerison;
            updateMessage.NewVersion = message.NewVersion;

            foreach (var notification in _notificationFactory.OnApplicationUpdateEnabled())
            {
                try
                {
                    notification.OnApplicationUpdate(updateMessage);
                    _notificationStatusService.RecordSuccess(notification.Definition.Id);
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnApplicationUpdate notification to: " + notification.Definition.Name);
                }
            }
        }

        public void HandleAsync(DeleteCompletedEvent message)
        {
            ProcessQueue();
        }

        private void ProcessQueue()
        {
            foreach (var notification in _notificationFactory.GetAvailableProviders())
            {
                try
                {
                    notification.ProcessQueue();
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to process notification queue for " + notification.Definition.Name);
                }
            }
        }
    }
}
