using System.Collections.Generic;
using NzbDrone.Core.Issues;

namespace Inkarr.Api.V1.Bookshelf
{
    public class BookshelfResource
    {
        public List<BookshelfVolumeResource> Volumes { get; set; }
        public MonitoringOptions MonitoringOptions { get; set; }
        public NewItemMonitorTypes? MonitorNewItems { get; set; }
    }
}
