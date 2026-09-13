using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Download;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.History
{
    public interface IHistoryService
    {
        PagingSpec<EntityHistory> Paged(PagingSpec<EntityHistory> pagingSpec);
        EntityHistory MostRecentForIssue(int issueId);
        EntityHistory MostRecentForDownloadId(string downloadId);
        EntityHistory Get(int historyId);
        List<EntityHistory> GetByVolume(int volumeId, EntityHistoryEventType? eventType);
        List<EntityHistory> GetByIssue(int issueId, EntityHistoryEventType? eventType);
        List<EntityHistory> Find(string downloadId, EntityHistoryEventType eventType);
        List<EntityHistory> FindByDownloadId(string downloadId);
        string FindDownloadId(TrackImportedEvent trackedDownload);
        List<EntityHistory> Since(DateTime date, EntityHistoryEventType? eventType);
        void UpdateMany(IList<EntityHistory> items);
    }

    public class HistoryService : IHistoryService,
                                  IHandle<IssueGrabbedEvent>,
                                  IHandle<IssueImportIncompleteEvent>,
                                  IHandle<TrackImportedEvent>,
                                  IHandle<DownloadFailedEvent>,
                                  IHandle<IssueFileDeletedEvent>,
                                  IHandle<IssueFileRenamedEvent>,
                                  IHandle<IssueFileRetaggedEvent>,
                                  IHandle<VolumeDeletedEvent>,
                                  IHandle<DownloadIgnoredEvent>
    {
        private readonly IHistoryRepository _historyRepository;
        private readonly Logger _logger;

        public HistoryService(IHistoryRepository historyRepository, Logger logger)
        {
            _historyRepository = historyRepository;
            _logger = logger;
        }

        public PagingSpec<EntityHistory> Paged(PagingSpec<EntityHistory> pagingSpec)
        {
            return _historyRepository.GetPaged(pagingSpec);
        }

        public EntityHistory MostRecentForIssue(int issueId)
        {
            return _historyRepository.MostRecentForIssue(issueId);
        }

        public EntityHistory MostRecentForDownloadId(string downloadId)
        {
            return _historyRepository.MostRecentForDownloadId(downloadId);
        }

        public EntityHistory Get(int historyId)
        {
            return _historyRepository.Get(historyId);
        }

        public List<EntityHistory> GetByVolume(int volumeId, EntityHistoryEventType? eventType)
        {
            return _historyRepository.GetByVolume(volumeId, eventType);
        }

        public List<EntityHistory> GetByIssue(int issueId, EntityHistoryEventType? eventType)
        {
            return _historyRepository.GetByIssue(issueId, eventType);
        }

        public List<EntityHistory> Find(string downloadId, EntityHistoryEventType eventType)
        {
            return _historyRepository.FindByDownloadId(downloadId).Where(c => c.EventType == eventType).ToList();
        }

        public List<EntityHistory> FindByDownloadId(string downloadId)
        {
            return _historyRepository.FindByDownloadId(downloadId);
        }

        public string FindDownloadId(TrackImportedEvent trackedDownload)
        {
            _logger.Debug("Trying to find downloadId for {0} from history", trackedDownload.ImportedIssue.Path);

            var issueIds = new List<int> { trackedDownload.IssueInfo.Issue.Id };
            var allHistory = _historyRepository.FindDownloadHistory(trackedDownload.IssueInfo.Volume.Id, trackedDownload.ImportedIssue.Quality);

            //Find download related items for these episodes
            var issuesHistory = allHistory.Where(h => issueIds.Contains(h.IssueId)).ToList();

            var processedDownloadId = issuesHistory
                .Where(c => c.EventType != EntityHistoryEventType.Grabbed && c.DownloadId != null)
                .Select(c => c.DownloadId);

            var stillDownloading = issuesHistory.Where(c => c.EventType == EntityHistoryEventType.Grabbed && !processedDownloadId.Contains(c.DownloadId)).ToList();

            string downloadId = null;

            if (stillDownloading.Any())
            {
                var matchingHistory = stillDownloading.Where(c => c.IssueId == trackedDownload.IssueInfo.Issue.Id).ToList();

                if (matchingHistory.Count != 1)
                {
                    return null;
                }

                var newDownloadId = matchingHistory.Single().DownloadId;

                if (downloadId == null || downloadId == newDownloadId)
                {
                    downloadId = newDownloadId;
                }
                else
                {
                    return null;
                }
            }

            return downloadId;
        }

        public void Handle(IssueGrabbedEvent message)
        {
            foreach (var issue in message.Issue.Issues)
            {
                var history = new EntityHistory
                {
                    EventType = EntityHistoryEventType.Grabbed,
                    Date = DateTime.UtcNow,
                    Quality = message.Issue.ParsedIssueInfo.Quality,
                    SourceTitle = message.Issue.Release.Title,
                    VolumeId = issue.VolumeId,
                    IssueId = issue.Id,
                    DownloadId = message.DownloadId
                };

                history.Data.Add("Indexer", message.Issue.Release.Indexer);
                history.Data.Add("NzbInfoUrl", message.Issue.Release.InfoUrl);
                history.Data.Add("ReleaseGroup", message.Issue.ParsedIssueInfo.ReleaseGroup);
                history.Data.Add("Age", message.Issue.Release.Age.ToString());
                history.Data.Add("AgeHours", message.Issue.Release.AgeHours.ToString());
                history.Data.Add("AgeMinutes", message.Issue.Release.AgeMinutes.ToString());
                history.Data.Add("PublishedDate", message.Issue.Release.PublishDate.ToString("s") + "Z");
                history.Data.Add("DownloadClient", message.DownloadClient);
                history.Data.Add("DownloadClientName", message.DownloadClientName);
                history.Data.Add("Size", message.Issue.Release.Size.ToString());
                history.Data.Add("DownloadUrl", message.Issue.Release.DownloadUrl);
                history.Data.Add("Guid", message.Issue.Release.Guid);
                history.Data.Add("Protocol", ((int)message.Issue.Release.DownloadProtocol).ToString());
                history.Data.Add("DownloadForced", (!message.Issue.DownloadAllowed).ToString());
                history.Data.Add("CustomFormatScore", message.Issue.CustomFormatScore.ToString());
                history.Data.Add("ReleaseSource", message.Issue.ReleaseSource.ToString());
                history.Data.Add("IndexerFlags", message.Issue.Release.IndexerFlags.ToString());

                if (!message.Issue.ParsedIssueInfo.ReleaseHash.IsNullOrWhiteSpace())
                {
                    history.Data.Add("ReleaseHash", message.Issue.ParsedIssueInfo.ReleaseHash);
                }

                if (message.Issue.Release is TorrentInfo torrentRelease)
                {
                    history.Data.Add("TorrentInfoHash", torrentRelease.InfoHash);
                }

                _historyRepository.Insert(history);
            }
        }

        public void Handle(IssueImportIncompleteEvent message)
        {
            if (message.TrackedDownload.RemoteIssue == null)
            {
                return;
            }

            foreach (var issue in message.TrackedDownload.RemoteIssue.Issues)
            {
                var history = new EntityHistory
                {
                    EventType = EntityHistoryEventType.IssueImportIncomplete,
                    Date = DateTime.UtcNow,
                    Quality = message.TrackedDownload.RemoteIssue.ParsedIssueInfo?.Quality ?? new QualityModel(),
                    SourceTitle = message.TrackedDownload.DownloadItem.Title,
                    VolumeId = issue.VolumeId,
                    IssueId = issue.Id,
                    DownloadId = message.TrackedDownload.DownloadItem.DownloadId
                };

                history.Data.Add("StatusMessages", message.TrackedDownload.StatusMessages.ToJson());
                history.Data.Add("ReleaseGroup", message.TrackedDownload?.RemoteIssue?.ParsedIssueInfo?.ReleaseGroup);
                history.Data.Add("IndexerFlags", message.TrackedDownload?.RemoteIssue?.Release?.IndexerFlags.ToString());

                _historyRepository.Insert(history);
            }
        }

        public void Handle(TrackImportedEvent message)
        {
            if (!message.NewDownload)
            {
                return;
            }

            var downloadId = message.DownloadId;

            if (downloadId.IsNullOrWhiteSpace())
            {
                downloadId = FindDownloadId(message);
            }

            var history = new EntityHistory
            {
                EventType = EntityHistoryEventType.IssueFileImported,
                Date = DateTime.UtcNow,
                Quality = message.IssueInfo.Quality,
                SourceTitle = message.ImportedIssue.SceneName ?? Path.GetFileNameWithoutExtension(message.IssueInfo.Path),
                VolumeId = message.IssueInfo.Volume.Id,
                IssueId = message.IssueInfo.Issue.Id,
                DownloadId = downloadId
            };

            history.Data.Add("FileId", message.ImportedIssue.Id.ToString());
            history.Data.Add("DroppedPath", message.IssueInfo.Path);
            history.Data.Add("ImportedPath", message.ImportedIssue.Path);
            history.Data.Add("DownloadClient", message.DownloadClientInfo?.Type);
            history.Data.Add("DownloadClientName", message.DownloadClientInfo?.Name);
            history.Data.Add("ReleaseGroup", message.IssueInfo.ReleaseGroup);
            history.Data.Add("Size", message.IssueInfo.Size.ToString());
            history.Data.Add("IndexerFlags", message.IssueInfo.IndexerFlags.ToString());

            _historyRepository.Insert(history);
        }

        public void Handle(DownloadFailedEvent message)
        {
            foreach (var issueId in message.IssueIds)
            {
                var history = new EntityHistory
                {
                    EventType = EntityHistoryEventType.DownloadFailed,
                    Date = DateTime.UtcNow,
                    Quality = message.Quality,
                    SourceTitle = message.SourceTitle,
                    VolumeId = message.VolumeId,
                    IssueId = issueId,
                    DownloadId = message.DownloadId
                };

                history.Data.Add("DownloadClient", message.DownloadClient);
                history.Data.Add("DownloadClientName", message.TrackedDownload?.DownloadItem.DownloadClientInfo.Name);
                history.Data.Add("Message", message.Message);
                history.Data.Add("ReleaseGroup", message.TrackedDownload?.RemoteIssue?.ParsedIssueInfo?.ReleaseGroup ?? message.Data.GetValueOrDefault(EntityHistory.RELEASE_GROUP));
                history.Data.Add("Size", message.TrackedDownload?.DownloadItem.TotalSize.ToString() ?? message.Data.GetValueOrDefault(EntityHistory.SIZE));
                history.Data.Add("Indexer", message.TrackedDownload?.RemoteIssue?.Release?.Indexer ?? message.Data.GetValueOrDefault(EntityHistory.INDEXER));

                _historyRepository.Insert(history);
            }
        }

        public void Handle(IssueFileDeletedEvent message)
        {
            if (message.Reason == DeleteMediaFileReason.NoLinkedEpisodes)
            {
                _logger.Debug("Removing issue file from DB as part of cleanup routine, not creating history event.");
                return;
            }
            else if (message.Reason == DeleteMediaFileReason.ManualOverride)
            {
                _logger.Debug("Removing issue file from DB as part of manual override of existing file, not creating history event.");
                return;
            }

            var history = new EntityHistory
            {
                EventType = EntityHistoryEventType.IssueFileDeleted,
                Date = DateTime.UtcNow,
                Quality = message.IssueFile.Quality,
                SourceTitle = message.IssueFile.Path,
                VolumeId = message.IssueFile.Volume.Value.Id,
                IssueId = message.IssueFile.Edition.Value.IssueId
            };

            history.Data.Add("Reason", message.Reason.ToString());
            history.Data.Add("ReleaseGroup", message.IssueFile.ReleaseGroup);
            history.Data.Add("IndexerFlags", message.IssueFile.IndexerFlags.ToString());

            _historyRepository.Insert(history);
        }

        public void Handle(IssueFileRenamedEvent message)
        {
            var sourcePath = message.OriginalPath;
            var path = message.IssueFile.Path;

            var history = new EntityHistory
            {
                EventType = EntityHistoryEventType.IssueFileRenamed,
                Date = DateTime.UtcNow,
                Quality = message.IssueFile.Quality,
                SourceTitle = message.OriginalPath,
                VolumeId = message.IssueFile.Volume.Value.Id,
                IssueId = message.IssueFile.Edition.Value.IssueId
            };

            history.Data.Add("SourcePath", sourcePath);
            history.Data.Add("Path", path);
            history.Data.Add("ReleaseGroup", message.IssueFile.ReleaseGroup);
            history.Data.Add("Size", message.IssueFile.Size.ToString());
            history.Data.Add("IndexerFlags", message.IssueFile.IndexerFlags.ToString());

            _historyRepository.Insert(history);
        }

        public void Handle(IssueFileRetaggedEvent message)
        {
            var path = message.IssueFile.Path;

            var history = new EntityHistory
            {
                EventType = EntityHistoryEventType.IssueFileRetagged,
                Date = DateTime.UtcNow,
                Quality = message.IssueFile.Quality,
                SourceTitle = path,
                VolumeId = message.IssueFile.Volume.Value.Id,
                IssueId = message.IssueFile.Edition.Value.IssueId
            };

            history.Data.Add("TagsScrubbed", message.Scrubbed.ToString());
            history.Data.Add("Diff", message.Diff.Select(x => new
            {
                Field = x.Key,
                OldValue = x.Value.Item1,
                NewValue = x.Value.Item2
            }).ToJson());

            _historyRepository.Insert(history);
        }

        public void Handle(VolumeDeletedEvent message)
        {
            _historyRepository.DeleteForVolume(message.Volume.Id);
        }

        public void Handle(DownloadIgnoredEvent message)
        {
            var historyToAdd = new List<EntityHistory>();
            foreach (var issueId in message.IssueIds)
            {
                var history = new EntityHistory
                {
                    EventType = EntityHistoryEventType.DownloadIgnored,
                    Date = DateTime.UtcNow,
                    Quality = message.Quality,
                    SourceTitle = message.SourceTitle,
                    VolumeId = message.VolumeId,
                    IssueId = issueId,
                    DownloadId = message.DownloadId
                };

                history.Data.Add("DownloadClient", message.DownloadClientInfo.Name);
                history.Data.Add("Message", message.Message);
                history.Data.Add("ReleaseGroup", message.TrackedDownload?.RemoteIssue?.ParsedIssueInfo?.ReleaseGroup);
                history.Data.Add("Size", message.TrackedDownload?.DownloadItem.TotalSize.ToString());
                history.Data.Add("Indexer", message.TrackedDownload?.RemoteIssue?.Release?.Indexer);

                historyToAdd.Add(history);
            }

            _historyRepository.InsertMany(historyToAdd);
        }

        public List<EntityHistory> Since(DateTime date, EntityHistoryEventType? eventType)
        {
            return _historyRepository.Since(date, eventType);
        }

        public void UpdateMany(IList<EntityHistory> items)
        {
            _historyRepository.UpdateMany(items);
        }
    }
}
