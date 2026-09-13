using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MusicTests.VolumeRepositoryTests
{
    [TestFixture]

    public class VolumeMetadataRepositoryFixture : DbTest<VolumeMetadataRepository, VolumeMetadata>
    {
        private VolumeMetadataRepository _volumeMetadataRepo;
        private List<VolumeMetadata> _metadataList;

        [SetUp]
        public void Setup()
        {
            _volumeMetadataRepo = Mocker.Resolve<VolumeMetadataRepository>();
            _metadataList = Builder<VolumeMetadata>.CreateListOfSize(10).All().With(x => x.Id = 0).BuildList();
        }

        [Test]
        public void upsert_many_should_insert_list_of_new()
        {
            var updated = _volumeMetadataRepo.UpsertMany(_metadataList);
            AllStoredModels.Should().HaveCount(_metadataList.Count);
            updated.Should().BeTrue();
        }

        [Test]
        public void upsert_many_should_upsert_existing_with_id_0()
        {
            var clone = _metadataList.JsonClone();
            var updated = _volumeMetadataRepo.UpsertMany(clone);

            updated.Should().BeTrue();
            AllStoredModels.Should().HaveCount(_metadataList.Count);

            updated = _volumeMetadataRepo.UpsertMany(_metadataList);
            updated.Should().BeFalse();
            AllStoredModels.Should().HaveCount(_metadataList.Count);
        }

        [Test]
        public void upsert_many_should_upsert_mixed_list_of_old_and_new()
        {
            var clone = _metadataList.Take(5).ToList().JsonClone();
            var updated = _volumeMetadataRepo.UpsertMany(clone);

            updated.Should().BeTrue();
            AllStoredModels.Should().HaveCount(clone.Count);

            updated = _volumeMetadataRepo.UpsertMany(_metadataList);
            updated.Should().BeTrue();
            AllStoredModels.Should().HaveCount(_metadataList.Count);
        }
    }
}
