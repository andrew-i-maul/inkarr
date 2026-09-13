using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Inkarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Download;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Validation;
using NzbDrone.Core.Validation.Paths;
using NzbDrone.Core.VolumeStats;
using NzbDrone.Http.REST.Attributes;
using NzbDrone.SignalR;

namespace Inkarr.Api.V1.Issues
{
    [V1ApiController]
    public class IssueController : IssueControllerWithSignalR,
        IHandle<IssueGrabbedEvent>,
        IHandle<IssueEditedEvent>,
        IHandle<IssueUpdatedEvent>,
        IHandle<IssueDeletedEvent>,
        IHandle<IssueImportedEvent>,
        IHandle<TrackImportedEvent>,
        IHandle<IssueFileDeletedEvent>
    {
        protected readonly IVolumeService _volumeService;
        protected readonly IEditionService _editionService;
        protected readonly IAddIssueService _addIssueService;

        public IssueController(IVolumeService volumeService,
                          IIssueService issueService,
                          IAddIssueService addIssueService,
                          IEditionService editionService,
                          ISeriesIssueLinkService seriesIssueLinkService,
                          IVolumeStatisticsService volumeStatisticsService,
                          IMapCoversToLocal coverMapper,
                          IUpgradableSpecification upgradableSpecification,
                          IBroadcastSignalRMessage signalRBroadcaster,
                          QualityProfileExistsValidator qualityProfileExistsValidator,
                          MetadataProfileExistsValidator metadataProfileExistsValidator)

        : base(issueService, seriesIssueLinkService, volumeStatisticsService, coverMapper, upgradableSpecification, signalRBroadcaster)
        {
            _volumeService = volumeService;
            _editionService = editionService;
            _addIssueService = addIssueService;

            PostValidator.RuleFor(s => s.ForeignIssueId).NotEmpty();
            PostValidator.RuleFor(s => s.Volume.QualityProfileId).SetValidator(qualityProfileExistsValidator);
            PostValidator.RuleFor(s => s.Volume.MetadataProfileId).SetValidator(metadataProfileExistsValidator);
            PostValidator.RuleFor(s => s.Volume.RootFolderPath).IsValidPath().When(s => s.Volume.Path.IsNullOrWhiteSpace());
            PostValidator.RuleFor(s => s.Volume.ForeignVolumeId).NotEmpty();
        }

        [HttpGet]
        public List<IssueResource> GetIssues([FromQuery]int? volumeId,
            [FromQuery]List<int> issueIds,
            [FromQuery]string titleSlug,
            [FromQuery]bool includeAllVolumeIssues = false)
        {
            if (!volumeId.HasValue && !issueIds.Any() && titleSlug.IsNullOrWhiteSpace())
            {
                var editionTask = Task.Run(() => _editionService.GetAllMonitoredEditions());
                var metadataTask = Task.Run(() => _volumeService.GetAllVolumes());
                var issues = _issueService.GetAllIssues();

                var editions = editionTask.GetAwaiter().GetResult().GroupBy(x => x.IssueId).ToDictionary(x => x.Key, y => y.ToList());

                var volumes = metadataTask.GetAwaiter().GetResult().ToDictionary(x => x.VolumeMetadataId);

                foreach (var issue in issues)
                {
                    issue.Volume = volumes[issue.VolumeMetadataId];
                    if (editions.TryGetValue(issue.Id, out var issueEditions))
                    {
                        issue.Editions = issueEditions;
                    }
                    else
                    {
                        issue.Editions = new List<Edition>();
                    }
                }

                return MapToResource(issues, false);
            }

            if (volumeId.HasValue)
            {
                var issues = _issueService.GetIssuesByVolume(volumeId.Value);

                var volume = _volumeService.GetVolume(volumeId.Value);
                var editions = _editionService.GetEditionsByVolume(volumeId.Value)
                    .GroupBy(x => x.IssueId)
                    .ToDictionary(x => x.Key, y => y.ToList());

                foreach (var issue in issues)
                {
                    issue.Volume = volume;
                    if (editions.TryGetValue(issue.Id, out var issueEditions))
                    {
                        issue.Editions = issueEditions;
                    }
                    else
                    {
                        issue.Editions = new List<Edition>();
                    }
                }

                return MapToResource(issues, false);
            }

            if (titleSlug.IsNotNullOrWhiteSpace())
            {
                var issue = _issueService.FindBySlug(titleSlug);

                if (issue == null)
                {
                    return MapToResource(new List<Issue>(), false);
                }

                if (includeAllVolumeIssues)
                {
                    return MapToResource(_issueService.GetIssuesByVolume(issue.VolumeId), false);
                }
                else
                {
                    return MapToResource(new List<Issue> { issue }, false);
                }
            }

            return MapToResource(_issueService.GetIssues(issueIds), false);
        }

        [HttpGet("{id:int}/overview")]
        public object Overview(int id)
        {
            var overview = _editionService.GetEditionsByIssue(id).Single(x => x.Monitored).Overview;
            return new
            {
                id,
                overview
            };
        }

        [RestPostById]
        public ActionResult<IssueResource> AddIssue(IssueResource issueResource)
        {
            var issue = _addIssueService.AddIssue(issueResource.ToModel());

            return Created(issue.Id);
        }

        [RestPutById]
        public ActionResult<IssueResource> UpdateIssue(IssueResource issueResource)
        {
            var issue = _issueService.GetIssue(issueResource.Id);

            var model = issueResource.ToModel(issue);

            _issueService.UpdateIssue(model);
            _editionService.UpdateMany(model.Editions.Value);

            BroadcastResourceChange(ModelAction.Updated, model.Id);

            return Accepted(model.Id);
        }

        [RestDeleteById]
        public void DeleteIssue(int id, bool deleteFiles = false, bool addImportListExclusion = false)
        {
            _issueService.DeleteIssue(id, deleteFiles, addImportListExclusion);
        }

        [HttpPut("monitor")]
        public IActionResult SetIssuesMonitored([FromBody]IssuesMonitoredResource resource)
        {
            _issueService.SetMonitored(resource.IssueIds, resource.Monitored);

            if (resource.IssueIds.Count == 1)
            {
                _issueService.SetIssueMonitored(resource.IssueIds.First(), resource.Monitored);
            }
            else
            {
                _issueService.SetMonitored(resource.IssueIds, resource.Monitored);
            }

            return Accepted(MapToResource(_issueService.GetIssues(resource.IssueIds), false));
        }

        [NonAction]
        public void Handle(IssueGrabbedEvent message)
        {
            foreach (var issue in message.Issue.Issues)
            {
                var resource = issue.ToResource();
                resource.Grabbed = true;

                BroadcastResourceChange(ModelAction.Updated, resource);
            }
        }

        [NonAction]
        public void Handle(IssueEditedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, MapToResource(message.Issue, true));
        }

        [NonAction]
        public void Handle(IssueUpdatedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, MapToResource(message.Issue, true));
        }

        [NonAction]
        public void Handle(IssueDeletedEvent message)
        {
            BroadcastResourceChange(ModelAction.Deleted, message.Issue.ToResource());
        }

        [NonAction]
        public void Handle(IssueImportedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, MapToResource(message.Issue, true));
        }

        [NonAction]
        public void Handle(TrackImportedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, message.IssueInfo.Issue.ToResource());
        }

        [NonAction]
        public void Handle(IssueFileDeletedEvent message)
        {
            if (message.Reason == DeleteMediaFileReason.Upgrade)
            {
                return;
            }

            BroadcastResourceChange(ModelAction.Updated, MapToResource(message.IssueFile.Edition.Value.Issue.Value, true));
        }
    }
}
