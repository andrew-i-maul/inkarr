using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.Search
{
    public class VolumeSpecification : IDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public VolumeSpecification(Logger logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public Decision IsSatisfiedBy(RemoteIssue remoteIssue, SearchCriteriaBase searchCriteria)
        {
            if (searchCriteria == null)
            {
                return Decision.Accept();
            }

            _logger.Debug("Checking if volume matches searched volume");

            if (remoteIssue.Volume.Id != searchCriteria.Volume.Id)
            {
                _logger.Debug("Volume {0} does not match {1}", remoteIssue.Volume, searchCriteria.Volume);
                return Decision.Reject("Wrong volume");
            }

            return Decision.Accept();
        }
    }
}
