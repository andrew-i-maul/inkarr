using System.Collections.Generic;
using NzbDrone.Core.Issues;

namespace Inkarr.Api.V1.Issueshelf
{
    public class IssueshelfResource
    {
        public List<IssueshelfVolumeResource> Volumes { get; set; }
        public MonitoringOptions MonitoringOptions { get; set; }
        public NewItemMonitorTypes? MonitorNewItems { get; set; }
    }
}
