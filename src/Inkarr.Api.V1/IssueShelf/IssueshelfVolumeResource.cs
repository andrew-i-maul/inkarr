using System.Collections.Generic;
using Inkarr.Api.V1.Issues;

namespace Inkarr.Api.V1.Issueshelf
{
    public class IssueshelfVolumeResource
    {
        public int Id { get; set; }
        public bool? Monitored { get; set; }
        public List<IssueResource> Issues { get; set; }
    }
}
