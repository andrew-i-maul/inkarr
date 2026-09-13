using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles
{
    public class RenameTrackFileServiceFixture : CoreTest<RenameIssueFileService>
    {
        private Volume _volume;
        private List<IssueFile> _trackFiles;

        [SetUp]
        public void Setup()
        {
            _volume = Builder<Volume>.CreateNew()
                                     .Build();

            _trackFiles = Builder<IssueFile>.CreateListOfSize(2)
                                                .All()
                                                .With(e => e.Volume = _volume)
                                                .With(e => e.CalibreId = 0)
                                                .Build()
                                                .ToList();

            Mocker.GetMock<IVolumeService>()
                  .Setup(s => s.GetVolume(_volume.Id))
                  .Returns(_volume);

            Mocker.GetMock<IMediaFileService>()
                .Setup(s => s.GetFilesByVolume(_volume.Id))
                .Returns(_trackFiles);
        }

        private void GivenNoTrackFiles()
        {
            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.Get(It.IsAny<IEnumerable<int>>()))
                  .Returns(new List<IssueFile>());
        }

        private void GivenTrackFiles()
        {
            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.Get(It.IsAny<IEnumerable<int>>()))
                  .Returns(_trackFiles);
        }

        private void GivenMovedFiles()
        {
            Mocker.GetMock<IMoveIssueFiles>()
                  .Setup(s => s.MoveIssueFile(It.IsAny<IssueFile>(), _volume));
        }

        [Test]
        public void should_not_publish_event_if_no_files_to_rename()
        {
            GivenNoTrackFiles();

            Subject.Execute(new RenameFilesCommand(_volume.Id, new List<int> { 1 }));

            Mocker.GetMock<IEventAggregator>()
                  .Verify(v => v.PublishEvent(It.IsAny<VolumeRenamedEvent>()), Times.Never());
        }

        [Test]
        public void should_not_publish_event_if_no_files_are_renamed()
        {
            GivenTrackFiles();

            Mocker.GetMock<IMoveIssueFiles>()
                  .Setup(s => s.MoveIssueFile(It.IsAny<IssueFile>(), It.IsAny<Volume>()))
                  .Throws(new SameFilenameException("Same file name", "Filename"));

            Subject.Execute(new RenameFilesCommand(_volume.Id, new List<int> { 1 }));

            Mocker.GetMock<IEventAggregator>()
                  .Verify(v => v.PublishEvent(It.IsAny<VolumeRenamedEvent>()), Times.Never());
        }

        [Test]
        public void should_publish_event_if_files_are_renamed()
        {
            GivenTrackFiles();
            GivenMovedFiles();

            Subject.Execute(new RenameFilesCommand(_volume.Id, new List<int> { 1 }));

            Mocker.GetMock<IEventAggregator>()
                  .Verify(v => v.PublishEvent(It.IsAny<VolumeRenamedEvent>()), Times.Once());
        }

        [Test]
        public void should_update_moved_files()
        {
            GivenTrackFiles();
            GivenMovedFiles();

            Subject.Execute(new RenameFilesCommand(_volume.Id, new List<int> { 1 }));

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Update(It.IsAny<IssueFile>()), Times.Exactly(2));
        }

        [Test]
        public void should_get_trackfiles_by_ids_only()
        {
            GivenTrackFiles();
            GivenMovedFiles();

            var files = new List<int> { 1 };

            Subject.Execute(new RenameFilesCommand(_volume.Id, files));

            Mocker.GetMock<IMediaFileService>()
                  .Verify(v => v.Get(files), Times.Once());
        }
    }
}
