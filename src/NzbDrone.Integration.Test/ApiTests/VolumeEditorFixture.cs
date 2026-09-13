using System.Linq;
using FluentAssertions;
using Inkarr.Api.V1.Volume;
using NUnit.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Integration.Test.ApiTests
{
    [TestFixture]
    [Ignore("Waiting for metadata to be back again", Until = "2026-01-15 00:00:00Z")]
    public class VolumeEditorFixture : IntegrationTest
    {
        private void GivenExistingVolume()
        {
            WaitForCompletion(() => Profiles.All().Count > 0);

            foreach (var name in new[] { "Alien Ant Farm", "Kiss" })
            {
                var newVolume = Volume.Lookup(name).First();

                newVolume.QualityProfileId = 1;
                newVolume.MetadataProfileId = 1;
                newVolume.Path = string.Format(@"C:\Test\{0}", name).AsOsAgnostic();

                Volume.Post(newVolume);
            }
        }

        [Test]
        public void should_be_able_to_update_multiple_volume()
        {
            GivenExistingVolume();

            var volume = Volume.All();

            var volumeEditor = new VolumeEditorResource
            {
                QualityProfileId = 2,
                VolumeIds = volume.Select(o => o.Id).ToList()
            };

            var result = Volume.Editor(volumeEditor);

            result.Should().HaveCount(2);
            result.TrueForAll(s => s.QualityProfileId == 2).Should().BeTrue();
        }
    }
}
