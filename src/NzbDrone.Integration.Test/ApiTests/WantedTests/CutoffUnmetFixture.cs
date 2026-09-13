using System.Linq;
using FluentAssertions;
using Inkarr.Api.V1.RootFolders;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Integration.Test.ApiTests.WantedTests
{
    [TestFixture]
    [Ignore("Waiting for metadata to be back again", Until = "2026-01-15 00:00:00Z")]
    public class CutoffUnmetFixture : IntegrationTest
    {
        [SetUp]
        public void Setup()
        {
            // Add a root folder
            RootFolders.Post(new RootFolderResource
            {
                Name = "TestLibrary",
                Path = VolumeRootFolder,
                DefaultMetadataProfileId = 1,
                DefaultQualityProfileId = 1,
                DefaultMonitorOption = MonitorTypes.All
            });
        }

        [Test]
        [Order(2)]
        public void cutoff_should_have_monitored_items()
        {
            EnsureProfileCutoff(1, Quality.AZW3, true);
            var volume = EnsureVolume("14586394", "43765115", "Andrew Hunter Murray", true);
            EnsureIssueFile(volume, 1, "43765115", Quality.MOBI);

            var result = WantedCutoffUnmet.GetPaged(0, 15, "releaseDate", "desc");

            result.Records.Should().NotBeEmpty();
        }

        [Test]
        [Order(2)]
        public void cutoff_should_not_have_unmonitored_items()
        {
            EnsureProfileCutoff(1, Quality.AZW3, true);
            var volume = EnsureVolume("14586394", "43765115", "Andrew Hunter Murray", false);
            EnsureIssueFile(volume, 1, "43765115", Quality.MOBI);

            var result = WantedCutoffUnmet.GetPaged(0, 15, "releaseDate", "desc");

            result.Records.Should().BeEmpty();
        }

        [Test]
        [Order(2)]
        public void cutoff_should_have_volume()
        {
            EnsureProfileCutoff(1, Quality.AZW3, true);
            var volume = EnsureVolume("14586394", "43765115", "Andrew Hunter Murray", true);
            EnsureIssueFile(volume, 1, "43765115", Quality.MOBI);

            var result = WantedCutoffUnmet.GetPagedIncludeVolume(0, 15, "releaseDate", "desc", includeVolume: true);

            result.Records.First().Volume.Should().NotBeNull();
            result.Records.First().Volume.VolumeName.Should().Be("Andrew Hunter Murray");
        }

        [Test]
        [Order(2)]
        public void cutoff_should_not_have_volume()
        {
            EnsureProfileCutoff(1, Quality.AZW3, true);
            var volume = EnsureVolume("14586394", "43765115", "Andrew Hunter Murray", true);
            EnsureIssueFile(volume, 1, "43765115", Quality.MOBI);

            var result = WantedCutoffUnmet.GetPagedIncludeVolume(0, 15, "releaseDate", "desc", includeVolume: false);

            result.Records.First().Volume.Should().BeNull();
        }

        [Test]
        [Order(2)]
        public void cutoff_should_have_unmonitored_items()
        {
            EnsureProfileCutoff(1, Quality.AZW3, true);
            var volume = EnsureVolume("14586394", "43765115", "Andrew Hunter Murray", false);
            EnsureIssueFile(volume, 1, "43765115", Quality.MOBI);

            var result = WantedCutoffUnmet.GetPaged(0, 15, "releaseDate", "desc", "monitored", false);

            result.Records.Should().NotBeEmpty();
        }
    }
}
