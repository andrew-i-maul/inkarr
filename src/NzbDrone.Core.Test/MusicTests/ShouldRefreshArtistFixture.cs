using System;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MusicTests
{
    [TestFixture]
    public class ShouldRefreshVolumeFixture : TestBase<ShouldRefreshVolume>
    {
        private Volume _volume;

        [SetUp]
        public void Setup()
        {
            _volume = Builder<Volume>.CreateNew()
                                     .With(v => v.Metadata.Value.Status == VolumeStatusType.Continuing)
                                     .Build();

            Mocker.GetMock<IIssueService>()
                  .Setup(s => s.GetIssuesByVolume(_volume.Id))
                  .Returns(Builder<Issue>.CreateListOfSize(2)
                                           .All()
                                           .With(e => e.ReleaseDate = DateTime.Today.AddDays(-100))
                                           .Build()
                                           .ToList());
        }

        private void GivenVolumeIsEnded()
        {
            _volume.Metadata.Value.Status = VolumeStatusType.Ended;
        }

        private void GivenVolumeLastRefreshedMonthsAgo()
        {
            _volume.LastInfoSync = DateTime.UtcNow.AddDays(-90);
        }

        private void GivenVolumeLastRefreshedYesterday()
        {
            _volume.LastInfoSync = DateTime.UtcNow.AddDays(-1);
        }

        private void GivenVolumeLastRefreshedThreeDaysAgo()
        {
            _volume.LastInfoSync = DateTime.UtcNow.AddDays(-3);
        }

        private void GivenVolumeLastRefreshedRecently()
        {
            _volume.LastInfoSync = DateTime.UtcNow.AddHours(-7);
        }

        private void GivenRecentlyAired()
        {
            Mocker.GetMock<IIssueService>()
                              .Setup(s => s.GetIssuesByVolume(_volume.Id))
                              .Returns(Builder<Issue>.CreateListOfSize(2)
                                                       .TheFirst(1)
                                                       .With(e => e.ReleaseDate = DateTime.Today.AddDays(-7))
                                                       .TheLast(1)
                                                       .With(e => e.ReleaseDate = DateTime.Today.AddDays(-100))
                                                       .Build()
                                                       .ToList());
        }

        [Test]
        public void should_return_true_if_running_volume_last_refreshed_more_than_24_hours_ago()
        {
            GivenVolumeLastRefreshedThreeDaysAgo();

            Subject.ShouldRefresh(_volume).Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_running_volume_last_refreshed_less_than_12_hours_ago()
        {
            GivenVolumeLastRefreshedRecently();

            Subject.ShouldRefresh(_volume).Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_ended_volume_last_refreshed_yesterday()
        {
            GivenVolumeIsEnded();
            GivenVolumeLastRefreshedYesterday();

            Subject.ShouldRefresh(_volume).Should().BeFalse();
        }

        [Test]
        public void should_return_true_if_volume_last_refreshed_more_than_30_days_ago()
        {
            GivenVolumeIsEnded();
            GivenVolumeLastRefreshedMonthsAgo();

            Subject.ShouldRefresh(_volume).Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_issue_released_in_last_30_days()
        {
            GivenVolumeIsEnded();
            GivenVolumeLastRefreshedYesterday();

            GivenRecentlyAired();

            Subject.ShouldRefresh(_volume).Should().BeTrue();
        }

        [Test]
        public void should_return_false_when_recently_refreshed_ended_show_has_not_aired_for_30_days()
        {
            GivenVolumeIsEnded();
            GivenVolumeLastRefreshedYesterday();

            Subject.ShouldRefresh(_volume).Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_recently_refreshed_ended_show_aired_in_last_30_days()
        {
            GivenVolumeIsEnded();
            GivenVolumeLastRefreshedRecently();

            GivenRecentlyAired();

            Subject.ShouldRefresh(_volume).Should().BeFalse();
        }
    }
}
