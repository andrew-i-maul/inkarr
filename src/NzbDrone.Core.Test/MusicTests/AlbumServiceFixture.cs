using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MusicTests.IssueRepositoryTests
{
    [TestFixture]
    public class IssueServiceFixture : CoreTest<IssueService>
    {
        private List<Issue> _issues;

        [SetUp]
        public void Setup()
        {
            _issues = new List<Issue>();
            _issues.Add(new Issue
            {
                Title = "ANThology",
                CleanTitle = "anthology",
                VolumeMetadata = new VolumeMetadata
                {
                    Name = "Volume"
                }
            });

            _issues.Add(new Issue
            {
                Title = "+",
                CleanTitle = "",
                VolumeMetadata = new VolumeMetadata
                {
                    Name = "Volume"
                }
            });

            Mocker.GetMock<IIssueRepository>()
                .Setup(s => s.GetIssuesByVolumeMetadataId(It.IsAny<int>()))
                .Returns(_issues);
        }

        private void GivenSimilarIssue()
        {
            _issues.Add(new Issue
            {
                Title = "ANThology2",
                CleanTitle = "anthology2",
                VolumeMetadata = new VolumeMetadata
                {
                    Name = "Volume"
                }
            });
        }

        [TestCase("ANTholog", "ANThology")]
        [TestCase("antholoyg", "ANThology")]
        [TestCase("ANThology CD", "ANThology")]
        [TestCase("ANThology CD xxxx (Remastered) - [Oh please why do they do this?]", "ANThology")]
        [TestCase("+ (Plus) - I feel the need for redundant information in the title field", "+")]
        public void should_find_issue_in_db_by_inexact_title(string title, string expected)
        {
            var issue = Subject.FindByTitleInexact(0, title);

            issue.Should().NotBeNull();
            issue.Title.Should().Be(expected);
        }

        [TestCase("ANTholog")]
        [TestCase("antholoyg")]
        [TestCase("ANThology CD")]
        [TestCase("÷")]
        [TestCase("÷ (Divide)")]
        public void should_not_find_issue_in_db_by_inexact_title_when_two_similar_matches(string title)
        {
            GivenSimilarIssue();
            var issue = Subject.FindByTitleInexact(0, title);

            issue.Should().BeNull();
        }
    }
}
