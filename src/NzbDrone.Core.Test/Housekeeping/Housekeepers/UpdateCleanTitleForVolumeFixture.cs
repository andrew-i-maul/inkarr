using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class UpdateCleanTitleForVolumeFixture : CoreTest<UpdateCleanTitleForVolume>
    {
        [Test]
        public void should_update_clean_title()
        {
            var volume = Builder<Volume>.CreateNew()
                                        .With(s => s.Name = "Full Name")
                                        .With(s => s.CleanName = "unclean")
                                        .Build();

            Mocker.GetMock<IVolumeRepository>()
                 .Setup(s => s.All())
                 .Returns(new[] { volume });

            Subject.Clean();

            Mocker.GetMock<IVolumeRepository>()
                .Verify(v => v.Update(It.Is<Volume>(s => s.CleanName == "fullname")), Times.Once());
        }

        [Test]
        public void should_not_update_unchanged_title()
        {
            var volume = Builder<Volume>.CreateNew()
                                        .With(s => s.Name = "Full Name")
                                        .With(s => s.CleanName = "fullname")
                                        .Build();

            Mocker.GetMock<IVolumeRepository>()
                 .Setup(s => s.All())
                 .Returns(new[] { volume });

            Subject.Clean();

            Mocker.GetMock<IVolumeRepository>()
                .Verify(v => v.Update(It.Is<Volume>(s => s.CleanName == "fullname")), Times.Never());
        }
    }
}
