using System.Linq;
using NzbDrone.Core.Indexers.Newznab;

namespace NzbDrone.Core.Indexers.Torznab
{
    public class TorznabRequestGenerator : NewznabRequestGenerator
    {
        public TorznabRequestGenerator(INewznabCapabilitiesProvider capabilitiesProvider)
        : base(capabilitiesProvider)
        {
        }

        protected override bool SupportsIssueSearch
        {
            get
            {
                var capabilities = _capabilitiesProvider.GetCapabilities(Settings);

                return capabilities.SupportedIssueSearchParameters != null &&
                       capabilities.SupportedIssueSearchParameters.Contains("q") &&
                       capabilities.SupportedIssueSearchParameters.Contains("volume") &&
                       capabilities.SupportedIssueSearchParameters.Contains("title");
            }
        }
    }
}
