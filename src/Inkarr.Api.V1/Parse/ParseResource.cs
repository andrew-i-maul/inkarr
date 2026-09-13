using System.Collections.Generic;
using Inkarr.Api.V1.Issues;
using Inkarr.Api.V1.Volume;
using Inkarr.Http.REST;
using NzbDrone.Core.Parser.Model;

namespace Inkarr.Api.V1.Parse
{
    public class ParseResource : RestResource
    {
        public string Title { get; set; }
        public ParsedIssueInfo ParsedIssueInfo { get; set; }
        public VolumeResource Volume { get; set; }
        public List<IssueResource> Issues { get; set; }
    }
}
