using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NLog;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.History;
using NzbDrone.Core.Issues.Commands;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.Issues
{
    public interface IRefreshIssueService
    {
        bool RefreshIssueInfo(Issue issue, List<Issue> remoteIssues, Volume remoteData, bool forceUpdateFileTags);
        bool RefreshIssueInfo(List<Issue> issues, List<Issue> remoteIssues, Volume remoteData, bool forceIssueRefresh, bool forceUpdateFileTags, DateTime? lastUpdate);
    }

    public class RefreshIssueService : RefreshEntityServiceBase<Issue, Edition>,
        IRefreshIssueService,
        IExecute<RefreshIssueCommand>,
        IExecute<BulkRefreshIssueCommand>
    {
        private readonly IIssueService _issueService;
        private readonly IVolumeService _volumeService;
        private readonly IRootFolderService _rootFolderService;
        private readonly IAddVolumeService _addVolumeService;
        private readonly IEditionService _editionService;
        private readonly IProvideVolumeInfo _volumeInfo;
        private readonly IProvideIssueInfo _issueInfo;
        private readonly IRefreshEditionService _refreshEditionService;
        private readonly IMediaFileService _mediaFileService;
        private readonly IHistoryService _historyService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ICheckIfIssueShouldBeRefreshed _checkIfIssueShouldBeRefreshed;
        private readonly IMapCoversToLocal _mediaCoverService;
        private readonly Logger _logger;

        public RefreshIssueService(IIssueService issueService,
                                  IVolumeService volumeService,
                                  IRootFolderService rootFolderService,
                                  IAddVolumeService addVolumeService,
                                  IEditionService editionService,
                                  IVolumeMetadataService volumeMetadataService,
                                  IProvideVolumeInfo volumeInfo,
                                  IProvideIssueInfo issueInfo,
                                  IRefreshEditionService refreshEditionService,
                                  IMediaFileService mediaFileService,
                                  IHistoryService historyService,
                                  IEventAggregator eventAggregator,
                                  ICheckIfIssueShouldBeRefreshed checkIfIssueShouldBeRefreshed,
                                  IMapCoversToLocal mediaCoverService,
                                  Logger logger)
        : base(logger, volumeMetadataService)
        {
            _issueService = issueService;
            _volumeService = volumeService;
            _rootFolderService = rootFolderService;
            _addVolumeService = addVolumeService;
            _editionService = editionService;
            _volumeInfo = volumeInfo;
            _issueInfo = issueInfo;
            _refreshEditionService = refreshEditionService;
            _mediaFileService = mediaFileService;
            _historyService = historyService;
            _eventAggregator = eventAggregator;
            _checkIfIssueShouldBeRefreshed = checkIfIssueShouldBeRefreshed;
            _mediaCoverService = mediaCoverService;
            _logger = logger;
        }

        private Volume GetSkyhookData(Issue issue)
        {
            try
            {
                var tuple = _issueInfo.GetIssueInfo(issue.ForeignIssueId);
                var volume = _volumeInfo.GetVolumeInfo(tuple.Item1);
                var newissue = tuple.Item2;

                newissue.Volume = volume;
                newissue.VolumeMetadata = volume.Metadata.Value;
                newissue.VolumeMetadataId = issue.VolumeMetadataId;
                newissue.VolumeMetadata.Value.Id = issue.VolumeMetadataId;

                volume.Issues = new List<Issue> { newissue };
                return volume;
            }
            catch (IssueNotFoundException)
            {
                _logger.Error($"Could not find issue with id {issue.ForeignIssueId}");
            }

            return null;
        }

        protected override RemoteData GetRemoteData(Issue local, List<Issue> remote, Volume data)
        {
            var result = new RemoteData();

            var issue = remote.SingleOrDefault(x => x.ForeignIssueId == local.ForeignIssueId);

            if (issue == null && ShouldDelete(local))
            {
                return result;
            }

            if (issue == null)
            {
                data = GetSkyhookData(local);
                issue = data.Issues.Value.SingleOrDefault(x => x.ForeignIssueId == local.ForeignIssueId);
            }

            result.Entity = issue;
            if (result.Entity != null)
            {
                result.Entity.Id = local.Id;
            }

            return result;
        }

        protected override void EnsureNewParent(Issue local, Issue remote)
        {
            // Make sure the appropriate volume exists (it could be that an issue changes parent)
            // The volumeMetadata entry will be in the db but make sure a corresponding volume is too
            // so that the issue doesn't just disappear.

            // TODO filter by metadata id before hitting database
            _logger.Trace($"Ensuring parent volume exists [{remote.VolumeMetadata.Value.ForeignVolumeId}]");

            var newVolume = _volumeService.FindById(remote.VolumeMetadata.Value.ForeignVolumeId);

            if (newVolume == null)
            {
                var oldVolume = local.Volume.Value;
                var addVolume = new Volume
                {
                    Metadata = remote.VolumeMetadata.Value,
                    MetadataProfileId = oldVolume.MetadataProfileId,
                    QualityProfileId = oldVolume.QualityProfileId,
                    RootFolderPath = _rootFolderService.GetBestRootFolderPath(oldVolume.Path),
                    Monitored = oldVolume.Monitored,
                    Tags = oldVolume.Tags
                };
                _logger.Debug($"Adding missing parent volume {addVolume}");
                _addVolumeService.AddVolume(addVolume);
            }
        }

        protected override bool ShouldDelete(Issue local)
        {
            // not manually added and has no files
            return local.AddOptions.AddType != IssueAddType.Manual &&
                !_mediaFileService.GetFilesByIssue(local.Id).Any();
        }

        protected override void LogProgress(Issue local)
        {
            _logger.ProgressInfo("Updating Info for {0}", local.Title);
        }

        protected override bool IsMerge(Issue local, Issue remote)
        {
            return local.ForeignIssueId != remote.ForeignIssueId;
        }

        protected override UpdateResult UpdateEntity(Issue local, Issue remote)
        {
            UpdateResult result;

            remote.UseDbFieldsFrom(local);

            if (local.Title != (remote.Title ?? "Unknown") ||
                local.ForeignIssueId != remote.ForeignIssueId ||
                local.VolumeMetadata.Value.ForeignVolumeId != remote.VolumeMetadata.Value.ForeignVolumeId)
            {
                result = UpdateResult.UpdateTags;
            }
            else if (!local.Equals(remote))
            {
                result = UpdateResult.Standard;
            }
            else
            {
                result = UpdateResult.None;
            }

            // Force update and fetch covers if images have changed so that we can write them into tags
            // if (remote.Images.Any() && !local.Images.SequenceEqual(remote.Images))
            // {
            //     _mediaCoverService.EnsureIssueCovers(remote);
            //     result = UpdateResult.UpdateTags;
            // }
            local.UseMetadataFrom(remote);

            local.VolumeMetadataId = remote.VolumeMetadata.Value.Id;
            local.LastInfoSync = DateTime.UtcNow;

            return result;
        }

        protected override UpdateResult MergeEntity(Issue local, Issue target, Issue remote)
        {
            _logger.Warn($"Issue {local} was merged with {remote} because the original was a duplicate.");

            // Update issue ids for trackfiles
            var files = _mediaFileService.GetFilesByIssue(local.Id);
            files.ForEach(x => x.EditionId = target.Editions.Value.Single(e => e.Monitored).Id);
            _mediaFileService.Update(files);

            // Update issue ids for history
            var items = _historyService.GetByIssue(local.Id, null);
            items.ForEach(x => x.IssueId = target.Id);
            _historyService.UpdateMany(items);

            // Finally delete the old issue
            _issueService.DeleteMany(new List<Issue> { local });

            return UpdateResult.UpdateTags;
        }

        protected override Issue GetEntityByForeignId(Issue local)
        {
            return _issueService.FindById(local.ForeignIssueId);
        }

        protected override void SaveEntity(Issue local)
        {
            // Use UpdateMany to avoid firing the issue edited event
            _issueService.UpdateMany(new List<Issue> { local });
        }

        protected override void DeleteEntity(Issue local, bool deleteFiles)
        {
            _issueService.DeleteIssue(local.Id, deleteFiles);
        }

        protected override List<Edition> GetRemoteChildren(Issue local, Issue remote)
        {
            return remote.Editions.Value.DistinctBy(m => m.ForeignEditionId).ToList();
        }

        protected override List<Edition> GetLocalChildren(Issue entity, List<Edition> remoteChildren)
        {
            return _editionService.GetEditionsForRefresh(entity.Id, remoteChildren.Select(x => x.ForeignEditionId).ToList());
        }

        protected override Tuple<Edition, List<Edition>> GetMatchingExistingChildren(List<Edition> existingChildren, Edition remote)
        {
            var existingChild = existingChildren.SingleOrDefault(x => x.ForeignEditionId == remote.ForeignEditionId);
            return Tuple.Create(existingChild, new List<Edition>());
        }

        protected override void PrepareNewChild(Edition child, Issue entity)
        {
            child.IssueId = entity.Id;
            child.Issue = entity;
        }

        protected override void PrepareExistingChild(Edition local, Edition remote, Issue entity)
        {
            local.IssueId = entity.Id;
            local.Issue = entity;

            remote.UseDbFieldsFrom(local);
        }

        protected override void AddChildren(List<Edition> children)
        {
            // hack - add the chilren in refresh children so we can control monitored status
        }

        private void MonitorSingleEdition(SortedChildren children)
        {
            children.Old.ForEach(x => x.Monitored = false);
            var monitored = children.Future.Where(x => x.Monitored).ToList();

            if (monitored.Count == 1)
            {
                return;
            }

            if (monitored.Count == 0)
            {
                monitored = children.Future;
            }

            if (monitored.Count == 0)
            {
                // there are no future children so nothing to do
                return;
            }

            var toMonitor = monitored.OrderByDescending(x => x.Id > 0 ? _mediaFileService.GetFilesByEdition(x.Id).Count : 0)
                .ThenByDescending(x => x.Ratings.Popularity).First();

            monitored.ForEach(x => x.Monitored = false);
            toMonitor.Monitored = true;

            // force update of anything we've messed with
            var extraToUpdate = children.UpToDate.Where(x => monitored.Contains(x));
            children.UpToDate = children.UpToDate.Except(extraToUpdate).ToList();
            children.Updated.AddRange(extraToUpdate);

            Debug.Assert(!children.Future.Any() || children.Future.Count(x => x.Monitored) == 1, "one edition monitored");
        }

        protected override bool RefreshChildren(SortedChildren localChildren, List<Edition> remoteChildren, Volume remoteData, bool forceChildRefresh, bool forceUpdateFileTags, DateTime? lastUpdate)
        {
            // make sure only one of the releases ends up monitored
            MonitorSingleEdition(localChildren);

            localChildren.All.ForEach(x => _logger.Trace($"release: {x} monitored: {x.Monitored}"));

            _editionService.InsertMany(localChildren.Added);

            return _refreshEditionService.RefreshEditionInfo(localChildren.Added, localChildren.Updated, localChildren.Merged, localChildren.Deleted, localChildren.UpToDate, remoteChildren, forceUpdateFileTags);
        }

        protected override void PublishEntityUpdatedEvent(Issue entity)
        {
            // Fetch fresh from DB so all lazy loads are available
            _eventAggregator.PublishEvent(new IssueUpdatedEvent(_issueService.GetIssue(entity.Id)));
        }

        public bool RefreshIssueInfo(List<Issue> issues, List<Issue> remoteIssues, Volume remoteData, bool forceIssueRefresh, bool forceUpdateFileTags, DateTime? lastUpdate)
        {
            var updated = false;

            foreach (var issue in issues)
            {
                if (forceIssueRefresh || _checkIfIssueShouldBeRefreshed.ShouldRefresh(issue))
                {
                    updated |= RefreshIssueInfo(issue, remoteIssues, remoteData, forceUpdateFileTags);
                }
                else
                {
                    _logger.Debug("Skipping refresh of issue: {0}", issue.Title);
                }
            }

            return updated;
        }

        public bool RefreshIssueInfo(Issue issue, List<Issue> remoteIssues, Volume remoteData, bool forceUpdateFileTags)
        {
            return RefreshEntityInfo(issue, remoteIssues, remoteData, true, forceUpdateFileTags, null);
        }

        public bool RefreshIssueInfo(Issue issue)
        {
            var data = GetSkyhookData(issue);

            return RefreshIssueInfo(issue, data.Issues, data, false);
        }

        public void Execute(BulkRefreshIssueCommand message)
        {
            var issues = _issueService.GetIssues(message.IssueIds);

            foreach (var issue in issues)
            {
                RefreshIssueInfo(issue);
            }
        }

        public void Execute(RefreshIssueCommand message)
        {
            if (message.IssueId.HasValue)
            {
                var issue = _issueService.GetIssue(message.IssueId.Value);

                RefreshIssueInfo(issue);
            }
        }
    }
}
