using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerSearchTests
{
    [TestFixture]
    public class VolumeSearchServiceFixture : CoreTest<VolumeSearchService>
    {
        private Volume _volume;

        [SetUp]
        public void Setup()
        {
            _volume = new Volume();

            Mocker.GetMock<IVolumeService>()
                .Setup(s => s.GetVolume(It.IsAny<int>()))
                .Returns(_volume);

            Mocker.GetMock<ISearchForReleases>()
                .Setup(s => s.VolumeSearch(_volume.Id, false, true, false))
                .Returns(Task.FromResult(new List<DownloadDecision>()));

            Mocker.GetMock<IProcessDownloadDecisions>()
                .Setup(s => s.ProcessDecisions(It.IsAny<List<DownloadDecision>>()))
                .Returns(Task.FromResult(new ProcessedDecisions(new List<DownloadDecision>(), new List<DownloadDecision>(), new List<DownloadDecision>())));
        }

        [Test]
        public void should_only_include_monitored_issues()
        {
            _volume.Issues = new List<Issue>
            {
                new Issue { Monitored = false },
                new Issue { Monitored = true }
            };

            Subject.Execute(new VolumeSearchCommand { VolumeId = _volume.Id, Trigger = CommandTrigger.Manual });

            Mocker.GetMock<ISearchForReleases>()
                .Verify(v => v.VolumeSearch(_volume.Id, false, true, false),
                    Times.Exactly(_volume.Issues.Value.Count(s => s.Monitored)));
        }
    }
}
