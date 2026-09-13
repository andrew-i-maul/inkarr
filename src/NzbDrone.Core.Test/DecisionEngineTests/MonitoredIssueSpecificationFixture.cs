using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine.Specifications.RssSync;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]

    public class MonitoredIssueSpecificationFixture : CoreTest<MonitoredIssueSpecification>
    {
        private MonitoredIssueSpecification _monitoredIssueSpecification;

        private RemoteIssue _parseResultMulti;
        private RemoteIssue _parseResultSingle;
        private Volume _fakeVolume;
        private Issue _firstIssue;
        private Issue _secondIssue;

        [SetUp]
        public void Setup()
        {
            _monitoredIssueSpecification = Mocker.Resolve<MonitoredIssueSpecification>();

            _fakeVolume = Builder<Volume>.CreateNew()
                .With(c => c.Monitored = true)
                .Build();

            _firstIssue = new Issue { Monitored = true };
            _secondIssue = new Issue { Monitored = true };

            var singleIssueList = new List<Issue> { _firstIssue };
            var doubleIssueList = new List<Issue> { _firstIssue, _secondIssue };

            _parseResultMulti = new RemoteIssue
            {
                Volume = _fakeVolume,
                Issues = doubleIssueList
            };

            _parseResultSingle = new RemoteIssue
            {
                Volume = _fakeVolume,
                Issues = singleIssueList
            };
        }

        private void WithFirstIssueUnmonitored()
        {
            _firstIssue.Monitored = false;
        }

        private void WithSecondIssueUnmonitored()
        {
            _secondIssue.Monitored = false;
        }

        [Test]
        public void setup_should_return_monitored_issue_should_return_true()
        {
            _monitoredIssueSpecification.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
            _monitoredIssueSpecification.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void not_monitored_volume_should_be_skipped()
        {
            _fakeVolume.Monitored = false;
            _monitoredIssueSpecification.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void only_issue_not_monitored_should_return_false()
        {
            WithFirstIssueUnmonitored();
            _monitoredIssueSpecification.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void both_issues_not_monitored_should_return_false()
        {
            WithFirstIssueUnmonitored();
            WithSecondIssueUnmonitored();
            _monitoredIssueSpecification.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void only_first_issue_not_monitored_should_return_false()
        {
            WithFirstIssueUnmonitored();
            _monitoredIssueSpecification.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void only_second_issue_not_monitored_should_return_false()
        {
            WithSecondIssueUnmonitored();
            _monitoredIssueSpecification.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_for_single_issue_search()
        {
            _fakeVolume.Monitored = false;
            _monitoredIssueSpecification.IsSatisfiedBy(_parseResultSingle, new IssueSearchCriteria()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_issue_is_not_monitored_and_monitoredEpisodesOnly_flag_is_false()
        {
            WithFirstIssueUnmonitored();
            _monitoredIssueSpecification.IsSatisfiedBy(_parseResultSingle, new IssueSearchCriteria { MonitoredIssuesOnly = false }).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_issue_is_not_monitored_and_monitoredEpisodesOnly_flag_is_true()
        {
            WithFirstIssueUnmonitored();
            _monitoredIssueSpecification.IsSatisfiedBy(_parseResultSingle, new IssueSearchCriteria { MonitoredIssuesOnly = true }).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_all_issues_are_not_monitored_for_discography_pack_release()
        {
            WithSecondIssueUnmonitored();
            _parseResultMulti.ParsedIssueInfo = new ParsedIssueInfo()
            {
                Discography = true
            };

            _monitoredIssueSpecification.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeFalse();
        }
    }
}
