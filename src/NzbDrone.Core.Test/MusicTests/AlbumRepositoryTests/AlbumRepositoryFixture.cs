using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using FluentAssertions.Equivalency;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MusicTests.IssueRepositoryTests
{
    [TestFixture]
    public class IssueRepositoryFixture : DbTest<IssueService, Issue>
    {
        private Volume _volume;
        private Issue _issue;
        private Issue _issueSpecial;
        private List<Issue> _issues;
        private IssueRepository _issueRepo;

        [SetUp]
        public void Setup()
        {
            AssertionOptions.AssertEquivalencyUsing(options =>
            {
                options.Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation.ToUniversalTime())).WhenTypeIs<DateTime>();
                options.Using<DateTime?>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation.Value.ToUniversalTime())).WhenTypeIs<DateTime?>();
                return options;
            });

            _volume = new Volume
            {
                Name = "Alien Ant Farm",
                Monitored = true,
                ForeignVolumeId = "this is a fake id",
                Id = 1,
                VolumeMetadataId = 1
            };

            _issueRepo = Mocker.Resolve<IssueRepository>();

            _issue = new Issue
            {
                Title = "ANThology",
                ForeignIssueId = "1",
                TitleSlug = "1-ANThology",
                CleanTitle = "anthology",
                Volume = _volume,
                VolumeMetadataId = _volume.VolumeMetadataId,
            };

            _issueRepo.Insert(_issue);
            _issueRepo.Update(_issue);

            _issueSpecial = new Issue
            {
                Title = "+",
                ForeignIssueId = "2",
                TitleSlug = "2-_",
                CleanTitle = "",
                Volume = _volume,
                VolumeMetadataId = _volume.VolumeMetadataId
            };

            _issueRepo.Insert(_issueSpecial);
        }

        [TestCase("ANThology")]
        [TestCase("anthology")]
        [TestCase("anthology!")]
        public void should_find_issue_in_db_by_title(string title)
        {
            var issue = _issueRepo.FindByTitle(_volume.VolumeMetadataId, title);

            issue.Should().NotBeNull();
            issue.Title.Should().Be(_issue.Title);
        }

        [Test]
        public void should_find_issue_in_db_by_title_all_special_characters()
        {
            var issue = _issueRepo.FindByTitle(_volume.VolumeMetadataId, "+");

            issue.Should().NotBeNull();
            issue.Title.Should().Be(_issueSpecial.Title);
        }

        [TestCase("ANTholog")]
        [TestCase("nthology")]
        [TestCase("antholoyg")]
        [TestCase("÷")]
        public void should_not_find_issue_in_db_by_incorrect_title(string title)
        {
            var issue = _issueRepo.FindByTitle(_volume.VolumeMetadataId, title);

            issue.Should().BeNull();
        }

        [Test]
        public void should_not_find_issue_when_two_issues_have_same_name()
        {
            var issues = Builder<Issue>.CreateListOfSize(2)
                .All()
                .With(x => x.Id = 0)
                .With(x => x.Volume = _volume)
                .With(x => x.VolumeMetadataId = _volume.VolumeMetadataId)
                .With(x => x.Title = "Weezer")
                .With(x => x.CleanTitle = "weezer")
                .Build();

            _issueRepo.InsertMany(issues);

            var issue = _issueRepo.FindByTitle(_volume.VolumeMetadataId, "Weezer");

            _issueRepo.All().Should().HaveCount(4);
            issue.Should().BeNull();
        }

        private void GivenMultipleIssues()
        {
            _issues = Builder<Issue>.CreateListOfSize(4)
                .All()
                .With(x => x.Id = 0)
                .With(x => x.Volume = _volume)
                .With(x => x.VolumeMetadataId = _volume.VolumeMetadataId)
                .TheFirst(1)

                // next
                .With(x => x.ReleaseDate = DateTime.UtcNow.AddDays(1))
                .TheNext(1)

                // another future one
                .With(x => x.ReleaseDate = DateTime.UtcNow.AddDays(2))
                .TheNext(1)

                // most recent
                .With(x => x.ReleaseDate = DateTime.UtcNow.AddDays(-1))
                .TheNext(1)

                // an older one
                .With(x => x.ReleaseDate = DateTime.UtcNow.AddDays(-2))
                .BuildList();

            _issueRepo.InsertMany(_issues);
        }

        [Test]
        public void get_next_issues_should_return_next_issue()
        {
            GivenMultipleIssues();

            var result = _issueRepo.GetNextIssues(new[] { _volume.VolumeMetadataId });
            result.Should().BeEquivalentTo(_issues.Take(1), IssueComparerOptions);
        }

        [Test]
        public void get_last_issues_should_return_next_issue()
        {
            GivenMultipleIssues();

            var result = _issueRepo.GetLastIssues(new[] { _volume.VolumeMetadataId });
            result.Should().BeEquivalentTo(_issues.Skip(2).Take(1), IssueComparerOptions);
        }

        private EquivalencyAssertionOptions<Issue> IssueComparerOptions(EquivalencyAssertionOptions<Issue> opts) => opts.ComparingByMembers<Issue>()
                .Excluding(ctx => ctx.SelectedMemberInfo.MemberType.IsGenericType && ctx.SelectedMemberInfo.MemberType.GetGenericTypeDefinition() == typeof(LazyLoaded<>))
                .Excluding(x => x.VolumeId)
                .Excluding(x => x.ForeignEditionId);
    }
}
