using System.Collections.Generic;
using System.Linq;
using Inkarr.Http;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.IssueImport.Manual;
using NzbDrone.Core.Qualities;

namespace Inkarr.Api.V1.ManualImport
{
    [V1ApiController]
    public class ManualImportController : Controller
    {
        private readonly IVolumeService _volumeService;
        private readonly IIssueService _issueService;
        private readonly IEditionService _editionService;
        private readonly IManualImportService _manualImportService;
        private readonly Logger _logger;

        public ManualImportController(IManualImportService manualImportService,
                                  IVolumeService volumeService,
                                  IEditionService editionService,
                                  IIssueService issueService,
                                  Logger logger)
        {
            _volumeService = volumeService;
            _issueService = issueService;
            _editionService = editionService;
            _manualImportService = manualImportService;
            _logger = logger;
        }

        [HttpPost]
        public IActionResult UpdateItems(List<ManualImportUpdateResource> resource)
        {
            return Accepted(UpdateImportItems(resource));
        }

        [HttpGet]
        public List<ManualImportResource> GetMediaFiles(string folder, string downloadId, int? volumeId, bool filterExistingFiles = true, bool replaceExistingFiles = true)
        {
            NzbDrone.Core.Issues.Volume volume = null;

            if (volumeId > 0)
            {
                volume = _volumeService.GetVolume(volumeId.Value);
            }

            var filter = filterExistingFiles ? FilterFilesType.Matched : FilterFilesType.None;

            return _manualImportService.GetMediaFiles(folder, downloadId, volume, filter, replaceExistingFiles).ToResource().Select(AddQualityWeight).ToList();
        }

        private ManualImportResource AddQualityWeight(ManualImportResource item)
        {
            if (item.Quality != null)
            {
                item.QualityWeight = Quality.DefaultQualityDefinitions.Single(q => q.Quality == item.Quality.Quality).Weight;
                item.QualityWeight += item.Quality.Revision.Real * 10;
                item.QualityWeight += item.Quality.Revision.Version;
            }

            return item;
        }

        private List<ManualImportResource> UpdateImportItems(List<ManualImportUpdateResource> resources)
        {
            var items = new List<ManualImportItem>();
            foreach (var resource in resources)
            {
                items.Add(new ManualImportItem
                {
                    Id = resource.Id,
                    Path = resource.Path,
                    Name = resource.Name,
                    Volume = resource.VolumeId.HasValue ? _volumeService.GetVolume(resource.VolumeId.Value) : null,
                    Issue = resource.IssueId.HasValue ? _issueService.GetIssue(resource.IssueId.Value) : null,
                    Edition = resource.ForeignEditionId == null ? null : _editionService.GetEditionByForeignEditionId(resource.ForeignEditionId),
                    Quality = resource.Quality,
                    ReleaseGroup = resource.ReleaseGroup,
                    IndexerFlags = resource.IndexerFlags,
                    DownloadId = resource.DownloadId,
                    AdditionalFile = resource.AdditionalFile,
                    ReplaceExistingFiles = resource.ReplaceExistingFiles,
                    DisableReleaseSwitching = resource.DisableReleaseSwitching
                });
            }

            return _manualImportService.UpdateItems(items).Select(x => x.ToResource()).ToList();
        }
    }
}
