using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.IssueImport.Identification;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.IssueImport.Identification
{
    [TestFixture]
    public class DistanceCalculatorFixture : TestBase
    {
        [Test]
        public void should_reverse_single_reversed_volume()
        {
            var input = new List<string> { "Last, First" };
            var volumes = DistanceCalculator.GetVolumeVariants(input);

            volumes.Should().Contain("First Last");
        }

        [Test]
        public void should_reverse_two_reversed_volume()
        {
            var input = new List<string>
            {
                "Last, First",
                "Last2, First2"
            };

            var volumes = DistanceCalculator.GetVolumeVariants(input);

            volumes.Should().HaveCount(4);
            volumes.Should().Contain("First Last");
            volumes.Should().Contain("First2 Last2");
            volumes.Should().Contain("Last, First");
            volumes.Should().Contain("Last2, First2");
        }

        [Test]
        public void should_not_reverse_single_volume()
        {
            var input = new List<string> { "First Last" };
            var volumes = DistanceCalculator.GetVolumeVariants(input);

            volumes.Should().HaveCount(1);
            volumes.Should().Contain("First Last");
        }

        [TestCase("First1 Last1, First2 Last2", "First1 Last1", "First2 Last2")]
        [TestCase("First1 Last1; First2 Last2", "First1 Last1", "First2 Last2")]
        [TestCase("First1 Last1 & First2 Last2", "First1 Last1", "First2 Last2")]
        [TestCase("First1 Last1 / First2 Last2", "First1 Last1", "First2 Last2")]
        [TestCase("First1 Last1 and First2 Last2", "First1 Last1", "First2 Last2")]
        public void should_split_concatenated_volume(string inputString, string first, string second)
        {
            var input = new List<string> { inputString };
            var volumes = DistanceCalculator.GetVolumeVariants(input);

            volumes.Should().Contain(inputString);
            volumes.Should().Contain(first);
            volumes.Should().Contain(second);
            volumes.Should().HaveCount(3);
        }

        [Test]
        public void should_split_concatenated_with_trailing_and()
        {
            var inputString = "First Last, First2 Last2 & First3 Last3";
            var input = new List<string> { inputString };
            var volumes = DistanceCalculator.GetVolumeVariants(input);

            volumes.Should().Contain(inputString);
            volumes.Should().Contain("First Last");
            volumes.Should().Contain("First2 Last2");
            volumes.Should().Contain("First3 Last3");
            volumes.Should().HaveCount(4);
        }

        [Test]
        public void should_not_split_if_multiple_input()
        {
            var input = new List<string>
            {
                "First Last",
                "Second Third, Fourth Fifth"
            };

            var volumes = DistanceCalculator.GetVolumeVariants(input);

            volumes.Should().HaveCount(2);
            volumes.Should().Contain("First Last");
            volumes.Should().Contain("Second Third, Fourth Fifth");
        }
    }
}
