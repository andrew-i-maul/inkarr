using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IssueTests
{
    [TestFixture]
    public class MonitorNewIssueServiceFixture : CoreTest<MonitorNewIssueService>
    {
        private List<Issue> _issues;

        [SetUp]
        public void Setup()
        {
            _issues = Builder<Issue>.CreateListOfSize(4)
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
        }

        [Test]
        public void should_monitor_with_all()
        {
            foreach (var issue in _issues)
            {
                Subject.ShouldMonitorNewIssue(issue, _issues, NewItemMonitorTypes.All).Should().BeTrue();
            }
        }

        [Test]
        public void should_not_monitor_with_none()
        {
            foreach (var issue in _issues)
            {
                Subject.ShouldMonitorNewIssue(issue, _issues, NewItemMonitorTypes.None).Should().BeFalse();
            }
        }

        [Test]
        public void should_only_monitor_new_with_new()
        {
            Subject.ShouldMonitorNewIssue(_issues[0], _issues, NewItemMonitorTypes.New).Should().BeTrue();

            foreach (var issue in _issues.Skip(1))
            {
                Subject.ShouldMonitorNewIssue(issue, _issues, NewItemMonitorTypes.New).Should().BeFalse();
            }
        }
    }
}
