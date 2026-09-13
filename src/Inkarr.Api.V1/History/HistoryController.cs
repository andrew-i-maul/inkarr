using System;
using System.Collections.Generic;
using System.Linq;
using Inkarr.Api.V1.Issues;
using Inkarr.Api.V1.Volume;
using Inkarr.Http;
using Inkarr.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Download;
using NzbDrone.Core.History;
using NzbDrone.Core.Issues;

namespace Inkarr.Api.V1.History
{
    [V1ApiController]
    public class HistoryController : Controller
    {
        private readonly IHistoryService _historyService;
        private readonly ICustomFormatCalculationService _formatCalculator;
        private readonly IUpgradableSpecification _upgradableSpecification;
        private readonly IFailedDownloadService _failedDownloadService;
        private readonly IVolumeService _volumeService;

        public HistoryController(IHistoryService historyService,
                             ICustomFormatCalculationService formatCalculator,
                             IUpgradableSpecification upgradableSpecification,
                             IFailedDownloadService failedDownloadService,
                             IVolumeService volumeService)
        {
            _historyService = historyService;
            _formatCalculator = formatCalculator;
            _upgradableSpecification = upgradableSpecification;
            _failedDownloadService = failedDownloadService;
            _volumeService = volumeService;
        }

        protected HistoryResource MapToResource(EntityHistory model, bool includeVolume, bool includeIssue)
        {
            var resource = model.ToResource(_formatCalculator);

            if (includeVolume)
            {
                resource.Volume = model.Volume.ToResource();
            }

            if (includeIssue)
            {
                resource.Issue = model.Issue.ToResource();
            }

            if (model.Volume != null)
            {
                resource.QualityCutoffNotMet = _upgradableSpecification.QualityCutoffNotMet(model.Volume.QualityProfile.Value, model.Quality);
            }

            return resource;
        }

        [HttpGet]
        [Produces("application/json")]
        public PagingResource<HistoryResource> GetHistory([FromQuery] PagingRequestResource paging, bool includeVolume, bool includeIssue, [FromQuery(Name = "eventType")] int[] eventTypes, int? issueId, string downloadId)
        {
            var pagingResource = new PagingResource<HistoryResource>(paging);
            var pagingSpec = pagingResource.MapToPagingSpec<HistoryResource, EntityHistory>("date", SortDirection.Descending);

            if (eventTypes != null && eventTypes.Any())
            {
                pagingSpec.FilterExpressions.Add(v => eventTypes.Contains((int)v.EventType));
            }

            if (issueId.HasValue)
            {
                pagingSpec.FilterExpressions.Add(h => h.IssueId == issueId);
            }

            if (downloadId.IsNotNullOrWhiteSpace())
            {
                pagingSpec.FilterExpressions.Add(h => h.DownloadId == downloadId);
            }

            return pagingSpec.ApplyToPage(_historyService.Paged, h => MapToResource(h, includeVolume, includeIssue));
        }

        [HttpGet("since")]
        public List<HistoryResource> GetHistorySince(DateTime date, EntityHistoryEventType? eventType = null, bool includeVolume = false, bool includeIssue = false)
        {
            return _historyService.Since(date, eventType).Select(h => MapToResource(h, includeVolume, includeIssue)).ToList();
        }

        [HttpGet("volume")]
        public List<HistoryResource> GetVolumeHistory(int volumeId, int? issueId = null, EntityHistoryEventType? eventType = null, bool includeVolume = false, bool includeIssue = false)
        {
            var volume = _volumeService.GetVolume(volumeId);

            if (issueId.HasValue)
            {
                return _historyService.GetByIssue(issueId.Value, eventType).Select(h =>
                {
                    h.Volume = volume;

                    return MapToResource(h, includeVolume, includeIssue);
                }).ToList();
            }

            return _historyService.GetByVolume(volumeId, eventType).Select(h =>
            {
                h.Volume = volume;

                return MapToResource(h, includeVolume, includeIssue);
            }).ToList();
        }

        [HttpPost("failed/{id}")]
        public object MarkAsFailed([FromRoute] int id)
        {
            _failedDownloadService.MarkAsFailed(id);
            return new { };
        }
    }
}
