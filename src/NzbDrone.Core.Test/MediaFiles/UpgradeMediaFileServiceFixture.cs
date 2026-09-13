using System.Collections.Generic;
using System.IO;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles
{
    public class UpgradeMediaFileServiceFixture : CoreTest<UpgradeMediaFileService>
    {
        private IssueFile _trackFile;
        private LocalIssue _localTrack;
        private string _rootPath = @"C:\Test\Music\Volume".AsOsAgnostic();

        [SetUp]
        public void Setup()
        {
            _localTrack = new LocalIssue();
            _localTrack.Volume = new Volume
            {
                Path = _rootPath
            };

            _trackFile = Builder<IssueFile>
                .CreateNew()
                .Build();

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FileExists(It.IsAny<string>()))
                .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FolderExists(It.IsAny<string>()))
                .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                 .Setup(c => c.GetParentFolder(It.IsAny<string>()))
                 .Returns<string>(c => Path.GetDirectoryName(c));

            Mocker.GetMock<IRootFolderService>()
                .Setup(c => c.GetBestRootFolder(It.IsAny<string>()))
                .Returns(new RootFolder());
        }

        private void GivenSingleTrackWithSingleTrackFile()
        {
            _localTrack.Issue = Builder<Issue>.CreateNew()
                .With(e => e.IssueFiles = new LazyLoaded<List<IssueFile>>(
                          new List<IssueFile>
                          {
                              new IssueFile
                              {
                                  Id = 1,
                                  Path = Path.Combine(_rootPath, @"Season 01\30.rock.s01e01.avi"),
                              }
                          }))
                .Build();
        }

        [Test]
        public void should_delete_single_track_file_once()
        {
            GivenSingleTrackWithSingleTrackFile();

            Subject.UpgradeIssueFile(_trackFile, _localTrack);

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFile(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_delete_track_file_from_database()
        {
            GivenSingleTrackWithSingleTrackFile();

            Subject.UpgradeIssueFile(_trackFile, _localTrack);

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(It.IsAny<IssueFile>(), DeleteMediaFileReason.Upgrade), Times.Once());
        }

        [Test]
        public void should_delete_existing_file_fromdb_if_file_doesnt_exist()
        {
            GivenSingleTrackWithSingleTrackFile();

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FileExists(It.IsAny<string>()))
                .Returns(false);

            Subject.UpgradeIssueFile(_trackFile, _localTrack);

            // Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(_localTrack.Issue.IssueFiles.Value, DeleteMediaFileReason.Upgrade), Times.Once());
        }

        [Test]
        public void should_not_try_to_recyclebin_existing_file_if_file_doesnt_exist()
        {
            GivenSingleTrackWithSingleTrackFile();

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FileExists(It.IsAny<string>()))
                .Returns(false);

            Subject.UpgradeIssueFile(_trackFile, _localTrack);

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFile(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_return_old_track_file_in_oldFiles()
        {
            GivenSingleTrackWithSingleTrackFile();

            Subject.UpgradeIssueFile(_trackFile, _localTrack).OldFiles.Count.Should().Be(1);
        }

        [Test]
        [Ignore("Pending inkarr fix")]
        public void should_import_if_existing_file_doesnt_exist_in_db()
        {
            _localTrack.Issue = Builder<Issue>.CreateNew()
                .With(e => e.IssueFiles = new LazyLoaded<List<IssueFile>>())
                .Build();

            Subject.UpgradeIssueFile(_trackFile, _localTrack);

            // Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(_localTrack.Issue.IssueFiles.Value, It.IsAny<DeleteMediaFileReason>()), Times.Never());
        }
    }
}
