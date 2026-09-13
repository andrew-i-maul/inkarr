using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Inkarr.Http;
using Inkarr.Http.REST;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Commands;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Validation;
using NzbDrone.Core.Validation.Paths;
using NzbDrone.Core.VolumeStats;
using NzbDrone.Http.REST.Attributes;
using NzbDrone.SignalR;

namespace Inkarr.Api.V1.Volume
{
    [V1ApiController]
    public class VolumeController : RestControllerWithSignalR<VolumeResource, NzbDrone.Core.Issues.Volume>,
                                IHandle<IssueImportedEvent>,
                                IHandle<IssueEditedEvent>,
                                IHandle<IssueFileDeletedEvent>,
                                IHandle<VolumeAddedEvent>,
                                IHandle<VolumeUpdatedEvent>,
                                IHandle<VolumeEditedEvent>,
                                IHandle<VolumeDeletedEvent>,
                                IHandle<VolumeRenamedEvent>,
                                IHandle<MediaCoversUpdatedEvent>
    {
        private readonly IVolumeService _volumeService;
        private readonly IIssueService _issueService;
        private readonly IAddVolumeService _addVolumeService;
        private readonly IVolumeStatisticsService _volumeStatisticsService;
        private readonly IMapCoversToLocal _coverMapper;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly IRootFolderService _rootFolderService;

        public VolumeController(IBroadcastSignalRMessage signalRBroadcaster,
                            IVolumeService volumeService,
                            IIssueService issueService,
                            IAddVolumeService addVolumeService,
                            IVolumeStatisticsService volumeStatisticsService,
                            IMapCoversToLocal coverMapper,
                            IManageCommandQueue commandQueueManager,
                            IRootFolderService rootFolderService,
                            RecycleBinValidator recycleBinValidator,
                            RootFolderValidator rootFolderValidator,
                            MappedNetworkDriveValidator mappedNetworkDriveValidator,
                            VolumePathValidator volumePathValidator,
                            VolumeExistsValidator volumeExistsValidator,
                            VolumeAncestorValidator volumeAncestorValidator,
                            SystemFolderValidator systemFolderValidator,
                            QualityProfileExistsValidator qualityProfileExistsValidator,
                            MetadataProfileExistsValidator metadataProfileExistsValidator,
                            VolumeFolderAsRootFolderValidator volumeFolderAsRootFolderValidator)
            : base(signalRBroadcaster)
        {
            _volumeService = volumeService;
            _issueService = issueService;
            _addVolumeService = addVolumeService;
            _volumeStatisticsService = volumeStatisticsService;

            _coverMapper = coverMapper;
            _commandQueueManager = commandQueueManager;
            _rootFolderService = rootFolderService;

            Http.Validation.RuleBuilderExtensions.ValidId(SharedValidator.RuleFor(s => s.QualityProfileId));
            Http.Validation.RuleBuilderExtensions.ValidId(SharedValidator.RuleFor(s => s.MetadataProfileId));

            SharedValidator.RuleFor(s => s.Path)
                           .Cascade(CascadeMode.Stop)
                           .IsValidPath()
                           .SetValidator(rootFolderValidator)
                           .SetValidator(mappedNetworkDriveValidator)
                           .SetValidator(volumePathValidator)
                           .SetValidator(volumeAncestorValidator)
                           .SetValidator(recycleBinValidator)
                           .SetValidator(systemFolderValidator)
                           .When(s => !s.Path.IsNullOrWhiteSpace());

            SharedValidator.RuleFor(s => s.QualityProfileId).SetValidator(qualityProfileExistsValidator);
            SharedValidator.RuleFor(s => s.MetadataProfileId).SetValidator(metadataProfileExistsValidator);

            PostValidator.RuleFor(s => s.Path).IsValidPath().When(s => s.RootFolderPath.IsNullOrWhiteSpace());
            PostValidator.RuleFor(s => s.RootFolderPath)
                         .IsValidPath()
                         .SetValidator(volumeFolderAsRootFolderValidator)
                         .When(s => s.Path.IsNullOrWhiteSpace());
            PostValidator.RuleFor(s => s.VolumeName).NotEmpty();
            PostValidator.RuleFor(s => s.ForeignVolumeId).NotEmpty().SetValidator(volumeExistsValidator);

            PutValidator.RuleFor(s => s.Path).IsValidPath();
        }

        protected override VolumeResource GetResourceById(int id)
        {
            var volume = _volumeService.GetVolume(id);
            return GetVolumeResource(volume);
        }

        private VolumeResource GetVolumeResource(NzbDrone.Core.Issues.Volume volume)
        {
            if (volume == null)
            {
                return null;
            }

            var resource = volume.ToResource();
            MapCoversToLocal(resource);
            FetchAndLinkVolumeStatistics(resource);
            LinkNextPreviousIssues(resource);

            LinkRootFolderPath(resource);

            return resource;
        }

        [HttpGet]
        public List<VolumeResource> AllVolumes()
        {
            var volumeStats = _volumeStatisticsService.VolumeStatistics();
            var volumeResources = _volumeService.GetAllVolumes().ToResource();

            MapCoversToLocal(volumeResources.ToArray());
            LinkNextPreviousIssues(volumeResources.ToArray());
            LinkVolumeStatistics(volumeResources, volumeStats.ToDictionary(x => x.VolumeId));
            LinkRootFolderPath(volumeResources.ToArray());

            return volumeResources;
        }

        [RestPostById]
        public ActionResult<VolumeResource> AddVolume(VolumeResource volumeResource)
        {
            var volume = _addVolumeService.AddVolume(volumeResource.ToModel());

            return Created(volume.Id);
        }

        [RestPutById]
        public ActionResult<VolumeResource> UpdateVolume(VolumeResource volumeResource, bool moveFiles = false)
        {
            var volume = _volumeService.GetVolume(volumeResource.Id);

            if (moveFiles)
            {
                var sourcePath = volume.Path;
                var destinationPath = volumeResource.Path;

                _commandQueueManager.Push(new MoveVolumeCommand
                {
                    VolumeId = volume.Id,
                    SourcePath = sourcePath,
                    DestinationPath = destinationPath,
                    Trigger = CommandTrigger.Manual
                });
            }

            var model = volumeResource.ToModel(volume);

            _volumeService.UpdateVolume(model);

            BroadcastResourceChange(ModelAction.Updated, volumeResource);

            return Accepted(volumeResource.Id);
        }

        [RestDeleteById]
        public void DeleteVolume(int id, bool deleteFiles = false, bool addImportListExclusion = false)
        {
            _volumeService.DeleteVolume(id, deleteFiles, addImportListExclusion);
        }

        private void MapCoversToLocal(params VolumeResource[] volumes)
        {
            foreach (var volumeResource in volumes)
            {
                _coverMapper.ConvertToLocalUrls(volumeResource.Id, MediaCoverEntity.Volume, volumeResource.Images);
            }
        }

        private void LinkNextPreviousIssues(params VolumeResource[] volumes)
        {
            var nextIssues = _issueService.GetNextIssuesByVolumeMetadataId(volumes.Select(x => x.VolumeMetadataId));
            var lastIssues = _issueService.GetLastIssuesByVolumeMetadataId(volumes.Select(x => x.VolumeMetadataId));

            foreach (var volumeResource in volumes)
            {
                volumeResource.NextIssue = nextIssues.FirstOrDefault(x => x.VolumeMetadataId == volumeResource.VolumeMetadataId);
                volumeResource.LastIssue = lastIssues.FirstOrDefault(x => x.VolumeMetadataId == volumeResource.VolumeMetadataId);
            }
        }

        private void FetchAndLinkVolumeStatistics(VolumeResource resource)
        {
            LinkVolumeStatistics(resource, _volumeStatisticsService.VolumeStatistics(resource.Id));
        }

        private void LinkVolumeStatistics(List<VolumeResource> resources, Dictionary<int, VolumeStatistics> volumeStatistics)
        {
            foreach (var volume in resources)
            {
                if (volumeStatistics.TryGetValue(volume.Id, out var stats))
                {
                    LinkVolumeStatistics(volume, stats);
                }
            }
        }

        private void LinkVolumeStatistics(VolumeResource resource, VolumeStatistics volumeStatistics)
        {
            resource.Statistics = volumeStatistics.ToResource();
        }

        private void LinkRootFolderPath(params VolumeResource[] volumes)
        {
            var rootFolders = _rootFolderService.All();

            foreach (var volume in volumes)
            {
                volume.RootFolderPath = _rootFolderService.GetBestRootFolderPath(volume.Path, rootFolders);
            }
        }

        [NonAction]
        public void Handle(IssueImportedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, GetVolumeResource(message.Volume));
        }

        [NonAction]
        public void Handle(IssueEditedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, GetVolumeResource(message.Issue.Volume.Value));
        }

        [NonAction]
        public void Handle(IssueFileDeletedEvent message)
        {
            if (message.Reason == DeleteMediaFileReason.Upgrade)
            {
                return;
            }

            BroadcastResourceChange(ModelAction.Updated, GetVolumeResource(message.IssueFile.Volume.Value));
        }

        [NonAction]
        public void Handle(VolumeAddedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, GetVolumeResource(message.Volume));
        }

        [NonAction]
        public void Handle(VolumeUpdatedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, GetVolumeResource(message.Volume));
        }

        [NonAction]
        public void Handle(VolumeEditedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, GetVolumeResource(message.Volume));
        }

        [NonAction]
        public void Handle(VolumeDeletedEvent message)
        {
            BroadcastResourceChange(ModelAction.Deleted, message.Volume.ToResource());
        }

        [NonAction]
        public void Handle(VolumeRenamedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, message.Volume.Id);
        }

        [NonAction]
        public void Handle(MediaCoversUpdatedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, GetVolumeResource(message.Volume));
        }
    }
}
