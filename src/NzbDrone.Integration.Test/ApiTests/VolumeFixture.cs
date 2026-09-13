using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace NzbDrone.Integration.Test.ApiTests
{
    [TestFixture]
    [Ignore("Waiting for metadata to be back again", Until = "2026-01-15 00:00:00Z")]
    public class VolumeFixture : IntegrationTest
    {
        [Test]
        [Order(0)]
        public void add_volume_with_tags_should_store_them()
        {
            EnsureNoVolume("14586394", "Andrew Hunter Murray");
            var tag = EnsureTag("abc");

            var volume = Volume.Lookup("edition:43765115").Single();

            volume.QualityProfileId = 1;
            volume.MetadataProfileId = 1;
            volume.Path = Path.Combine(VolumeRootFolder, volume.VolumeName);
            volume.Tags = new HashSet<int>();
            volume.Tags.Add(tag.Id);

            var result = Volume.Post(volume);

            result.Should().NotBeNull();
            result.Tags.Should().Equal(tag.Id);
        }

        [Test]
        [Order(0)]
        public void add_volume_without_profileid_should_return_badrequest()
        {
            EnsureNoVolume("14586394", "Andrew Hunter Murray");

            var volume = Volume.Lookup("edition:43765115").Single();

            volume.Path = Path.Combine(VolumeRootFolder, volume.VolumeName);

            Volume.InvalidPost(volume);
        }

        [Test]
        [Order(0)]
        public void add_volume_without_path_should_return_badrequest()
        {
            EnsureNoVolume("14586394", "Andrew Hunter Murray");

            var volume = Volume.Lookup("edition:43765115").Single();

            volume.QualityProfileId = 1;

            Volume.InvalidPost(volume);
        }

        [Test]
        [Order(1)]
        public void add_volume()
        {
            EnsureNoVolume("14586394", "Andrew Hunter Murray");

            var volume = Volume.Lookup("edition:43765115").Single();

            volume.QualityProfileId = 1;
            volume.MetadataProfileId = 1;
            volume.Path = Path.Combine(VolumeRootFolder, volume.VolumeName);

            var result = Volume.Post(volume);

            result.Should().NotBeNull();
            result.Id.Should().NotBe(0);
            result.QualityProfileId.Should().Be(1);
            result.MetadataProfileId.Should().Be(1);
            result.Path.Should().Be(Path.Combine(VolumeRootFolder, volume.VolumeName));
        }

        [Test]
        [Order(2)]
        public void get_all_volume()
        {
            EnsureVolume("14586394", "43765115", "Andrew Hunter Murray");
            EnsureVolume("383606", "16160797", "Robert Galbraith");

            var volumes = Volume.All();

            volumes.Should().NotBeNullOrEmpty();
            volumes.Should().Contain(v => v.ForeignVolumeId == "14586394");
            volumes.Should().Contain(v => v.ForeignVolumeId == "383606");
        }

        [Test]
        [Order(2)]
        public void get_volume_by_id()
        {
            var volume = EnsureVolume("14586394", "43765115", "Andrew Hunter Murray");

            var result = Volume.Get(volume.Id);

            result.ForeignVolumeId.Should().Be("14586394");
        }

        [Test]
        public void get_volume_by_unknown_id_should_return_404()
        {
            var result = Volume.InvalidGet(1000000);
        }

        [Test]
        [Order(2)]
        public void update_volume_profile_id()
        {
            var volume = EnsureVolume("14586394", "43765115", "Andrew Hunter Murray");

            var profileId = 1;
            if (volume.QualityProfileId == profileId)
            {
                profileId = 2;
            }

            volume.QualityProfileId = profileId;

            var result = Volume.Put(volume);

            Volume.Get(volume.Id).QualityProfileId.Should().Be(profileId);
        }

        [Test]
        [Order(3)]
        public void update_volume_monitored()
        {
            var volume = EnsureVolume("14586394", "43765115", "Andrew Hunter Murray", false);

            volume.Monitored.Should().BeFalse();

            volume.Monitored = true;

            var result = Volume.Put(volume);

            result.Monitored.Should().BeTrue();
        }

        [Test]
        [Order(3)]
        public void update_volume_tags()
        {
            var volume = EnsureVolume("14586394", "43765115", "Andrew Hunter Murray");
            var tag = EnsureTag("abc");

            if (volume.Tags.Contains(tag.Id))
            {
                volume.Tags.Remove(tag.Id);

                var result = Volume.Put(volume);
                Volume.Get(volume.Id).Tags.Should().NotContain(tag.Id);
            }
            else
            {
                volume.Tags.Add(tag.Id);

                var result = Volume.Put(volume);
                Volume.Get(volume.Id).Tags.Should().Contain(tag.Id);
            }
        }

        [Test]
        [Order(4)]
        public void delete_volume()
        {
            var volume = EnsureVolume("14586394", "43765115", "Andrew Hunter Murray");

            Volume.Get(volume.Id).Should().NotBeNull();

            Volume.Delete(volume.Id);

            Volume.All().Should().NotContain(v => v.ForeignVolumeId == "14586394");
        }
    }
}
