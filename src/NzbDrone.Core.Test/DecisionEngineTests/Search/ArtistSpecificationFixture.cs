using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine.Specifications.Search;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.DecisionEngineTests.Search
{
    [TestFixture]
    public class VolumeSpecificationFixture : TestBase<VolumeSpecification>
    {
        private Volume _volume1;
        private Volume _volume2;
        private RemoteIssue _remoteIssue = new RemoteIssue();
        private SearchCriteriaBase _searchCriteria = new IssueSearchCriteria();

        [SetUp]
        public void Setup()
        {
            _volume1 = Builder<Volume>.CreateNew().With(s => s.Id = 1).Build();
            _volume2 = Builder<Volume>.CreateNew().With(s => s.Id = 2).Build();

            _remoteIssue.Volume = _volume1;
        }

        [Test]
        public void should_return_false_if_volume_doesnt_match()
        {
            _searchCriteria.Volume = _volume2;

            Subject.IsSatisfiedBy(_remoteIssue, _searchCriteria).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_when_volume_ids_match()
        {
            _searchCriteria.Volume = _volume1;

            Subject.IsSatisfiedBy(_remoteIssue, _searchCriteria).Accepted.Should().BeTrue();
        }
    }
}
