using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]

    public class QualityAllowedByProfileSpecificationFixture : CoreTest<QualityAllowedByProfileSpecification>
    {
        private RemoteIssue _remoteIssue;

        public static object[] AllowedTestCases =
        {
            new object[] { Quality.MP3 },
            new object[] { Quality.MP3 },
            new object[] { Quality.MP3 }
        };

        public static object[] DeniedTestCases =
        {
            new object[] { Quality.FLAC },
            new object[] { Quality.Unknown }
        };

        [SetUp]
        public void Setup()
        {
            var fakeVolume = Builder<Volume>.CreateNew()
                         .With(c => c.QualityProfile = new QualityProfile { Cutoff = Quality.MP3.Id })
                         .Build();

            _remoteIssue = new RemoteIssue
            {
                Volume = fakeVolume,
                ParsedIssueInfo = new ParsedIssueInfo { Quality = new QualityModel(Quality.MP3, new Revision(version: 2)) },
            };
        }

        [Test]
        [TestCaseSource(nameof(AllowedTestCases))]
        public void should_allow_if_quality_is_defined_in_profile(Quality qualityType)
        {
            _remoteIssue.ParsedIssueInfo.Quality.Quality = qualityType;
            _remoteIssue.Volume.QualityProfile.Value.Items = Qualities.QualityFixture.GetDefaultQualities(Quality.MP3, Quality.MP3, Quality.MP3);

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        [TestCaseSource(nameof(DeniedTestCases))]
        public void should_not_allow_if_quality_is_not_defined_in_profile(Quality qualityType)
        {
            _remoteIssue.ParsedIssueInfo.Quality.Quality = qualityType;
            _remoteIssue.Volume.QualityProfile.Value.Items = Qualities.QualityFixture.GetDefaultQualities(Quality.MP3, Quality.MP3, Quality.MP3);

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeFalse();
        }
    }
}
