using System.Collections.Generic;
using Inkarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Issues;

namespace Inkarr.Api.V1.Series
{
    [V1ApiController]
    public class SeriesController : Controller
    {
        protected readonly ISeriesService _seriesService;

        public SeriesController(ISeriesService seriesService)
        {
            _seriesService = seriesService;
        }

        [HttpGet]
        public List<SeriesResource> GetSeries(int volumeId)
        {
            return _seriesService.GetByVolumeId(volumeId).ToResource();
        }
    }
}
