using System.Collections.Generic;
using System.Linq;
using Inkarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Commands;
using NzbDrone.Core.Messaging.Commands;

namespace Inkarr.Api.V1.Volume
{
    [V1ApiController("volume/editor")]
    public class VolumeEditorController : Controller
    {
        private readonly IVolumeService _volumeService;
        private readonly IManageCommandQueue _commandQueueManager;

        public VolumeEditorController(IVolumeService volumeService, IManageCommandQueue commandQueueManager)
        {
            _volumeService = volumeService;
            _commandQueueManager = commandQueueManager;
        }

        [HttpPut]
        public IActionResult SaveAll([FromBody] VolumeEditorResource resource)
        {
            var volumesToUpdate = _volumeService.GetVolumes(resource.VolumeIds);
            var volumesToMove = new List<BulkMoveVolume>();

            foreach (var volume in volumesToUpdate)
            {
                if (resource.Monitored.HasValue)
                {
                    volume.Monitored = resource.Monitored.Value;
                }

                if (resource.MonitorNewItems.HasValue)
                {
                    volume.MonitorNewItems = resource.MonitorNewItems.Value;
                }

                if (resource.QualityProfileId.HasValue)
                {
                    volume.QualityProfileId = resource.QualityProfileId.Value;
                }

                if (resource.MetadataProfileId.HasValue)
                {
                    volume.MetadataProfileId = resource.MetadataProfileId.Value;
                }

                if (resource.RootFolderPath.IsNotNullOrWhiteSpace())
                {
                    volume.RootFolderPath = resource.RootFolderPath;
                    volumesToMove.Add(new BulkMoveVolume
                    {
                        VolumeId = volume.Id,
                        SourcePath = volume.Path
                    });
                }

                if (resource.Tags != null)
                {
                    var newTags = resource.Tags;
                    var applyTags = resource.ApplyTags;

                    switch (applyTags)
                    {
                        case ApplyTags.Add:
                            newTags.ForEach(t => volume.Tags.Add(t));
                            break;
                        case ApplyTags.Remove:
                            newTags.ForEach(t => volume.Tags.Remove(t));
                            break;
                        case ApplyTags.Replace:
                            volume.Tags = new HashSet<int>(newTags);
                            break;
                    }
                }
            }

            if (resource.MoveFiles && volumesToMove.Any())
            {
                _commandQueueManager.Push(new BulkMoveVolumeCommand
                {
                    DestinationRootFolder = resource.RootFolderPath,
                    Volume = volumesToMove
                });
            }

            return Accepted(_volumeService.UpdateVolumes(volumesToUpdate, !resource.MoveFiles).ToResource());
        }

        [HttpDelete]
        public object DeleteVolume([FromBody] VolumeEditorResource resource)
        {
            foreach (var volumeId in resource.VolumeIds)
            {
                _volumeService.DeleteVolume(volumeId, false);
            }

            return new { };
        }
    }
}
