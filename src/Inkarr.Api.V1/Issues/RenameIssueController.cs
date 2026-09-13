using System.Collections.Generic;
using Inkarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaFiles;

namespace Inkarr.Api.V1.Issues
{
    [V1ApiController("rename")]
    public class RenameIssueController : Controller
    {
        private readonly IRenameIssueFileService _renameIssueFileService;

        public RenameIssueController(IRenameIssueFileService renameIssueFileService)
        {
            _renameIssueFileService = renameIssueFileService;
        }

        [HttpGet]
        public List<RenameIssueResource> GetIssueFiles(int volumeId, int? issueId)
        {
            if (issueId.HasValue)
            {
                return _renameIssueFileService.GetRenamePreviews(volumeId, issueId.Value).ToResource();
            }

            return _renameIssueFileService.GetRenamePreviews(volumeId).ToResource();
        }
    }
}
