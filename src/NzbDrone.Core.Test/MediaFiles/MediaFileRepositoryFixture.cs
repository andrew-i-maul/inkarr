using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class MediaFileRepositoryFixture : DbTest<MediaFileRepository, IssueFile>
    {
        private Volume _volume;
        private Issue _issue;
        private Edition _edition;

        [SetUp]
        public void Setup()
        {
            var meta = Builder<VolumeMetadata>.CreateNew()
                .With(a => a.Id = 0)
                .Build();
            Db.Insert(meta);

            _volume = Builder<Volume>.CreateNew()
                .With(a => a.VolumeMetadataId = meta.Id)
                .With(a => a.Id = 0)
                .Build();
            Db.Insert(_volume);

            _issue = Builder<Issue>.CreateNew()
                .With(a => a.Id = 0)
                .With(a => a.VolumeMetadataId = _volume.VolumeMetadataId)
                .Build();
            Db.Insert(_issue);

            _edition = Builder<Edition>.CreateNew()
                .With(a => a.Id = 0)
                .With(a => a.IssueId = _issue.Id)
                .Build();
            Db.Insert(_edition);

            var files = Builder<IssueFile>.CreateListOfSize(10)
                .All()
                .With(c => c.Id = 0)
                .With(c => c.Quality = new QualityModel(Quality.MP3))
                .TheFirst(5)
                .With(c => c.EditionId = _edition.Id)
                .TheRest()
                .With(c => c.EditionId = 0)
                .TheFirst(1)
                .With(c => c.Path = @"C:\Test\Path\Volume\somefile1.flac".AsOsAgnostic())
                .TheNext(1)
                .With(c => c.Path = @"C:\Test\Path\Volume\somefile2.flac".AsOsAgnostic())
                .BuildListOfNew();
            Db.InsertMany(files);
        }

        [Test]
        public void get_files_by_volume()
        {
            VerifyData();
            var volumeFiles = Subject.GetFilesByVolume(_volume.Id);
            VerifyEagerLoaded(volumeFiles);

            volumeFiles.Should().OnlyContain(c => c.Volume.Value.Id == _volume.Id);
        }

        [Test]
        public void get_unmapped_files()
        {
            VerifyData();
            var unmappedfiles = Subject.GetUnmappedFiles();
            VerifyUnmapped(unmappedfiles);

            unmappedfiles.Should().HaveCount(5);
        }

        [TestCase("C:\\Test\\Path")]
        [TestCase("C:\\Test\\Path\\")]
        public void get_files_by_base_path_should_cope_with_trailing_slash(string dir)
        {
            VerifyData();
            var firstReleaseFiles = Subject.GetFilesWithBasePath(dir.AsOsAgnostic());

            firstReleaseFiles.Should().HaveCount(2);
        }

        [TestCase("C:\\Test\\Path")]
        [TestCase("C:\\Test\\Path\\")]
        public void get_files_by_base_path_should_not_get_files_for_partial_path(string dir)
        {
            VerifyData();

            var files = Builder<IssueFile>.CreateListOfSize(2)
                .All()
                .With(c => c.Id = 0)
                .With(c => c.Quality = new QualityModel(Quality.MP3))
                .TheFirst(1)
                .With(c => c.Path = @"C:\Test\Path2\Volume\somefile1.flac".AsOsAgnostic())
                .TheNext(1)
                .With(c => c.Path = @"C:\Test\Path2\Volume\somefile2.flac".AsOsAgnostic())
                .BuildListOfNew();
            Db.InsertMany(files);

            var firstReleaseFiles = Subject.GetFilesWithBasePath(dir.AsOsAgnostic());
            firstReleaseFiles.Should().HaveCount(2);
        }

        [Test]
        public void get_file_by_path()
        {
            VerifyData();
            var file = Subject.GetFileWithPath(@"C:\Test\Path\Volume\somefile2.flac".AsOsAgnostic());

            file.Should().NotBeNull();
            file.Edition.IsLoaded.Should().BeTrue();
            file.Edition.Value.Should().NotBeNull();
            file.Volume.IsLoaded.Should().BeTrue();
            file.Volume.Value.Should().NotBeNull();
        }

        [Test]
        public void get_files_by_issue()
        {
            VerifyData();
            var files = Subject.GetFilesByIssue(_issue.Id);
            VerifyEagerLoaded(files);

            files.Should().OnlyContain(c => c.EditionId == _issue.Id);
        }

        private void VerifyData()
        {
            Db.All<Volume>().Should().HaveCount(1);
            Db.All<Issue>().Should().HaveCount(1);
            Db.All<IssueFile>().Should().HaveCount(10);
        }

        private void VerifyEagerLoaded(List<IssueFile> files)
        {
            foreach (var file in files)
            {
                file.Edition.IsLoaded.Should().BeTrue();
                file.Edition.Value.Should().NotBeNull();
                file.Volume.IsLoaded.Should().BeTrue();
                file.Volume.Value.Should().NotBeNull();
                file.Volume.Value.Metadata.IsLoaded.Should().BeTrue();
                file.Volume.Value.Metadata.Value.Should().NotBeNull();
            }
        }

        private void VerifyUnmapped(List<IssueFile> files)
        {
            foreach (var file in files)
            {
                file.Edition.IsLoaded.Should().BeFalse();
                file.Edition.Value.Should().BeNull();
                file.Volume.IsLoaded.Should().BeFalse();
                file.Volume.Value.Should().BeNull();
            }
        }

        [Ignore("Doesn't make sense now we link to edition")]
        [Test]
        public void delete_files_by_issue_should_work_if_join_fails()
        {
            Db.Delete(_issue);
            Subject.DeleteFilesByIssue(_issue.Id);

            Db.All<IssueFile>().Where(x => x.EditionId == _issue.Id).Should().HaveCount(0);
        }
    }
}
