using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MusicTests.IssueMonitoredServiceTests
{
    [TestFixture]
    public class SetIssueMontitoredFixture : CoreTest<IssueMonitoredService>
    {
        private Volume _volume;
        private List<Issue> _issues;

        [SetUp]
        public void Setup()
        {
            const int issues = 4;

            _volume = Builder<Volume>.CreateNew()
                                     .Build();

            _issues = Builder<Issue>.CreateListOfSize(issues)
                                        .All()
                                        .With(e => e.Monitored = true)
                                        .With(e => e.ReleaseDate = DateTime.UtcNow.AddDays(-7))

                                        //Future
                                        .TheFirst(1)
                                        .With(e => e.ReleaseDate = DateTime.UtcNow.AddDays(7))

                                        //Future/TBA
                                        .TheNext(1)
                                        .With(e => e.ReleaseDate = null)
                                        .Build()
                                        .ToList();

            Mocker.GetMock<IIssueService>()
                  .Setup(s => s.GetIssuesByVolume(It.IsAny<int>()))
                  .Returns(_issues);

            Mocker.GetMock<IIssueService>()
                .Setup(s => s.GetVolumeIssuesWithFiles(It.IsAny<Volume>()))
                .Returns(new List<Issue>());
        }

        [Test]
        public void should_be_able_to_monitor_volume_without_changing_issues()
        {
            Subject.SetIssueMonitoredStatus(_volume, null);

            Mocker.GetMock<IVolumeService>()
                  .Verify(v => v.UpdateVolume(It.IsAny<Volume>()), Times.Once());

            Mocker.GetMock<IIssueService>()
                  .Verify(v => v.UpdateMany(It.IsAny<List<Issue>>()), Times.Never());
        }

        [Test]
        public void should_be_able_to_monitor_issues_when_passed_in_volume()
        {
            var issuesToMonitor = new List<string> { _issues.First().ForeignIssueId };

            Subject.SetIssueMonitoredStatus(_volume, new MonitoringOptions { Monitored = true, IssuesToMonitor = issuesToMonitor });

            Mocker.GetMock<IVolumeService>()
                .Verify(v => v.UpdateVolume(It.IsAny<Volume>()), Times.Once());

            VerifyMonitored(e => e.ForeignIssueId == _issues.First().ForeignIssueId);
            VerifyNotMonitored(e => e.ForeignIssueId != _issues.First().ForeignIssueId);
        }

        [Test]
        public void should_be_able_to_monitor_all_issues()
        {
            Subject.SetIssueMonitoredStatus(_volume, new MonitoringOptions { Monitor = MonitorTypes.All });

            Mocker.GetMock<IIssueService>()
                  .Verify(v => v.UpdateIssue(It.Is<Issue>(l => l.Monitored)), Times.Exactly(_issues.Count));
        }

        [Test]
        public void should_be_able_to_monitor_new_issues_only()
        {
            var monitoringOptions = new MonitoringOptions
            {
                Monitor = MonitorTypes.Future
            };

            Subject.SetIssueMonitoredStatus(_volume, monitoringOptions);

            VerifyMonitored(e => e.ReleaseDate.HasValue && e.ReleaseDate.Value.After(DateTime.UtcNow));
            VerifyMonitored(e => !e.ReleaseDate.HasValue);
            VerifyNotMonitored(e => e.ReleaseDate.HasValue && e.ReleaseDate.Value.Before(DateTime.UtcNow));
        }

        private void VerifyMonitored(Func<Issue, bool> predicate)
        {
            Mocker.GetMock<IIssueService>()
                .Verify(v => v.UpdateIssue(It.Is<Issue>(b => b.Monitored)), Times.AtLeast(_issues.Where(predicate).Count()));
        }

        private void VerifyNotMonitored(Func<Issue, bool> predicate)
        {
            Mocker.GetMock<IIssueService>()
                .Verify(v => v.UpdateIssue(It.Is<Issue>(b => !b.Monitored)), Times.AtLeast(_issues.Where(predicate).Count()));
        }
    }
}
