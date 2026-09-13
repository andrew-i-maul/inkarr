using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore
{
    [TestFixture]
    public class LazyLoadingFixture : DbTest
    {
        [SetUp]
        public void Setup()
        {
            SqlBuilderExtensions.LogSql = true;

            var profile = new QualityProfile
            {
                Name = "Test",
                Cutoff = Quality.MP3.Id,
                Items = Qualities.QualityFixture.GetDefaultQualities()
            };

            profile = Db.Insert(profile);

            var metadata = Builder<VolumeMetadata>.CreateNew()
                .With(v => v.Id = 0)
                .Build();
            Db.Insert(metadata);

            var volume = Builder<Volume>.CreateListOfSize(1)
                .All()
                .With(v => v.Id = 0)
                .With(v => v.QualityProfileId = profile.Id)
                .With(v => v.VolumeMetadataId = metadata.Id)
                .BuildListOfNew();

            Db.InsertMany(volume);

            var issues = Builder<Issue>.CreateListOfSize(3)
                .All()
                .With(v => v.Id = 0)
                .With(v => v.VolumeMetadataId = metadata.Id)
                .BuildListOfNew();

            Db.InsertMany(issues);

            var editions = new List<Edition>();
            foreach (var issue in issues)
            {
                editions.Add(
                    Builder<Edition>.CreateNew()
                    .With(v => v.Id = 0)
                    .With(v => v.IssueId = issue.Id)
                    .With(v => v.ForeignEditionId = "test" + issue.Id)
                    .Build());
            }

            Db.InsertMany(editions);

            var trackFiles = Builder<IssueFile>.CreateListOfSize(1)
                .All()
                .With(v => v.Id = 0)
                .With(v => v.EditionId = editions[0].Id)
                .With(v => v.Quality = new QualityModel())
                .BuildListOfNew();

            Db.InsertMany(trackFiles);
        }

        [Test]
        public void should_lazy_load_volume_for_trackfile()
        {
            var db = Mocker.Resolve<IDatabase>();
            var tracks = db.Query<IssueFile>(new SqlBuilder(db.DatabaseType)).ToList();

            Assert.IsNotEmpty(tracks);
            foreach (var track in tracks)
            {
                Assert.IsFalse(track.Volume.IsLoaded);
                Assert.IsNotNull(track.Volume.Value);
                Assert.IsTrue(track.Volume.IsLoaded);
                Assert.IsTrue(track.Volume.Value.Metadata.IsLoaded);
            }
        }

        [Test]
        public void should_lazy_load_trackfile_if_not_joined()
        {
            var db = Mocker.Resolve<IDatabase>();
            var tracks = db.Query<Issue>(new SqlBuilder(db.DatabaseType)).ToList();

            foreach (var track in tracks)
            {
                Assert.IsFalse(track.IssueFiles.IsLoaded);
                Assert.IsNotNull(track.IssueFiles.Value);
                Assert.IsTrue(track.IssueFiles.IsLoaded);
            }
        }

        [Test]
        public void should_explicit_load_everything_if_joined()
        {
            var db = Mocker.Resolve<IDatabase>();
            var files = MediaFileRepository.Query(db,
                                                  new SqlBuilder(db.DatabaseType)
                                                  .Join<IssueFile, Edition>((t, a) => t.EditionId == a.Id)
                                                  .Join<Edition, Issue>((e, b) => e.IssueId == b.Id)
                                                  .Join<Issue, Volume>((issue, volume) => issue.VolumeMetadataId == volume.VolumeMetadataId)
                                                  .Join<Volume, VolumeMetadata>((a, m) => a.VolumeMetadataId == m.Id));

            Assert.IsNotEmpty(files);
            foreach (var file in files)
            {
                Assert.IsTrue(file.Edition.IsLoaded);
                Assert.IsTrue(file.Volume.IsLoaded);
                Assert.IsTrue(file.Volume.Value.Metadata.IsLoaded);
            }
        }
    }
}
