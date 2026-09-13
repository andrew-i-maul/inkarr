using System.Collections.Generic;
using NzbDrone.Core.Qualities;

namespace Inkarr.Api.V1.IssueFiles
{
    public class IssueFileListResource
    {
        public List<int> IssueFileIds { get; set; }
        public QualityModel Quality { get; set; }
    }
}
