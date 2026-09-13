using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Inkarr.Http;
using Inkarr.Http.REST;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Http.REST.Attributes;
using NzbDrone.SignalR;
using BadRequestException = NzbDrone.Core.Exceptions.BadRequestException;
using HttpStatusCode = System.Net.HttpStatusCode;

namespace Inkarr.Api.V1.IssueFiles
{
    [V1ApiController]
    public class IssueFileController : RestControllerWithSignalR<IssueFileResource, IssueFile>,
                                 IHandle<IssueFileAddedEvent>,
                                 IHandle<IssueFileDeletedEvent>
    {
        private readonly IMediaFileService _mediaFileService;
        private readonly IDeleteMediaFiles _mediaFileDeletionService;
        private readonly IMetadataTagService _metadataTagService;
        private readonly IVolumeService _volumeService;
        private readonly IIssueService _issueService;
        private readonly IUpgradableSpecification _upgradableSpecification;

        public IssueFileController(IBroadcastSignalRMessage signalRBroadcaster,
                               IMediaFileService mediaFileService,
                               IDeleteMediaFiles mediaFileDeletionService,
                               IMetadataTagService metadataTagService,
                               IVolumeService volumeService,
                               IIssueService issueService,
                               IUpgradableSpecification upgradableSpecification)
            : base(signalRBroadcaster)
        {
            _mediaFileService = mediaFileService;
            _mediaFileDeletionService = mediaFileDeletionService;
            _metadataTagService = metadataTagService;
            _volumeService = volumeService;
            _issueService = issueService;
            _upgradableSpecification = upgradableSpecification;
        }

        private IssueFileResource MapToResource(IssueFile issueFile)
        {
            if (issueFile.EditionId > 0 && issueFile.Volume != null && issueFile.Volume.Value != null)
            {
                return issueFile.ToResource(issueFile.Volume.Value, _upgradableSpecification);
            }
            else
            {
                return issueFile.ToResource();
            }
        }

        protected override IssueFileResource GetResourceById(int id)
        {
            var resource = MapToResource(_mediaFileService.Get(id));
            resource.AudioTags = _metadataTagService.ReadTags((FileInfoBase)new FileInfo(resource.Path));
            return resource;
        }

        [HttpGet]
        public List<IssueFileResource> GetIssueFiles(int? volumeId, [FromQuery]List<int> issueFileIds, [FromQuery(Name="issueId")]List<int> issueIds, bool? unmapped)
        {
            if (!volumeId.HasValue && !issueFileIds.Any() && !issueIds.Any() && !unmapped.HasValue)
            {
                throw new BadRequestException("volumeId, issueId, issueFileIds or unmapped must be provided");
            }

            if (unmapped.HasValue && unmapped.Value)
            {
                var files = _mediaFileService.GetUnmappedFiles();
                return files.ConvertAll(f => MapToResource(f));
            }

            if (volumeId.HasValue && !issueIds.Any())
            {
                var volume = _volumeService.GetVolume(volumeId.Value);

                return _mediaFileService.GetFilesByVolume(volumeId.Value).ConvertAll(f => f.ToResource(volume, _upgradableSpecification));
            }

            if (issueIds.Any())
            {
                var result = new List<IssueFileResource>();
                foreach (var issueId in issueIds)
                {
                    var issue = _issueService.GetIssue(issueId);
                    var issueVolume = _volumeService.GetVolume(issue.VolumeId);
                    result.AddRange(_mediaFileService.GetFilesByIssue(issue.Id).ConvertAll(f => f.ToResource(issueVolume, _upgradableSpecification)));
                }

                return result;
            }
            else
            {
                // trackfiles will come back with the volume already populated
                var issueFiles = _mediaFileService.Get(issueFileIds);
                return issueFiles.ConvertAll(e => MapToResource(e));
            }
        }

        [RestPutById]
        public ActionResult<IssueFileResource> SetQuality(IssueFileResource issueFileResource)
        {
            var issueFile = _mediaFileService.Get(issueFileResource.Id);
            issueFile.Quality = issueFileResource.Quality;
            _mediaFileService.Update(issueFile);
            return Accepted(issueFile.Id);
        }

        [HttpPut("editor")]
        public IActionResult SetQuality([FromBody] IssueFileListResource resource)
        {
            var issueFiles = _mediaFileService.Get(resource.IssueFileIds);

            foreach (var issueFile in issueFiles)
            {
                if (resource.Quality != null)
                {
                    issueFile.Quality = resource.Quality;
                }
            }

            _mediaFileService.Update(issueFiles);

            return Accepted(issueFiles.ConvertAll(f => f.ToResource(issueFiles.First().Volume.Value, _upgradableSpecification)));
        }

        [RestDeleteById]
        public void DeleteIssueFile(int id)
        {
            var issueFile = _mediaFileService.Get(id);

            if (issueFile == null)
            {
                throw new NzbDroneClientException(HttpStatusCode.NotFound, "Issue file not found");
            }

            if (issueFile.EditionId > 0 && issueFile.Volume != null && issueFile.Volume.Value != null)
            {
                _mediaFileDeletionService.DeleteTrackFile(issueFile.Volume.Value, issueFile);
            }
            else
            {
                _mediaFileDeletionService.DeleteTrackFile(issueFile, "Unmapped_Files");
            }
        }

        [HttpDelete("bulk")]
        public object DeleteTrackFiles([FromBody] IssueFileListResource resource)
        {
            var issueFiles = _mediaFileService.Get(resource.IssueFileIds);

            foreach (var issueFile in issueFiles)
            {
                if (issueFile.EditionId > 0 && issueFile.Volume != null && issueFile.Volume.Value != null)
                {
                    _mediaFileDeletionService.DeleteTrackFile(issueFile.Volume.Value, issueFile);
                }
                else
                {
                    _mediaFileDeletionService.DeleteTrackFile(issueFile, "Unmapped_Files");
                }
            }

            return new { };
        }

        [NonAction]
        public void Handle(IssueFileAddedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, MapToResource(message.IssueFile));
        }

        [NonAction]
        public void Handle(IssueFileDeletedEvent message)
        {
            BroadcastResourceChange(ModelAction.Deleted, MapToResource(message.IssueFile));
        }
    }
}
