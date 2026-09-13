using Inkarr.Api.V1.Issues;
using Inkarr.Http;
using Inkarr.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.VolumeStats;
using NzbDrone.SignalR;

namespace Inkarr.Api.V1.Wanted
{
    [V1ApiController("wanted/missing")]
    public class MissingController : IssueControllerWithSignalR
    {
        public MissingController(IIssueService issueService,
                             ISeriesIssueLinkService seriesIssueLinkService,
                             IVolumeStatisticsService volumeStatisticsService,
                             IMapCoversToLocal coverMapper,
                             IUpgradableSpecification upgradableSpecification,
                             IBroadcastSignalRMessage signalRBroadcaster)
        : base(issueService, seriesIssueLinkService, volumeStatisticsService, coverMapper, upgradableSpecification, signalRBroadcaster)
        {
        }

        [HttpGet]
        public PagingResource<IssueResource> GetMissingIssues([FromQuery] PagingRequestResource paging, bool includeVolume = false, bool monitored = true)
        {
            var pagingResource = new PagingResource<IssueResource>(paging);
            var pagingSpec = new PagingSpec<Issue>
            {
                Page = pagingResource.Page,
                PageSize = pagingResource.PageSize,
                SortKey = pagingResource.SortKey,
                SortDirection = pagingResource.SortDirection
            };

            if (monitored)
            {
                pagingSpec.FilterExpressions.Add(v => v.Monitored == true && v.Volume.Value.Monitored == true);
            }
            else
            {
                pagingSpec.FilterExpressions.Add(v => v.Monitored == false || v.Volume.Value.Monitored == false);
            }

            return pagingSpec.ApplyToPage(_issueService.IssuesWithoutFiles, v => MapToResource(v, includeVolume));
        }
    }
}
