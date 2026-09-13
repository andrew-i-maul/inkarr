using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.History;
using NzbDrone.Core.ImportLists.Exclusions;
using NzbDrone.Core.Issues.Commands;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Profiles.Metadata;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.Issues
{
    public interface IRefreshVolumeService
    {
    }

    public class RefreshVolumeService : RefreshEntityServiceBase<Volume, Issue>,
        IRefreshVolumeService,
        IExecute<RefreshVolumeCommand>,
        IExecute<BulkRefreshVolumeCommand>
    {
        private readonly IProvideVolumeInfo _volumeInfo;
        private readonly IVolumeService _volumeService;
        private readonly IIssueService _issueService;
        private readonly IMetadataProfileService _metadataProfileService;
        private readonly IRefreshIssueService _refreshIssueService;
        private readonly IRefreshSeriesService _refreshSeriesService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly IMediaFileService _mediaFileService;
        private readonly IHistoryService _historyService;
        private readonly IRootFolderService _rootFolderService;
        private readonly ICheckIfVolumeShouldBeRefreshed _checkIfVolumeShouldBeRefreshed;
        private readonly IMonitorNewIssueService _monitorNewIssueService;
        private readonly IConfigService _configService;
        private readonly IImportListExclusionService _importListExclusionService;
        private readonly Logger _logger;

        public RefreshVolumeService(IProvideVolumeInfo volumeInfo,
                                    IVolumeService volumeService,
                                    IVolumeMetadataService volumeMetadataService,
                                    IIssueService issueService,
                                    IMetadataProfileService metadataProfileService,
                                    IRefreshIssueService refreshIssueService,
                                    IRefreshSeriesService refreshSeriesService,
                                    IEventAggregator eventAggregator,
                                    IManageCommandQueue commandQueueManager,
                                    IMediaFileService mediaFileService,
                                    IHistoryService historyService,
                                    IRootFolderService rootFolderService,
                                    ICheckIfVolumeShouldBeRefreshed checkIfVolumeShouldBeRefreshed,
                                    IMonitorNewIssueService monitorNewIssueService,
                                    IConfigService configService,
                                    IImportListExclusionService importListExclusionService,
                                    Logger logger)
        : base(logger, volumeMetadataService)
        {
            _volumeInfo = volumeInfo;
            _volumeService = volumeService;
            _issueService = issueService;
            _metadataProfileService = metadataProfileService;
            _refreshIssueService = refreshIssueService;
            _refreshSeriesService = refreshSeriesService;
            _eventAggregator = eventAggregator;
            _commandQueueManager = commandQueueManager;
            _mediaFileService = mediaFileService;
            _historyService = historyService;
            _rootFolderService = rootFolderService;
            _checkIfVolumeShouldBeRefreshed = checkIfVolumeShouldBeRefreshed;
            _monitorNewIssueService = monitorNewIssueService;
            _configService = configService;
            _importListExclusionService = importListExclusionService;
            _logger = logger;
        }

        private Volume GetSkyhookData(string foreignId)
        {
            try
            {
                return _volumeInfo.GetVolumeInfo(foreignId);
            }
            catch (VolumeNotFoundException)
            {
                _logger.Error($"Could not find volume with id {foreignId}");
            }

            return null;
        }

        protected override RemoteData GetRemoteData(Volume local, List<Volume> remote, Volume data)
        {
            var result = new RemoteData();

            if (data != null)
            {
                result.Entity = data;
                result.Metadata = new List<VolumeMetadata> { data.Metadata.Value };
            }

            return result;
        }

        protected override bool ShouldDelete(Volume local)
        {
            return !_mediaFileService.GetFilesByVolume(local.Id).Any();
        }

        protected override void LogProgress(Volume local)
        {
            _logger.ProgressInfo("Updating Info for {0}", local.Name);
        }

        protected override bool IsMerge(Volume local, Volume remote)
        {
            _logger.Trace($"local: {local.VolumeMetadataId} remote: {remote.Metadata.Value.Id}");
            return local.VolumeMetadataId != remote.Metadata.Value.Id;
        }

        protected override UpdateResult UpdateEntity(Volume local, Volume remote)
        {
            var result = UpdateResult.None;

            if (!local.Metadata.Value.Equals(remote.Metadata.Value))
            {
                result = UpdateResult.UpdateTags;
            }

            local.UseMetadataFrom(remote);
            local.Metadata = remote.Metadata;
            local.Series = remote.Series.Value;
            local.LastInfoSync = DateTime.UtcNow;

            try
            {
                local.Path = new DirectoryInfo(local.Path).FullName;
                local.Path = local.Path.GetActualCasing();
            }
            catch (Exception e)
            {
                _logger.Warn(e, "Couldn't update volume path for " + local.Path);
            }

            return result;
        }

        protected override UpdateResult MoveEntity(Volume local, Volume remote)
        {
            _logger.Debug($"Updating foreign id for {local} to {remote}");

            // We are moving from one metadata to another (will already have been poplated)
            local.VolumeMetadataId = remote.Metadata.Value.Id;
            local.Metadata = remote.Metadata.Value;

            // Update list exclusion if one exists
            var importExclusion = _importListExclusionService.FindByForeignId(local.Metadata.Value.ForeignVolumeId);

            if (importExclusion != null)
            {
                importExclusion.ForeignId = remote.Metadata.Value.ForeignVolumeId;
                _importListExclusionService.Update(importExclusion);
            }

            // Do the standard update
            UpdateEntity(local, remote);

            // We know we need to update tags as volume id has changed
            return UpdateResult.UpdateTags;
        }

        protected override UpdateResult MergeEntity(Volume local, Volume target, Volume remote)
        {
            _logger.Warn($"Volume {local} was replaced with {remote} because the original was a duplicate.");

            // Update list exclusion if one exists
            var importExclusionLocal = _importListExclusionService.FindByForeignId(local.Metadata.Value.ForeignVolumeId);

            if (importExclusionLocal != null)
            {
                var importExclusionTarget = _importListExclusionService.FindByForeignId(target.Metadata.Value.ForeignVolumeId);
                if (importExclusionTarget == null)
                {
                    importExclusionLocal.ForeignId = remote.Metadata.Value.ForeignVolumeId;
                    _importListExclusionService.Update(importExclusionLocal);
                }
            }

            // move any issues over to the new volume and remove the local volume
            var issues = _issueService.GetIssuesByVolume(local.Id);
            issues.ForEach(x => x.VolumeMetadataId = target.VolumeMetadataId);
            _issueService.UpdateMany(issues);
            _volumeService.DeleteVolume(local.Id, false);

            // Update history entries to new id
            var items = _historyService.GetByVolume(local.Id, null);
            items.ForEach(x => x.VolumeId = target.Id);
            _historyService.UpdateMany(items);

            // We know we need to update tags as volume id has changed
            return UpdateResult.UpdateTags;
        }

        protected override Volume GetEntityByForeignId(Volume local)
        {
            return _volumeService.FindById(local.ForeignVolumeId);
        }

        protected override void SaveEntity(Volume local)
        {
            _volumeService.UpdateVolume(local);
        }

        protected override void DeleteEntity(Volume local, bool deleteFiles)
        {
            _volumeService.DeleteVolume(local.Id, deleteFiles);
        }

        protected override List<Issue> GetRemoteChildren(Volume local, Volume remote)
        {
            var filtered = _metadataProfileService.FilterIssues(remote, local.MetadataProfileId);

            var all = filtered.DistinctBy(m => m.ForeignIssueId).ToList();
            var ids = all.Select(x => x.ForeignIssueId).ToList();
            var excluded = _importListExclusionService.FindByForeignId(ids).Select(x => x.ForeignId).ToList();
            return all.Where(x => !excluded.Contains(x.ForeignIssueId)).ToList();
        }

        protected override List<Issue> GetLocalChildren(Volume entity, List<Issue> remoteChildren)
        {
            return _issueService.GetIssuesForRefresh(entity.VolumeMetadataId,
                                                     remoteChildren.Select(x => x.ForeignIssueId).ToList());
        }

        protected override Tuple<Issue, List<Issue>> GetMatchingExistingChildren(List<Issue> existingChildren, Issue remote)
        {
            var existingChild = existingChildren.SingleOrDefault(x => x.ForeignIssueId == remote.ForeignIssueId);
            var mergeChildren = new List<Issue>();
            return Tuple.Create(existingChild, mergeChildren);
        }

        protected override void PrepareNewChild(Issue child, Volume entity)
        {
            child.Volume = entity;
            child.VolumeMetadata = entity.Metadata.Value;
            child.VolumeMetadataId = entity.Metadata.Value.Id;
            child.Added = DateTime.UtcNow;
            child.LastInfoSync = DateTime.MinValue;
            child.Monitored = entity.Monitored;
        }

        protected override void PrepareExistingChild(Issue local, Issue remote, Volume entity)
        {
            local.Volume = entity;
            local.VolumeMetadata = entity.Metadata.Value;
            local.VolumeMetadataId = entity.Metadata.Value.Id;

            remote.UseDbFieldsFrom(local);
        }

        protected override void ProcessChildren(Volume entity, SortedChildren children)
        {
            foreach (var issue in children.Added)
            {
                issue.Monitored = _monitorNewIssueService.ShouldMonitorNewIssue(issue, children.UpToDate, entity.MonitorNewItems);
            }
        }

        protected override void AddChildren(List<Issue> children)
        {
            _issueService.InsertMany(children);
        }

        protected override bool RefreshChildren(SortedChildren localChildren, List<Issue> remoteChildren, Volume remoteData, bool forceChildRefresh, bool forceUpdateFileTags, DateTime? lastUpdate)
        {
            return _refreshIssueService.RefreshIssueInfo(localChildren.All, remoteChildren, remoteData, forceChildRefresh, forceUpdateFileTags, lastUpdate);
        }

        protected override void PublishEntityUpdatedEvent(Volume entity)
        {
            _eventAggregator.PublishEvent(new VolumeUpdatedEvent(entity));
        }

        protected override void PublishRefreshCompleteEvent(Volume entity)
        {
            // little hack - trigger the series update here
            _refreshSeriesService.RefreshSeriesInfo(entity.VolumeMetadataId, entity.Series, entity, false, false, null);
            _eventAggregator.PublishEvent(new VolumeRefreshCompleteEvent(entity));
        }

        protected override void PublishChildrenUpdatedEvent(Volume entity, List<Issue> newChildren, List<Issue> updateChildren, List<Issue> deleteChildren)
        {
            _eventAggregator.PublishEvent(new IssueInfoRefreshedEvent(entity, newChildren, updateChildren, deleteChildren));
        }

        private void Rescan(List<int> volumeIds, bool isNew, CommandTrigger trigger, bool infoUpdated)
        {
            var rescanAfterRefresh = _configService.RescanAfterRefresh;
            var shouldRescan = true;

            if (isNew)
            {
                _logger.Trace("Forcing rescan. Reason: New volume added");
                shouldRescan = true;
            }
            else if (rescanAfterRefresh == RescanAfterRefreshType.Never)
            {
                _logger.Trace("Skipping rescan. Reason: never rescan after refresh");
                shouldRescan = false;
            }
            else if (rescanAfterRefresh == RescanAfterRefreshType.AfterManual && trigger != CommandTrigger.Manual)
            {
                _logger.Trace("Skipping rescan. Reason: not after automatic refreshes");
                shouldRescan = false;
            }
            else if (!infoUpdated)
            {
                _logger.Trace("Skipping rescan. Reason: no metadata updated");
                shouldRescan = false;
            }

            if (shouldRescan)
            {
                // some metadata has updated so rescan unmatched
                // (but don't add new volumes to reduce repeated searches against api)
                var folders = _rootFolderService.All().Select(x => x.Path).ToList();

                _commandQueueManager.Push(new RescanFoldersCommand(folders, FilterFilesType.Matched, false, volumeIds));
            }
        }

        private void RefreshSelectedVolumes(List<int> volumeIds, bool isNew, CommandTrigger trigger)
        {
            var updated = false;
            var volumes = _volumeService.GetVolumes(volumeIds);

            foreach (var volume in volumes)
            {
                try
                {
                    var data = GetSkyhookData(volume.ForeignVolumeId);
                    updated |= RefreshEntityInfo(volume, null, data, true, false, null);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Couldn't refresh info for {0}", volume);
                }
            }

            Rescan(volumeIds, isNew, trigger, updated);
        }

        public void Execute(BulkRefreshVolumeCommand message)
        {
            RefreshSelectedVolumes(message.VolumeIds, message.AreNewVolumes, message.Trigger);
        }

        public void Execute(RefreshVolumeCommand message)
        {
            var trigger = message.Trigger;
            var isNew = message.IsNewVolume;

            if (message.VolumeId.HasValue)
            {
                RefreshSelectedVolumes(new List<int> { message.VolumeId.Value }, isNew, trigger);
            }
            else
            {
                var updated = false;
                var volumes = _volumeService.GetAllVolumes().OrderBy(c => c.Name).ToList();
                var volumeIds = volumes.Select(x => x.Id).ToList();

                var updatedGoodreadsVolumes = new HashSet<string>();

                if (message.LastExecutionTime.HasValue && message.LastExecutionTime.Value.AddDays(14) > DateTime.UtcNow)
                {
                    updatedGoodreadsVolumes = _volumeInfo.GetChangedVolumes(message.LastStartTime.Value);
                }

                foreach (var volume in volumes)
                {
                    var manualTrigger = message.Trigger == CommandTrigger.Manual;

                    if ((updatedGoodreadsVolumes == null && _checkIfVolumeShouldBeRefreshed.ShouldRefresh(volume)) ||
                        (updatedGoodreadsVolumes != null && updatedGoodreadsVolumes.Contains(volume.ForeignVolumeId)) ||
                        manualTrigger)
                    {
                        try
                        {
                            LogProgress(volume);
                            var data = GetSkyhookData(volume.ForeignVolumeId);
                            updated |= RefreshEntityInfo(volume, null, data, manualTrigger, false, message.LastStartTime);
                        }
                        catch (Exception e)
                        {
                            _logger.Error(e, "Couldn't refresh info for {0}", volume);
                        }
                    }
                    else
                    {
                        _logger.Info("Skipping refresh of volume: {0}", volume.Name);
                    }
                }

                Rescan(volumeIds, isNew, trigger, updated);
            }
        }
    }
}
