using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.OrganizerTests
{
    [TestFixture]
    [Ignore("Don't use issue folder in inkarr")]
    public class BuildFilePathFixture : CoreTest<FileNameBuilder>
    {
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _namingConfig = NamingConfig.Default;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);
        }

        [Test]
        public void should_clean_issue_folder_when_it_contains_illegal_characters_in_issue_or_volume_title()
        {
            var filename = @"issuefile";
            var expectedPath = @"C:\Test\Fake- The Volume\Fake- The Issue\issuefile.mobi";

            var fakeVolume = Builder<Volume>.CreateNew()
                .With(s => s.Name = "Fake: The Volume")
                .With(s => s.Path = @"C:\Test\Fake- The Volume".AsOsAgnostic())
                .Build();

            var fakeIssue = Builder<Issue>.CreateNew()
                .With(s => s.Title = "Fake: Issue")
                .Build();

            var fakeEdition = Builder<Edition>
                .CreateNew()
                .With(s => s.Title = fakeIssue.Title)
                .With(s => s.Issue = fakeIssue)
                .Build();

            Subject.BuildIssueFilePath(fakeVolume, fakeEdition, filename, ".mobi").Should().Be(expectedPath.AsOsAgnostic());
        }
    }
}
