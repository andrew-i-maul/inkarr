using System.Collections.Generic;
using System.IO;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Commands;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MusicTests
{
    [TestFixture]
    public class MoveVolumeServiceFixture : CoreTest<MoveVolumeService>
    {
        private Volume _volume;
        private MoveVolumeCommand _command;
        private BulkMoveVolumeCommand _bulkCommand;

        [SetUp]
        public void Setup()
        {
            _volume = Builder<Volume>
                .CreateNew()
                .Build();

            _command = new MoveVolumeCommand
            {
                VolumeId = 1,
                SourcePath = @"C:\Test\Music\Volume".AsOsAgnostic(),
                DestinationPath = @"C:\Test\Music2\Volume".AsOsAgnostic()
            };

            _bulkCommand = new BulkMoveVolumeCommand
            {
                Volume = new List<BulkMoveVolume>
                {
                    new BulkMoveVolume
                    {
                        VolumeId = 1,
                        SourcePath = @"C:\Test\Music\Volume".AsOsAgnostic()
                    }
                },
                DestinationRootFolder = @"C:\Test\Music2".AsOsAgnostic()
            };

            Mocker.GetMock<IVolumeService>()
                .Setup(s => s.GetVolume(It.IsAny<int>()))
                .Returns(_volume);

            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.FolderExists(It.IsAny<string>()))
                .Returns(true);
        }

        private void GivenFailedMove()
        {
            Mocker.GetMock<IDiskTransferService>()
                .Setup(s => s.TransferFolder(It.IsAny<string>(), It.IsAny<string>(), TransferMode.Move))
                .Throws<IOException>();
        }

        [Test]
        public void should_log_error_when_move_throws_an_exception()
        {
            GivenFailedMove();

            Subject.Execute(_command);

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_revert_volume_path_on_error()
        {
            GivenFailedMove();

            Subject.Execute(_command);

            ExceptionVerification.ExpectedErrors(1);

            Mocker.GetMock<IVolumeService>()
                .Verify(v => v.UpdateVolume(It.IsAny<Volume>()), Times.Once());
        }

        [Test]
        public void should_use_destination_path()
        {
            Subject.Execute(_command);

            Mocker.GetMock<IDiskTransferService>()
                .Verify(
                    v => v.TransferFolder(_command.SourcePath,
                                          _command.DestinationPath,
                                          TransferMode.Move),
                    Times.Once());

            Mocker.GetMock<IBuildFileNames>()
                .Verify(v => v.GetVolumeFolder(It.IsAny<Volume>(), null), Times.Never());
        }

        [Test]
        public void should_build_new_path_when_root_folder_is_provided()
        {
            var volumeFolder = "Volume";
            var expectedPath = Path.Combine(_bulkCommand.DestinationRootFolder, volumeFolder);

            Mocker.GetMock<IBuildFileNames>()
                .Setup(s => s.GetVolumeFolder(It.IsAny<Volume>(), null))
                .Returns(volumeFolder);

            Subject.Execute(_bulkCommand);

            Mocker.GetMock<IDiskTransferService>()
                .Verify(
                    v => v.TransferFolder(_bulkCommand.Volume.First().SourcePath,
                                          expectedPath,
                                          TransferMode.Move),
                    Times.Once());
        }

        [Test]
        public void should_skip_volume_folder_if_it_does_not_exist()
        {
            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.FolderExists(It.IsAny<string>()))
                .Returns(false);

            Subject.Execute(_command);

            Mocker.GetMock<IDiskTransferService>()
                .Verify(
                    v => v.TransferFolder(_command.SourcePath,
                        _command.DestinationPath,
                        TransferMode.Move), Times.Never());

            Mocker.GetMock<IBuildFileNames>()
                .Verify(v => v.GetVolumeFolder(It.IsAny<Volume>(), null), Times.Never());
        }
    }
}
