using FluentAssertions;
using NUnit.Framework;

namespace NzbDrone.Integration.Test.ApiTests
{
    [TestFixture]
    [Ignore("Waiting for metadata to be back again", Until = "2026-01-15 00:00:00Z")]
    public class VolumeLookupFixture : IntegrationTest
    {
        [TestCase("Robert Harris", "Robert Harris")]
        [TestCase("Philip W. Errington", "Philip W. Errington")]
        public void lookup_new_volume_by_name(string term, string name)
        {
            var volume = Volume.Lookup(term);

            volume.Should().NotBeEmpty();
            volume.Should().Contain(c => c.VolumeName == name);
        }

        [Test]
        public void lookup_new_volume_by_goodreads_issue_id()
        {
            var volume = Volume.Lookup("edition:2");

            volume.Should().NotBeEmpty();
            volume.Should().Contain(c => c.VolumeName == "J.K. Rowling");
        }
    }
}
