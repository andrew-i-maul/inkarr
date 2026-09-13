using System;
using System.IO;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.TrackFileMovingServiceTests
{
    [TestFixture]
    public class MoveTrackFileFixture : CoreTest<IssueFileMovingService>
    {
        private Volume _volume;
        private IssueFile _trackFile;
        private LocalIssue _localtrack;

        [SetUp]
        public void Setup()
        {
            _volume = Builder<Volume>.CreateNew()
                                     .With(s => s.Path = @"C:\Test\Music\Volume".AsOsAgnostic())
                                     .Build();

            _trackFile = Builder<IssueFile>.CreateNew()
                                               .With(f => f.Path = null)
                                               .With(f => f.Path = Path.Combine(_volume.Path, @"Issue\File.mp3"))
                                               .Build();

            _localtrack = Builder<LocalIssue>.CreateNew()
                                                 .With(l => l.Volume = _volume)
                                                 .With(l => l.Issue = Builder<Issue>.CreateNew().Build())
                                                 .Build();

            Mocker.GetMock<IBuildFileNames>()
                  .Setup(s => s.BuildIssueFileName(It.IsAny<Volume>(), It.IsAny<Edition>(), It.IsAny<IssueFile>(), null, null))
                  .Returns("File Name");

            Mocker.GetMock<IBuildFileNames>()
                  .Setup(s => s.BuildIssueFilePath(It.IsAny<Volume>(), It.IsAny<Edition>(), It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(@"C:\Test\Music\Volume\Issue\File Name.mp3".AsOsAgnostic());

            Mocker.GetMock<IBuildFileNames>()
                  .Setup(s => s.BuildIssuePath(It.IsAny<Volume>()))
                  .Returns(@"C:\Test\Music\Volume\Issue".AsOsAgnostic());

            var rootFolder = @"C:\Test\Music\".AsOsAgnostic();
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(rootFolder))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FileExists(It.IsAny<string>()))
                  .Returns(true);
        }

        [Test]
        public void should_catch_UnauthorizedAccessException_during_folder_inheritance()
        {
            WindowsOnly();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.InheritFolderPermissions(It.IsAny<string>()))
                  .Throws<UnauthorizedAccessException>();

            Subject.MoveIssueFile(_trackFile, _localtrack);
        }

        [Test]
        public void should_catch_InvalidOperationException_during_folder_inheritance()
        {
            WindowsOnly();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.InheritFolderPermissions(It.IsAny<string>()))
                  .Throws<InvalidOperationException>();

            Subject.MoveIssueFile(_trackFile, _localtrack);
        }

        [Test]
        public void should_notify_on_volume_folder_creation()
        {
            Subject.MoveIssueFile(_trackFile, _localtrack);

            Mocker.GetMock<IEventAggregator>()
                  .Verify(s => s.PublishEvent<TrackFolderCreatedEvent>(It.Is<TrackFolderCreatedEvent>(p =>
                      p.VolumeFolder.IsNotNullOrWhiteSpace())), Times.Once());
        }

        [Test]
        public void should_notify_on_issue_folder_creation()
        {
            Subject.MoveIssueFile(_trackFile, _localtrack);

            Mocker.GetMock<IEventAggregator>()
                  .Verify(s => s.PublishEvent<TrackFolderCreatedEvent>(It.Is<TrackFolderCreatedEvent>(p =>
                      p.IssueFolder.IsNotNullOrWhiteSpace())), Times.Once());
        }

        [Test]
        public void should_not_notify_if_volume_folder_already_exists()
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(_volume.Path))
                  .Returns(true);

            Subject.MoveIssueFile(_trackFile, _localtrack);

            Mocker.GetMock<IEventAggregator>()
                  .Verify(s => s.PublishEvent<TrackFolderCreatedEvent>(It.Is<TrackFolderCreatedEvent>(p =>
                      p.VolumeFolder.IsNotNullOrWhiteSpace())), Times.Never());
        }
    }
}
