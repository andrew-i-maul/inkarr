using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.OrganizerTests.FileNameBuilderTests
{
    [TestFixture]
    public class ColonReplacementFixture : CoreTest<FileNameBuilder>
    {
        private Volume _volume;
        private Issue _issue;
        private Edition _edition;
        private IssueFile _issueFile;
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _volume = Builder<Volume>
                .CreateNew()
                .With(s => s.Name = "Christopher Hopper")
                .Build();

            var series = Builder<Series>
                .CreateNew()
                .With(x => x.Title = "Series: Ruins of the Earth")
                .Build();

            var seriesLink = Builder<SeriesIssueLink>
                .CreateListOfSize(1)
                .All()
                .With(s => s.Position = "1-2")
                .With(s => s.Series = series)
                .BuildListOfNew();

            _issue = Builder<Issue>
                .CreateNew()
                .With(s => s.Title = "Fake: Phantom Deadfall")
                .With(s => s.VolumeMetadata = _volume.Metadata.Value)
                .With(s => s.ReleaseDate = new DateTime(2021, 2, 14))
                .With(s => s.SeriesLinks = seriesLink)
                .Build();

            _edition = Builder<Edition>
                .CreateNew()
                .With(s => s.Monitored = true)
                .With(s => s.Issue = _issue)
                .With(s => s.Title = _issue.Title)
                .With(s => s.ReleaseDate = new DateTime(2021, 2, 17))
                .Build();

            _issueFile = new IssueFile { Quality = new QualityModel(Quality.EPUB), ReleaseGroup = "InkarrTest" };

            _namingConfig = NamingConfig.Default;
            _namingConfig.RenameIssues = true;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);

            Mocker.GetMock<IQualityDefinitionService>()
                .Setup(v => v.Get(Moq.It.IsAny<Quality>()))
                .Returns<Quality>(v => Quality.DefaultQualityDefinitions.First(c => c.Quality == v));

            Mocker.GetMock<ICustomFormatService>()
                  .Setup(v => v.All())
                  .Returns(new List<CustomFormat>());
        }

        [Test]
        public void should_replace_colon_followed_by_space_with_space_dash_space_by_default()
        {
            _namingConfig.StandardIssueFormat = "{Volume Name} - {Issue SeriesTitle - }{Issue Title} {(Release Year)}";

            Subject.BuildIssueFileName(_volume, _edition, _issueFile)
                   .Should().Be("Christopher Hopper - Series - Ruins of the Earth #1-2 - Fake - Phantom Deadfall (2021)");
        }

        [TestCase("Fake: Phantom Deadfall", ColonReplacementFormat.Smart, "Christopher Hopper - Series - Ruins of the Earth - Fake - Phantom Deadfall (2021)")]
        [TestCase("Fake: Phantom Deadfall", ColonReplacementFormat.Dash, "Christopher Hopper - Series- Ruins of the Earth - Fake- Phantom Deadfall (2021)")]
        [TestCase("Fake: Phantom Deadfall", ColonReplacementFormat.Delete, "Christopher Hopper - Series Ruins of the Earth - Fake Phantom Deadfall (2021)")]
        [TestCase("Fake: Phantom Deadfall", ColonReplacementFormat.SpaceDash, "Christopher Hopper - Series - Ruins of the Earth - Fake - Phantom Deadfall (2021)")]
        [TestCase("Fake: Phantom Deadfall", ColonReplacementFormat.SpaceDashSpace, "Christopher Hopper - Series - Ruins of the Earth - Fake - Phantom Deadfall (2021)")]
        public void should_replace_colon_followed_by_space_with_expected_result(string issueTitle, ColonReplacementFormat replacementFormat, string expected)
        {
            _issue.Title = issueTitle;
            _namingConfig.StandardIssueFormat = "{Volume Name} - {Issue Series - }{Issue Title} {(Release Year)}";
            _namingConfig.ColonReplacementFormat = replacementFormat;

            Subject.BuildIssueFileName(_volume, _edition, _issueFile)
                .Should().Be(expected);
        }

        [TestCase("Volume:Name", ColonReplacementFormat.Smart, "Volume-Name")]
        [TestCase("Volume:Name", ColonReplacementFormat.Dash, "Volume-Name")]
        [TestCase("Volume:Name", ColonReplacementFormat.Delete, "VolumeName")]
        [TestCase("Volume:Name", ColonReplacementFormat.SpaceDash, "Volume -Name")]
        [TestCase("Volume:Name", ColonReplacementFormat.SpaceDashSpace, "Volume - Name")]
        public void should_replace_colon_with_expected_result(string volumeName, ColonReplacementFormat replacementFormat, string expected)
        {
            _volume.Name = volumeName;
            _namingConfig.StandardIssueFormat = "{Volume Name}";
            _namingConfig.ColonReplacementFormat = replacementFormat;

            Subject.BuildIssueFileName(_volume, _edition, _issueFile)
                .Should().Be(expected);
        }
    }
}
