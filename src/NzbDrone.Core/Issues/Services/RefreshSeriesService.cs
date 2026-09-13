using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Issues
{
    public interface IRefreshSeriesService
    {
        bool RefreshSeriesInfo(int volumeMetadataId, List<Series> remoteIssues, Volume remoteData, bool forceIssueRefresh, bool forceUpdateFileTags, DateTime? lastUpdate);
    }

    public class RefreshSeriesService : RefreshEntityServiceBase<Series, SeriesIssueLink>, IRefreshSeriesService
    {
        private readonly IIssueService _issueService;
        private readonly ISeriesService _seriesService;
        private readonly ISeriesIssueLinkService _linkService;
        private readonly IRefreshSeriesIssueLinkService _refreshLinkService;
        private readonly Logger _logger;

        public RefreshSeriesService(IIssueService issueService,
                                    ISeriesService seriesService,
                                    ISeriesIssueLinkService linkService,
                                    IRefreshSeriesIssueLinkService refreshLinkService,
                                    IVolumeMetadataService volumeMetadataService,
                                    Logger logger)
        : base(logger, volumeMetadataService)
        {
            _issueService = issueService;
            _seriesService = seriesService;
            _linkService = linkService;
            _refreshLinkService = refreshLinkService;
            _logger = logger;
        }

        protected override RemoteData GetRemoteData(Series local, List<Series> remote, Volume data)
        {
            return new RemoteData
            {
                Entity = remote.SingleOrDefault(x => x.ForeignSeriesId == local.ForeignSeriesId)
            };
        }

        protected override bool IsMerge(Series local, Series remote)
        {
            return local.ForeignSeriesId != remote.ForeignSeriesId;
        }

        protected override UpdateResult UpdateEntity(Series local, Series remote)
        {
            if (local.Equals(remote))
            {
                return UpdateResult.None;
            }

            local.UseMetadataFrom(remote);

            return UpdateResult.UpdateTags;
        }

        protected override Series GetEntityByForeignId(Series local)
        {
            return _seriesService.FindById(local.ForeignSeriesId);
        }

        protected override void SaveEntity(Series local)
        {
            // Use UpdateMany to avoid firing the issue edited event
            _seriesService.UpdateMany(new List<Series> { local });
        }

        protected override void DeleteEntity(Series local, bool deleteFiles)
        {
            _logger.Trace($"Removing links for series {local} volume {local.ForeignVolumeId}");
            var children = GetLocalChildren(local, null);
            _linkService.DeleteMany(children);

            if (!_linkService.GetLinksBySeries(local.Id).Any())
            {
                _logger.Trace($"Series {local} has no links remaining, removing");
                _seriesService.Delete(local.Id);
            }
        }

        protected override List<SeriesIssueLink> GetRemoteChildren(Series local, Series remote)
        {
            return remote.LinkItems;
        }

        protected override List<SeriesIssueLink> GetLocalChildren(Series entity, List<SeriesIssueLink> remoteChildren)
        {
            return _linkService.GetLinksBySeriesAndVolume(entity.Id, entity.ForeignVolumeId);
        }

        protected override Tuple<SeriesIssueLink, List<SeriesIssueLink>> GetMatchingExistingChildren(List<SeriesIssueLink> existingChildren, SeriesIssueLink remote)
        {
            var existingChild = existingChildren.SingleOrDefault(x => x.IssueId == remote.Issue.Value.Id);
            var mergeChildren = new List<SeriesIssueLink>();
            return Tuple.Create(existingChild, mergeChildren);
        }

        protected override void PrepareNewChild(SeriesIssueLink child, Series entity)
        {
            child.Series = entity;
            child.SeriesId = entity.Id;
            child.IssueId = child.Issue.Value.Id;
        }

        protected override void PrepareExistingChild(SeriesIssueLink local, SeriesIssueLink remote, Series entity)
        {
            local.Series = entity;
            local.SeriesId = entity.Id;

            remote.Id = local.Id;
            remote.IssueId = local.IssueId;
            remote.SeriesId = entity.Id;
        }

        protected override void AddChildren(List<SeriesIssueLink> children)
        {
            _linkService.InsertMany(children);
        }

        protected override bool RefreshChildren(SortedChildren localChildren, List<SeriesIssueLink> remoteChildren, Volume remoteData, bool forceChildRefresh, bool forceUpdateFileTags, DateTime? lastUpdate)
        {
            return _refreshLinkService.RefreshSeriesIssueLinkInfo(localChildren.Added, localChildren.Updated, localChildren.Merged, localChildren.Deleted, localChildren.UpToDate, remoteChildren, forceUpdateFileTags);
        }

        public bool RefreshSeriesInfo(int volumeMetadataId, List<Series> remoteSeries, Volume remoteData, bool forceIssueRefresh, bool forceUpdateFileTags, DateTime? lastUpdate)
        {
            var updated = false;

            var existingByVolume = _seriesService.GetByVolumeMetadataId(volumeMetadataId);
            var existingBySeries = _seriesService.FindById(remoteSeries.Select(x => x.ForeignSeriesId).ToList());
            var existing = existingByVolume.Concat(existingBySeries).GroupBy(x => x.ForeignSeriesId).Select(x => x.First()).ToList();

            var issues = _issueService.GetIssuesByVolumeMetadataId(volumeMetadataId);
            var issueDict = issues.ToDictionary(x => x.ForeignIssueId);
            var links = new List<SeriesIssueLink>();

            foreach (var s in remoteData.Series.Value)
            {
                s.LinkItems.Value.ForEach(x => x.Series = s);
                links.AddRange(s.LinkItems.Value.Where(x => issueDict.ContainsKey(x.Issue.Value.ForeignIssueId)));
            }

            var grouped = links.GroupBy(x => x.Series.Value);

            // Put in the links that go with the issues we actually have
            foreach (var group in grouped)
            {
                group.Key.LinkItems = group.ToList();
            }

            remoteSeries = grouped.Select(x => x.Key).ToList();

            var toAdd = remoteSeries.ExceptBy(x => x.ForeignSeriesId, existing, x => x.ForeignSeriesId, StringComparer.Ordinal).ToList();
            var all = toAdd.Union(existing).ToList();

            _seriesService.InsertMany(toAdd);

            foreach (var item in all)
            {
                item.ForeignVolumeId = remoteData.ForeignVolumeId;
                updated |= RefreshEntityInfo(item, remoteSeries, remoteData, true, forceUpdateFileTags, null);
            }

            return updated;
        }
    }
}
