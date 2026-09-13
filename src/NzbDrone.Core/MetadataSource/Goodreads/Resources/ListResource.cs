using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;

namespace NzbDrone.Core.MetadataSource.Goodreads
{
    /// <summary>
    /// Represents information about a issue series as defined by the Goodreads API.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public sealed class ListResource : GoodreadsResource
    {
        public override string ElementName => "list";

        public int Page { get; private set; }

        public int PerPage { get; private set; }

        public int ListIssuesCount { get; private set; }

        public List<IssueResource> Issues { get; set; }

        public override void Parse(XElement element)
        {
            Page = element.ElementAsInt("page");
            PerPage = element.ElementAsInt("per_page");
            ListIssuesCount = element.ElementAsInt("total_issues");

            Issues = element.ParseChildren<IssueResource>("issues", "issue") ?? new List<IssueResource>();
        }
    }
}
