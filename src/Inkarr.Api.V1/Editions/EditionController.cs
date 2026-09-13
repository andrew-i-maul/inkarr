using System.Collections.Generic;
using Inkarr.Api.V1.Issues;
using Inkarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Issues;

namespace NzbDrone.Api.V1.Editions
{
    [V1ApiController]
    public class EditionController : Controller
    {
        private readonly IEditionService _editionService;

        public EditionController(IEditionService editionService)
        {
            _editionService = editionService;
        }

        [HttpGet]
        public List<EditionResource> GetEditions([FromQuery]List<int> issueId)
        {
            var editions = _editionService.GetEditionsByIssue(issueId);

            return editions.ToResource();
        }
    }
}
