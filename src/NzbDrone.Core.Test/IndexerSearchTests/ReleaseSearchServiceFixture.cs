using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerSearchTests
{
        public class ReleaseSearchServiceFixture : CoreTest<ReleaseSearchService>
    {
        private Mock<IIndexer> _mockIndexer;
        private Volume _volume;
        private Issue _firstIssue;

        [SetUp]
        public void SetUp()
        {
            _mockIndexer = Mocker.GetMock<IIndexer>();
            _mockIndexer.SetupGet(s => s.Definition).Returns(new IndexerDefinition { Id = 1 });
            _mockIndexer.SetupGet(s => s.SupportsSearch).Returns(true);

            Mocker.GetMock<IIndexerFactory>()
                  .Setup(s => s.AutomaticSearchEnabled(true))
                  .Returns(new List<IIndexer> { _mockIndexer.Object });

            Mocker.GetMock<IMakeDownloadDecision>()
                .Setup(s => s.GetSearchDecision(It.IsAny<List<Parser.Model.ReleaseInfo>>(), It.IsAny<SearchCriteriaBase>()))
                .Returns(new List<DownloadDecision>());

            _volume = Builder<Volume>.CreateNew()
                .With(v => v.Monitored = true)
                .Build();

            _firstIssue = Builder<Issue>.CreateNew()
                .With(e => e.Volume = _volume)
                .Build();

            var edition = Builder<Edition>.CreateNew()
                .With(e => e.Issue = _firstIssue)
                .With(e => e.Monitored = true)
                .Build();

            _firstIssue.Editions = new List<Edition> { edition };

            Mocker.GetMock<IVolumeService>()
                .Setup(v => v.GetVolume(_volume.Id))
                .Returns(_volume);
        }

        private List<SearchCriteriaBase> WatchForSearchCriteria()
        {
            var result = new List<SearchCriteriaBase>();

            _mockIndexer.Setup(v => v.Fetch(It.IsAny<IssueSearchCriteria>()))
                .Callback<IssueSearchCriteria>(s => result.Add(s))
                .Returns(Task.FromResult<IList<Parser.Model.ReleaseInfo>>(new List<Parser.Model.ReleaseInfo>()));

            return result;
        }

        [Test]
        public async Task Tags_IndexerTags_VolumeNoTags_IndexerNotIncluded()
        {
            _mockIndexer.SetupGet(s => s.Definition).Returns(new IndexerDefinition
            {
                Id = 1,
                Tags = new HashSet<int> { 3 }
            });

            var allCriteria = WatchForSearchCriteria();

            await Subject.IssueSearch(_firstIssue, false, true, false);

            var criteria = allCriteria.OfType<IssueSearchCriteria>().ToList();

            criteria.Count.Should().Be(0);
        }

        [Test]
        public async Task Tags_IndexerNoTags_VolumeTags_IndexerIncluded()
        {
            _mockIndexer.SetupGet(s => s.Definition).Returns(new IndexerDefinition
            {
                Id = 1
            });

            _volume = Builder<Volume>.CreateNew()
                .With(v => v.Monitored = true)
                .With(v => v.Tags = new HashSet<int> { 3 })
                .Build();

            Mocker.GetMock<IVolumeService>()
                .Setup(v => v.GetVolume(_volume.Id))
                .Returns(_volume);

            var allCriteria = WatchForSearchCriteria();

            await Subject.IssueSearch(_firstIssue, false, true, false);

            var criteria = allCriteria.OfType<IssueSearchCriteria>().ToList();

            criteria.Count.Should().Be(1);
        }

        [Test]
        public async Task Tags_IndexerAndVolumeTagsMatch_IndexerIncluded()
        {
            _mockIndexer.SetupGet(s => s.Definition).Returns(new IndexerDefinition
            {
                Id = 1,
                Tags = new HashSet<int> { 1, 2, 3 }
            });

            _volume = Builder<Volume>.CreateNew()
                .With(v => v.Monitored = true)
                .With(v => v.Tags = new HashSet<int> { 3, 4, 5 })
                .Build();

            Mocker.GetMock<IVolumeService>()
                .Setup(v => v.GetVolume(_volume.Id))
                .Returns(_volume);

            var allCriteria = WatchForSearchCriteria();

            await Subject.IssueSearch(_firstIssue, false, true, false);

            var criteria = allCriteria.OfType<IssueSearchCriteria>().ToList();

            criteria.Count.Should().Be(1);
        }

        [Test]
        public async Task Tags_IndexerAndVolumeTagsMismatch_IndexerNotIncluded()
        {
            _mockIndexer.SetupGet(s => s.Definition).Returns(new IndexerDefinition
            {
                Id = 1,
                Tags = new HashSet<int> { 1, 2, 3 }
            });

            _volume = Builder<Volume>.CreateNew()
                .With(v => v.Monitored = true)
                .With(v => v.Tags = new HashSet<int> { 4, 5, 6 })
                .Build();

            Mocker.GetMock<IVolumeService>()
                .Setup(v => v.GetVolume(_volume.Id))
                .Returns(_volume);

            var allCriteria = WatchForSearchCriteria();

            await Subject.IssueSearch(_firstIssue, false, true, false);

            var criteria = allCriteria.OfType<IssueSearchCriteria>().ToList();

            criteria.Count.Should().Be(0);
        }
    }
}
