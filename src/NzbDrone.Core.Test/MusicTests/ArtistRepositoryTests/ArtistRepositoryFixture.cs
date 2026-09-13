using System;
using System.Collections.Generic;
using System.Data.SQLite;
using FizzWare.NBuilder;
using FluentAssertions;
using Npgsql;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Profiles.Metadata;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MusicTests.VolumeRepositoryTests
{
    [TestFixture]

    public class VolumeRepositoryFixture : DbTest<VolumeRepository, Volume>
    {
        private VolumeRepository _volumeRepo;
        private VolumeMetadataRepository _volumeMetadataRepo;

        [SetUp]
        public void Setup()
        {
            _volumeRepo = Mocker.Resolve<VolumeRepository>();
            _volumeMetadataRepo = Mocker.Resolve<VolumeMetadataRepository>();
        }

        private void AddVolume(string name, string foreignId, List<string> oldIds = null)
        {
            if (oldIds == null)
            {
                oldIds = new List<string>();
            }

            var metadata = Builder<VolumeMetadata>.CreateNew()
                .With(a => a.Id = 0)
                .With(a => a.Name = name)
                .With(a => a.TitleSlug = foreignId)
                .BuildNew();

            var volume = Builder<Volume>.CreateNew()
                .With(a => a.Id = 0)
                .With(a => a.Metadata = metadata)
                .With(a => a.CleanName = Parser.Parser.CleanVolumeName(name))
                .With(a => a.ForeignVolumeId = foreignId)
                .BuildNew();

            _volumeMetadataRepo.Insert(metadata);
            volume.VolumeMetadataId = metadata.Id;
            _volumeRepo.Insert(volume);
        }

        private void GivenVolumes()
        {
            AddVolume("The Black Eyed Peas", "d5be5333-4171-427e-8e12-732087c6b78e");
            AddVolume("The Black Keys", "d15721d8-56b4-453d-b506-fc915b14cba2", new List<string> { "6f2ed437-825c-4cea-bb58-bf7688c6317a" });
        }

        [Test]
        public void should_lazyload_profiles()
        {
            var profile = new QualityProfile
            {
                Items = Qualities.QualityFixture.GetDefaultQualities(Quality.FLAC, Quality.MP3, Quality.MP3),

                Cutoff = Quality.FLAC.Id,
                Name = "TestProfile"
            };

            var metaProfile = new MetadataProfile
            {
                Name = "TestProfile"
            };

            Mocker.Resolve<QualityProfileRepository>().Insert(profile);
            Mocker.Resolve<MetadataProfileRepository>().Insert(metaProfile);

            var volume = Builder<Volume>.CreateNew().BuildNew();
            volume.QualityProfileId = profile.Id;
            volume.MetadataProfileId = metaProfile.Id;

            Subject.Insert(volume);

            StoredModel.QualityProfile.Should().NotBeNull();
            StoredModel.MetadataProfile.Should().NotBeNull();
        }

        [TestCase("The Black Eyed Peas")]
        [TestCase("The Black Keys")]
        public void should_find_volume_in_db_by_name(string name)
        {
            GivenVolumes();
            var volume = _volumeRepo.FindByName(Parser.Parser.CleanVolumeName(name));

            volume.Should().NotBeNull();
            volume.Name.Should().Be(name);
        }

        [Test]
        public void should_find_volume_in_by_id()
        {
            GivenVolumes();
            var volume = _volumeRepo.FindById("d5be5333-4171-427e-8e12-732087c6b78e");

            volume.Should().NotBeNull();
            volume.ForeignVolumeId.Should().Be("d5be5333-4171-427e-8e12-732087c6b78e");
        }

        [Test]
        public void should_not_find_volume_if_multiple_volumes_have_same_name()
        {
            GivenVolumes();

            var name = "Alice Cooper";
            AddVolume(name, "ee58c59f-8e7f-4430-b8ca-236c4d3745ae");
            AddVolume(name, "4d7928cd-7ed2-4282-8c29-c0c9f966f1bd");

            _volumeRepo.All().Should().HaveCount(4);

            var volume = _volumeRepo.FindByName(Parser.Parser.CleanVolumeName(name));
            volume.Should().BeNull();
        }

        [Test]
        public void should_throw_sql_exception_adding_duplicate_volume()
        {
            var name = "test";
            var metadata = Builder<VolumeMetadata>.CreateNew()
                .With(a => a.Id = 0)
                .With(a => a.Name = name)
                .BuildNew();

            var volume1 = Builder<Volume>.CreateNew()
                .With(a => a.Id = 0)
                .With(a => a.Metadata = metadata)
                .With(a => a.CleanName = Parser.Parser.CleanVolumeName(name))
                .BuildNew();

            var volume2 = volume1.JsonClone();
            volume2.Metadata = metadata;

            _volumeMetadataRepo.Insert(metadata);
            _volumeRepo.Insert(volume1);

            Action insertDupe = () => _volumeRepo.Insert(volume2);
            if (Db.DatabaseType == DatabaseType.PostgreSQL)
            {
                insertDupe.Should().Throw<PostgresException>();
            }
            else
            {
                insertDupe.Should().Throw<SQLiteException>();
            }
        }
    }
}
