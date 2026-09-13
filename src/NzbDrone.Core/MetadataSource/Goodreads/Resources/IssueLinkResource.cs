using System.Diagnostics;
using System.Xml.Linq;

namespace NzbDrone.Core.MetadataSource.Goodreads
{
    /// <summary>
    /// This class models a issue link as defined by the Goodreads API.
    /// This is usually a link to a third-party site to purchase the issue.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public sealed class IssueLinkResource : GoodreadsResource
    {
        public override string ElementName => "issue_link";

        /// <summary>
        /// The Id of this issue link.
        /// </summary>
        public long Id { get; private set; }

        /// <summary>
        /// The name of this issue link provider.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The link to this issue on the provider's site.
        /// Be sure to append issue_id as a query parameter
        /// to actually be redirected to the correct page.
        /// </summary>
        public string Link { get; private set; }

        public override void Parse(XElement element)
        {
            Id = element.ElementAsLong("id");
            Name = element.ElementAsString("name");
            Link = element.ElementAsString("link");
        }

        /// <summary>
        /// Goodreads returns incomplete issue links for some reason.
        /// The link results in an error unless you append a issue_id query parameter.
        /// This method fixes up these issue links with the given issue id.
        /// </summary>
        /// <param name="issueId">The issue id to append to the issue link.</param>
        internal void FixIssueLink(long issueId)
        {
            if (!string.IsNullOrWhiteSpace(Link))
            {
                if (!Link.Contains("issue_id"))
                {
                    Link += (Link.Contains("?") ? "&" : "?") + "issue_id=" + issueId;
                }
            }
        }
    }
}
