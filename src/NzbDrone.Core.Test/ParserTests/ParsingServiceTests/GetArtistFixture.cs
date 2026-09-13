using Moq;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests.ParsingServiceTests
{
    [TestFixture]
    public class GetVolumeFixture : CoreTest<ParsingService>
    {
        [Test]
        public void should_use_passed_in_title_when_it_cannot_be_parsed()
        {
            const string title = "30 Rock";

            Subject.GetVolume(title);

            Mocker.GetMock<IVolumeService>()
                  .Verify(s => s.FindByName(title), Times.Once());
        }

        [Test]
        public void should_use_parsed_volume_title()
        {
            const string title = "30 Rock - Get Some [FLAC]";

            Subject.GetVolume(title);

            Mocker.GetMock<IVolumeService>()
                  .Verify(s => s.FindByName(Parser.Parser.ParseIssueTitle(title).VolumeName), Times.Once());
        }
    }
}
