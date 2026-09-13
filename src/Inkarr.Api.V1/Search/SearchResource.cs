using Inkarr.Api.V1.Issues;
using Inkarr.Api.V1.Volume;
using Inkarr.Http.REST;

namespace Inkarr.Api.V1.Search
{
    public class SearchResource : RestResource
    {
        public string ForeignId { get; set; }
        public VolumeResource Volume { get; set; }
        public IssueResource Issue { get; set; }
    }
}
