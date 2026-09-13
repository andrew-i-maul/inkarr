using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.OrganizerTests
{
    [TestFixture]

    public class GetVolumeFolderFixture : CoreTest<FileNameBuilder>
    {
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _namingConfig = NamingConfig.Default;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);
        }

        [TestCase("Avenged Sevenfold", "{Volume Name}", "Avenged Sevenfold")]
        [TestCase("Avenged Sevenfold", "{Volume.Name}", "Avenged.Sevenfold")]
        [TestCase("AC/DC", "{Volume Name}", "AC+DC")]
        [TestCase("In the Woods...", "{Volume.Name}", "In.the.Woods")]
        [TestCase("3OH!3", "{Volume.Name}", "3OH!3")]
        [TestCase("Avenged Sevenfold", ".{Volume.Name}.", "Avenged.Sevenfold")]
        public void should_use_volumeFolderFormat_to_build_folder_name(string volumeName, string format, string expected)
        {
            _namingConfig.VolumeFolderFormat = format;

            var volume = new Volume { Name = volumeName };

            Subject.GetVolumeFolder(volume).Should().Be(expected);
        }
    }
}
