using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.DiskSpace;
using NzbDrone.Core.Issues;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.DiskSpace
{
    [TestFixture]
    public class DiskSpaceServiceFixture : CoreTest<DiskSpaceService>
    {
        private RootFolder _rootDir;
        private string _volumeFolder1;
        private string _volumeFolder2;

        [SetUp]
        public void SetUp()
        {
            _rootDir = new RootFolder { Path = @"G:\fasdlfsdf".AsOsAgnostic() };
            _volumeFolder1 = Path.Combine(_rootDir.Path, "volume1");
            _volumeFolder2 = Path.Combine(_rootDir.Path, "volume2");

            Mocker.GetMock<IRootFolderService>()
                  .Setup(x => x.All())
                  .Returns(new List<RootFolder>() { _rootDir });

            Mocker.GetMock<IDiskProvider>()
                .Setup(v => v.FolderExists(_rootDir.Path))
                .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.GetMounts())
                  .Returns(new List<IMount>());

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.GetPathRoot(It.IsAny<string>()))
                  .Returns(@"G:\".AsOsAgnostic());

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.GetAvailableSpace(It.IsAny<string>()))
                  .Returns(0);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.GetTotalSize(It.IsAny<string>()))
                  .Returns(0);

            GivenVolume();
        }

        private void GivenVolume(params Volume[] volume)
        {
            Mocker.GetMock<IVolumeService>()
                  .Setup(v => v.GetAllVolumes())
                  .Returns(volume.ToList());
        }

        private void GivenExistingFolder(string folder)
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FolderExists(folder))
                  .Returns(true);
        }

        [Test]
        public void should_check_diskspace_for_volume_folders()
        {
            GivenVolume(new Volume { Path = _volumeFolder1 });

            GivenExistingFolder(_volumeFolder1);

            var freeSpace = Subject.GetFreeSpace();

            freeSpace.Should().NotBeEmpty();
        }

        [Test]
        public void should_check_diskspace_for_same_root_folder_only_once()
        {
            GivenVolume(new Volume { Path = _volumeFolder1 }, new Volume { Path = _volumeFolder2 });

            GivenExistingFolder(_volumeFolder1);
            GivenExistingFolder(_volumeFolder2);

            var freeSpace = Subject.GetFreeSpace();

            freeSpace.Should().HaveCount(1);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.GetAvailableSpace(It.IsAny<string>()), Times.Once());
        }

        [TestCase("/boot")]
        [TestCase("/var/lib/rancher")]
        [TestCase("/var/lib/rancher/volumes")]
        [TestCase("/var/lib/kubelet")]
        [TestCase("/var/lib/docker")]
        [TestCase("/some/place/docker/aufs")]
        [TestCase("/etc/network")]
        public void should_not_check_diskspace_for_irrelevant_mounts(string path)
        {
            var mount = new Mock<IMount>();
            mount.SetupGet(v => v.RootDirectory).Returns(path);
            mount.SetupGet(v => v.DriveType).Returns(System.IO.DriveType.Fixed);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.GetMounts())
                  .Returns(new List<IMount> { mount.Object });

            Mocker.GetMock<IRootFolderService>()
                  .Setup(x => x.All())
                  .Returns(new List<RootFolder>());

            var freeSpace = Subject.GetFreeSpace();

            freeSpace.Should().BeEmpty();
        }
    }
}
