using System;
using System.Collections.Generic;
using System.Linq;
using Inkarr.Api.V1.Issues;
using Inkarr.Http;
using Inkarr.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.VolumeStats;
using NzbDrone.SignalR;

namespace Inkarr.Api.V1.Calendar
{
    [V1ApiController]
    public class CalendarController : IssueControllerWithSignalR
    {
        public CalendarController(IIssueService issueService,
                              ISeriesIssueLinkService seriesIssueLinkService,
                              IVolumeStatisticsService volumeStatisticsService,
                              IMapCoversToLocal coverMapper,
                              IUpgradableSpecification upgradableSpecification,
                              IBroadcastSignalRMessage signalRBroadcaster)
        : base(issueService, seriesIssueLinkService, volumeStatisticsService, coverMapper, upgradableSpecification, signalRBroadcaster)
        {
        }

        [HttpGet]
        public List<IssueResource> GetCalendar(DateTime? start, DateTime? end, bool unmonitored = false, bool includeVolume = false)
        {
            //TODO: Add Issue Image support to IssueControllerWithSignalR
            var includeIssueImages = Request.GetBooleanQueryParameter("includeIssueImages");

            var startUse = start ?? DateTime.Today;
            var endUse = end ?? DateTime.Today.AddDays(2);

            var resources = MapToResource(_issueService.IssuesBetweenDates(startUse, endUse, unmonitored), includeVolume);

            return resources.OrderBy(e => e.ReleaseDate).ToList();
        }
    }
}
