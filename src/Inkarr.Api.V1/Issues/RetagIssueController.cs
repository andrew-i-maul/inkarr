using System.Collections.Generic;
using System.Linq;
using Inkarr.Http;
using Inkarr.Http.REST;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaFiles;

namespace Inkarr.Api.V1.Issues
{
    [V1ApiController("retag")]
    public class RetagIssueController : Controller
    {
        private readonly IMetadataTagService _metadataTagService;

        public RetagIssueController(IMetadataTagService metadataTagService)
        {
            _metadataTagService = metadataTagService;
        }

        [HttpGet]
        public List<RetagIssueResource> GetIssues(int? volumeId, int? issueId)
        {
            if (issueId.HasValue)
            {
                return _metadataTagService.GetRetagPreviewsByIssue(issueId.Value).Where(x => x.Changes.Any()).ToResource();
            }
            else if (volumeId.HasValue)
            {
                return _metadataTagService.GetRetagPreviewsByVolume(volumeId.Value).Where(x => x.Changes.Any()).ToResource();
            }
            else
            {
                throw new BadRequestException("One of volumeId or issueId must be specified");
            }
        }
    }
}
