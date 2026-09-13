using System.Collections.Generic;
using System.IO;
using FizzWare.NBuilder;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MusicTests
{
    [TestFixture]
    public class AddVolumeFixture : CoreTest<AddVolumeService>
    {
        private Volume _fakeVolume;

        [SetUp]
        public void Setup()
        {
            _fakeVolume = Builder<Volume>
                .CreateNew()
                .With(s => s.Path = null)
                .Build();
            _fakeVolume.Issues = new List<Issue>();

            Mocker.GetMock<IVolumeService>()
                .Setup(s => s.AddVolume(It.IsAny<Volume>(), It.IsAny<bool>()))
                .Returns<Volume, bool>((volume, _) => volume);
        }

        private void GivenValidVolume(string inkarrId)
        {
            Mocker.GetMock<IProvideVolumeInfo>()
                .Setup(s => s.GetVolumeInfo(inkarrId, false))
                .Returns(_fakeVolume);
        }

        private void GivenValidPath()
        {
            Mocker.GetMock<IBuildFileNames>()
                  .Setup(s => s.GetVolumeFolder(It.IsAny<Volume>(), null))
                  .Returns<Volume, NamingConfig>((c, n) => c.Name);

            Mocker.GetMock<IAddVolumeValidator>()
                  .Setup(s => s.Validate(It.IsAny<Volume>()))
                  .Returns(new ValidationResult());
        }

        [Test]
        public void should_be_able_to_add_a_volume_without_passing_in_name()
        {
            var newVolume = new Volume
            {
                ForeignVolumeId = "ce09ea31-3d4a-4487-a797-e315175457a0",
                RootFolderPath = @"C:\Test\Music"
            };

            GivenValidVolume(newVolume.ForeignVolumeId);
            GivenValidPath();

            var volume = Subject.AddVolume(newVolume);

            volume.Name.Should().Be(_fakeVolume.Name);
        }

        [Test]
        public void should_have_proper_path()
        {
            var newVolume = new Volume
            {
                ForeignVolumeId = "ce09ea31-3d4a-4487-a797-e315175457a0",
                RootFolderPath = @"C:\Test\Music"
            };

            GivenValidVolume(newVolume.ForeignVolumeId);
            GivenValidPath();

            var volume = Subject.AddVolume(newVolume);

            volume.Path.Should().Be(Path.Combine(newVolume.RootFolderPath, _fakeVolume.Name));
        }

        [Test]
        public void should_throw_if_volume_validation_fails()
        {
            var newVolume = new Volume
            {
                ForeignVolumeId = "ce09ea31-3d4a-4487-a797-e315175457a0",
                Path = @"C:\Test\Music\Name1"
            };

            GivenValidVolume(newVolume.ForeignVolumeId);

            Mocker.GetMock<IAddVolumeValidator>()
                  .Setup(s => s.Validate(It.IsAny<Volume>()))
                  .Returns(new ValidationResult(new List<ValidationFailure>
                                                {
                                                    new ValidationFailure("Path", "Test validation failure")
                                                }));

            Assert.Throws<ValidationException>(() => Subject.AddVolume(newVolume));
        }

        [Test]
        public void should_throw_if_volume_cannot_be_found()
        {
            var newVolume = new Volume
            {
                ForeignVolumeId = "ce09ea31-3d4a-4487-a797-e315175457a0",
                Path = @"C:\Test\Music\Name1"
            };

            Mocker.GetMock<IProvideVolumeInfo>()
                  .Setup(s => s.GetVolumeInfo(newVolume.ForeignVolumeId, false))
                  .Throws(new VolumeNotFoundException(newVolume.ForeignVolumeId));

            Mocker.GetMock<IAddVolumeValidator>()
                  .Setup(s => s.Validate(It.IsAny<Volume>()))
                  .Returns(new ValidationResult(new List<ValidationFailure>
                                                {
                                                    new ValidationFailure("Path", "Test validation failure")
                                                }));

            Assert.Throws<ValidationException>(() => Subject.AddVolume(newVolume));

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_disambiguate_if_volume_folder_exists()
        {
            var newVolume = new Volume
            {
                ForeignVolumeId = "ce09ea31-3d4a-4487-a797-e315175457a0",
                Path = @"C:\Test\Music\Name1",
            };

            _fakeVolume.Metadata = Builder<VolumeMetadata>.CreateNew().With(x => x.Disambiguation = "Disambiguation").Build();

            GivenValidVolume(newVolume.ForeignVolumeId);
            GivenValidPath();

            Mocker.GetMock<IVolumeService>()
                .Setup(x => x.VolumePathExists(newVolume.Path))
                .Returns(true);

            var volume = Subject.AddVolume(newVolume);
            volume.Path.Should().Be(newVolume.Path + " (Disambiguation)");
        }

        [Test]
        public void should_disambiguate_with_numbers_if_volume_folder_still_exists()
        {
            var newVolume = new Volume
            {
                ForeignVolumeId = "ce09ea31-3d4a-4487-a797-e315175457a0",
                Path = @"C:\Test\Music\Name1",
            };

            _fakeVolume.Metadata = Builder<VolumeMetadata>.CreateNew().With(x => x.Disambiguation = "Disambiguation").Build();

            GivenValidVolume(newVolume.ForeignVolumeId);
            GivenValidPath();

            Mocker.GetMock<IVolumeService>()
                .Setup(x => x.VolumePathExists(newVolume.Path))
                .Returns(true);

            Mocker.GetMock<IVolumeService>()
                .Setup(x => x.VolumePathExists(newVolume.Path + " (Disambiguation)"))
                .Returns(true);

            Mocker.GetMock<IVolumeService>()
                .Setup(x => x.VolumePathExists(newVolume.Path + " (Disambiguation) (1)"))
                .Returns(true);

            Mocker.GetMock<IVolumeService>()
                .Setup(x => x.VolumePathExists(newVolume.Path + " (Disambiguation) (2)"))
                .Returns(true);

            var volume = Subject.AddVolume(newVolume);
            volume.Path.Should().Be(newVolume.Path + " (Disambiguation) (3)");
        }

        [Test]
        public void should_disambiguate_with_numbers_if_volume_folder_exists_and_no_disambiguation()
        {
            var newVolume = new Volume
            {
                ForeignVolumeId = "ce09ea31-3d4a-4487-a797-e315175457a0",
                Path = @"C:\Test\Music\Name1",
            };

            _fakeVolume.Metadata = Builder<VolumeMetadata>.CreateNew().With(x => x.Disambiguation = string.Empty).Build();

            GivenValidVolume(newVolume.ForeignVolumeId);
            GivenValidPath();

            Mocker.GetMock<IVolumeService>()
                .Setup(x => x.VolumePathExists(newVolume.Path))
                .Returns(true);

            Mocker.GetMock<IVolumeService>()
                .Setup(x => x.VolumePathExists(newVolume.Path + " (1)"))
                .Returns(true);

            Mocker.GetMock<IVolumeService>()
                .Setup(x => x.VolumePathExists(newVolume.Path + " (2)"))
                .Returns(true);

            var volume = Subject.AddVolume(newVolume);
            volume.Path.Should().Be(newVolume.Path + " (3)");
        }
    }
}
