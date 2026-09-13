using System;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MusicTests
{
    [TestFixture]
    public class ShouldRefreshIssueFixture : TestBase<ShouldRefreshIssue>
    {
        private Issue _issue;

        [SetUp]
        public void Setup()
        {
            _issue = Builder<Issue>.CreateNew()
                                   .With(e => e.ReleaseDate = DateTime.Today.AddDays(-100))
                                   .Build();
        }

        private void GivenIssueLastRefreshedMonthsAgo()
        {
            _issue.LastInfoSync = DateTime.UtcNow.AddDays(-90);
        }

        private void GivenIssueLastRefreshedYesterday()
        {
            _issue.LastInfoSync = DateTime.UtcNow.AddDays(-1);
        }

        private void GivenIssueLastRefreshedRecently()
        {
            _issue.LastInfoSync = DateTime.UtcNow.AddHours(-7);
        }

        private void GivenRecentlyReleased()
        {
            _issue.ReleaseDate = DateTime.Today.AddDays(-7);
        }

        private void GivenFutureRelease()
        {
            _issue.ReleaseDate = DateTime.Today.AddDays(7);
        }

        [Test]
        public void should_return_false_if_issue_last_refreshed_less_than_12_hours_ago()
        {
            GivenIssueLastRefreshedRecently();

            Subject.ShouldRefresh(_issue).Should().BeFalse();
        }

        [Test]
        public void should_return_true_if_issue_last_refreshed_more_than_30_days_ago()
        {
            GivenIssueLastRefreshedMonthsAgo();

            Subject.ShouldRefresh(_issue).Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_issue_released_in_last_30_days()
        {
            GivenIssueLastRefreshedYesterday();

            GivenRecentlyReleased();

            Subject.ShouldRefresh(_issue).Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_issue_releases_in_future()
        {
            GivenIssueLastRefreshedYesterday();

            GivenFutureRelease();

            Subject.ShouldRefresh(_issue).Should().BeTrue();
        }

        [Test]
        public void should_return_false_when_recently_refreshed_issue_released_over_30_days_ago()
        {
            GivenIssueLastRefreshedYesterday();

            Subject.ShouldRefresh(_issue).Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_recently_refreshed_issue_released_in_last_30_days()
        {
            GivenIssueLastRefreshedRecently();

            GivenRecentlyReleased();

            Subject.ShouldRefresh(_issue).Should().BeFalse();
        }
    }
}
