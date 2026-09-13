using System.Collections.Generic;

namespace Inkarr.Api.V1.Issues
{
    public class IssuesMonitoredResource
    {
        public List<int> IssueIds { get; set; }
        public bool Monitored { get; set; }
    }
}
