using System.Collections.Generic;
using System.Linq;
using Inkarr.Api.V1.Volume;
using Inkarr.Http.REST;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.VolumeStats;
using NzbDrone.SignalR;

namespace Inkarr.Api.V1.Issues
{
    public abstract class IssueControllerWithSignalR : RestControllerWithSignalR<IssueResource, Issue>
    {
        protected readonly IIssueService _issueService;
        protected readonly ISeriesIssueLinkService _seriesIssueLinkService;
        protected readonly IVolumeStatisticsService _volumeStatisticsService;
        protected readonly IUpgradableSpecification _qualityUpgradableSpecification;
        protected readonly IMapCoversToLocal _coverMapper;

        protected IssueControllerWithSignalR(IIssueService issueService,
                                        ISeriesIssueLinkService seriesIssueLinkService,
                                        IVolumeStatisticsService volumeStatisticsService,
                                        IMapCoversToLocal coverMapper,
                                        IUpgradableSpecification qualityUpgradableSpecification,
                                        IBroadcastSignalRMessage signalRBroadcaster)
            : base(signalRBroadcaster)
        {
            _issueService = issueService;
            _seriesIssueLinkService = seriesIssueLinkService;
            _volumeStatisticsService = volumeStatisticsService;
            _coverMapper = coverMapper;
            _qualityUpgradableSpecification = qualityUpgradableSpecification;
        }

        protected override IssueResource GetResourceById(int id)
        {
            var issue = _issueService.GetIssue(id);
            var resource = MapToResource(issue, true);
            return resource;
        }

        protected override IssueResource GetResourceByIdForBroadcast(int id)
        {
            var issue = _issueService.GetIssue(id);
            var resource = MapToResource(issue, false);
            return resource;
        }

        protected IssueResource MapToResource(Issue issue, bool includeVolume)
        {
            var resource = issue.ToResource();

            if (includeVolume)
            {
                var volume = issue.Volume.Value;

                resource.Volume = volume.ToResource();
            }

            FetchAndLinkIssueStatistics(resource);
            MapCoversToLocal(resource);

            return resource;
        }

        protected List<IssueResource> MapToResource(List<Issue> issues, bool includeVolume)
        {
            var seriesLinks = _seriesIssueLinkService.GetLinksByIssue(issues.Select(x => x.Id).ToList())
                .GroupBy(x => x.IssueId)
                .ToDictionary(x => x.Key, y => y.ToList());

            foreach (var issue in issues)
            {
                if (seriesLinks.TryGetValue(issue.Id, out var links))
                {
                    issue.SeriesLinks = links;
                }
                else
                {
                    issue.SeriesLinks = new List<SeriesIssueLink>();
                }
            }

            var result = issues.ToResource();

            if (includeVolume)
            {
                var volumeDict = new Dictionary<int, NzbDrone.Core.Issues.Volume>();
                for (var i = 0; i < issues.Count; i++)
                {
                    var issue = issues[i];
                    var resource = result[i];
                    var volume = volumeDict.GetValueOrDefault(issues[i].VolumeMetadataId) ?? issue.Volume?.Value;
                    volumeDict[volume.VolumeMetadataId] = volume;

                    resource.Volume = volume.ToResource();
                }
            }

            var volumeStats = _volumeStatisticsService.VolumeStatistics();
            LinkVolumeStatistics(result, volumeStats);
            MapCoversToLocal(result.ToArray());

            return result;
        }

        private void FetchAndLinkIssueStatistics(IssueResource resource)
        {
            LinkVolumeStatistics(resource, _volumeStatisticsService.VolumeStatistics(resource.VolumeId));
        }

        private void LinkVolumeStatistics(List<IssueResource> resources, List<VolumeStatistics> volumeStatistics)
        {
            var issueStatsDict = volumeStatistics.SelectMany(x => x.IssueStatistics).ToDictionary(x => x.IssueId);

            foreach (var issue in resources)
            {
                if (issueStatsDict.TryGetValue(issue.Id, out var stats))
                {
                    issue.Statistics = stats.ToResource();
                }
            }
        }

        private void LinkVolumeStatistics(IssueResource resource, VolumeStatistics volumeStatistics)
        {
            if (volumeStatistics?.IssueStatistics != null)
            {
                var dictIssueStats = volumeStatistics.IssueStatistics.ToDictionary(v => v.IssueId);

                resource.Statistics = dictIssueStats.GetValueOrDefault(resource.Id).ToResource();
            }
        }

        private void MapCoversToLocal(params IssueResource[] issues)
        {
            foreach (var issueResource in issues)
            {
                _coverMapper.ConvertToLocalUrls(issueResource.Id, MediaCoverEntity.Issue, issueResource.Images);
            }
        }
    }
}
