using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.OrganizerTests.FileNameBuilderTests
{
    [TestFixture]
    public class NestedFileNameBuilderFixture : CoreTest<FileNameBuilder>
    {
        private Volume _artist;
        private Issue _album;
        private Edition _release;
        private IssueFile _trackFile;
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _artist = Builder<Volume>
                    .CreateNew()
                    .With(s => s.Name = "VolumeName")
                    .With(s => s.Metadata = new VolumeMetadata
                    {
                        Disambiguation = "US Volume",
                        Name = "VolumeName"
                    })
                    .Build();

            _album = Builder<Issue>
                .CreateNew()
                .With(s => s.Volume = _artist)
                .With(s => s.VolumeMetadata = _artist.Metadata.Value)
                .With(s => s.Title = "A Novel")
                .With(s => s.ReleaseDate = new DateTime(2020, 1, 15))
                .With(s => s.SeriesLinks = new List<SeriesIssueLink>())
                .Build();

            _release = Builder<Edition>
                .CreateNew()
                .With(s => s.Monitored = true)
                .With(s => s.Issue = _album)
                .With(s => s.Title = "A Novel")
                .With(s => s.ReleaseDate = new DateTime(2020, 1, 15))
                .Build();

            _namingConfig = NamingConfig.Default;
            _namingConfig.RenameIssues = true;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);

            _trackFile = Builder<IssueFile>.CreateNew()
                .With(e => e.Quality = new QualityModel(Quality.MOBI))
                .With(e => e.ReleaseGroup = "InkarrTest")
                .Build();

            Mocker.GetMock<IQualityDefinitionService>()
                .Setup(v => v.Get(Moq.It.IsAny<Quality>()))
                .Returns<Quality>(v => Quality.DefaultQualityDefinitions.First(c => c.Quality == v));
        }

        private void WithSeries()
        {
            _album.SeriesLinks = new List<SeriesIssueLink>
            {
                new SeriesIssueLink
                {
                    Series = new Series
                    {
                        Title = "A Series",
                    },
                    Position = "2-3",
                    SeriesPosition = 1
                }
            };
        }

        [Test]
        public void should_build_nested_standard_track_filename_with_forward_slash()
        {
            WithSeries();

            _namingConfig.StandardIssueFormat = "{Issue Series}/{Issue SeriesTitle - }{Issue Title} {(Release Year)}";

            var name = Subject.BuildIssueFileName(_artist, _release, _trackFile)
                .Should().Be("A Series\\A Series #2-3 - A Novel (2020)".AsOsAgnostic());
        }

        [Test]
        public void should_build_standard_track_filename_with_forward_slash()
        {
            _namingConfig.StandardIssueFormat = "{Issue Series}/{Issue SeriesTitle - }{Issue Title} {(Release Year)}";

            Subject.BuildIssueFileName(_artist, _release, _trackFile)
                .Should().Be("A Novel (2020)".AsOsAgnostic());
        }

        [Test]
        public void should_build_nested_standard_track_filename_with_back_slash()
        {
            WithSeries();

            _namingConfig.StandardIssueFormat = "{Issue Series}\\{Issue SeriesTitle - }{Issue Title} {(Release Year)}";

            Subject.BuildIssueFileName(_artist, _release, _trackFile)
                   .Should().Be("A Series\\A Series #2-3 - A Novel (2020)".AsOsAgnostic());
        }

        [Test]
        public void should_build_standard_track_filename_with_back_slash()
        {
            _namingConfig.StandardIssueFormat = "{Issue Series}\\{Issue SeriesTitle - }{Issue Title} {(Release Year)}";

            Subject.BuildIssueFileName(_artist, _release, _trackFile)
                .Should().Be("A Novel (2020)".AsOsAgnostic());
        }
    }
}
