using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MusicTests.VolumeServiceTests
{
    [TestFixture]

    public class FindByNameInexactFixture : CoreTest<VolumeService>
    {
        private List<Volume> _volumes;

        private Volume CreateVolume(string name)
        {
            return Builder<Volume>.CreateNew()
                .With(a => a.Name = name)
                .With(a => a.CleanName = Parser.Parser.CleanVolumeName(name))
                .With(a => a.ForeignVolumeId = name)
                .BuildNew();
        }

        [SetUp]
        public void Setup()
        {
            _volumes = new List<Volume>();
            _volumes.Add(CreateVolume("The Black Eyed Peas"));
            _volumes.Add(CreateVolume("The Black Keys"));

            Mocker.GetMock<IVolumeRepository>()
                .Setup(s => s.All())
                .Returns(_volumes);
        }

        [TestCase("The Black Eyd Peas", "The Black Eyed Peas")]
        [TestCase("The Black eys", "The Black Keys")]
        public void should_find_volume_in_db_by_name_inexact(string name, string expected)
        {
            var volume = Subject.FindByNameInexact(name);

            volume.Should().NotBeNull();
            volume.Name.Should().Be(expected);
        }

        [TestCase("The Black Peas")]
        public void should_not_find_volume_in_db_by_ambiguous_name(string name)
        {
            var volume = Subject.FindByNameInexact(name);

            volume.Should().BeNull();
        }
    }
}
