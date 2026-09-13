using System.Linq;
using Inkarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Issues;

namespace Inkarr.Api.V1.Issueshelf
{
    [V1ApiController]
    public class IssueshelfController : Controller
    {
        private readonly IVolumeService _volumeService;
        private readonly IIssueMonitoredService _issueMonitoredService;

        public IssueshelfController(IVolumeService volumeService, IIssueMonitoredService issueMonitoredService)
        {
            _volumeService = volumeService;
            _issueMonitoredService = issueMonitoredService;
        }

        [HttpPost]
        public IActionResult UpdateAll([FromBody] IssueshelfResource request)
        {
            //Read from request
            var volumeToUpdate = _volumeService.GetVolumes(request.Volumes.Select(s => s.Id));

            foreach (var s in request.Volumes)
            {
                var volume = volumeToUpdate.Single(c => c.Id == s.Id);

                if (s.Monitored.HasValue)
                {
                    volume.Monitored = s.Monitored.Value;
                }

                if (request.MonitoringOptions != null && request.MonitoringOptions.Monitor == MonitorTypes.None)
                {
                    volume.Monitored = false;
                }

                if (request.MonitorNewItems.HasValue)
                {
                    volume.MonitorNewItems = request.MonitorNewItems.Value;
                }

                _issueMonitoredService.SetIssueMonitoredStatus(volume, request.MonitoringOptions);
            }

            return Accepted(request);
        }
    }
}
