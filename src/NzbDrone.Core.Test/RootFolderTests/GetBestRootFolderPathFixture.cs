using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.RootFolderTests
{
    [TestFixture]
    public class GetBestRootFolderPathFixture : CoreTest<RootFolderService>
    {
        private void GivenRootFolders(params string[] paths)
        {
            Mocker.GetMock<IRootFolderRepository>()
                .Setup(s => s.All())
                .Returns(paths.Select(p => new RootFolder { Path = p }));
        }

        [Test]
        public void should_return_root_folder_that_is_parent_path()
        {
            GivenRootFolders(@"C:\Test\Issues".AsOsAgnostic(), @"D:\Test\Issues".AsOsAgnostic());
            Subject.GetBestRootFolderPath(@"C:\Test\Issues\Volume Title".AsOsAgnostic()).Should().Be(@"C:\Test\Issues".AsOsAgnostic());
        }

        [Test]
        public void should_return_root_folder_that_is_grandparent_path()
        {
            GivenRootFolders(@"C:\Test\Issues".AsOsAgnostic(), @"D:\Test\Issues".AsOsAgnostic());
            Subject.GetBestRootFolderPath(@"C:\Test\Issues\S\Volume Title".AsOsAgnostic()).Should().Be(@"C:\Test\Issues".AsOsAgnostic());
        }

        [Test]
        public void should_get_parent_path_from_os_path_if_matching_root_folder_is_not_found()
        {
            var artistPath = @"T:\Test\Issues\Volume Title".AsOsAgnostic();

            GivenRootFolders(@"C:\Test\Issues".AsOsAgnostic(), @"D:\Test\Issues".AsOsAgnostic());
            Subject.GetBestRootFolderPath(artistPath).Should().Be(@"T:\Test\Issues".AsOsAgnostic());
        }

        [Test]
        public void should_get_parent_path_from_os_path_if_matching_root_folder_is_not_found_for_posix_path()
        {
            WindowsOnly();

            var artistPath = "/mnt/issues/Volume Title";

            GivenRootFolders(@"C:\Test\Issues".AsOsAgnostic(), @"D:\Test\Issues".AsOsAgnostic());
            Subject.GetBestRootFolderPath(artistPath).Should().Be(@"/mnt/issues");
        }

        [Test]
        public void should_get_parent_path_from_os_path_if_matching_root_folder_is_not_found_for_windows_path()
        {
            PosixOnly();

            var artistPath = @"T:\Test\Issues\Volume Title";

            GivenRootFolders(@"C:\Test\Issues".AsOsAgnostic(), @"D:\Test\Issues".AsOsAgnostic());
            Subject.GetBestRootFolderPath(artistPath).Should().Be(@"T:\Test\Issues");
        }
    }
}
