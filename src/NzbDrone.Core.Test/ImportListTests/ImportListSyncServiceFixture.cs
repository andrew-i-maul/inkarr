using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.ImportLists.Exclusions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.MetadataSource.Goodreads;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ImportListTests
{
    public class ImportListSyncServiceFixture : CoreTest<ImportListSyncService>
    {
        private List<ImportListItemInfo> _importListReports;

        [SetUp]
        public void SetUp()
        {
            var importListItem1 = new ImportListItemInfo
            {
                Volume = "Linkin Park"
            };

            _importListReports = new List<ImportListItemInfo> { importListItem1 };

            var mockImportList = new Mock<IImportList>();

            Mocker.GetMock<IFetchAndParseImportList>()
                .Setup(v => v.Fetch())
                .Returns(_importListReports);

            Mocker.GetMock<IGoodreadsSearchProxy>()
                .Setup(v => v.Search(It.IsAny<string>()))
                .Returns(new List<SearchJsonResource>());

            Mocker.GetMock<IGoodreadsProxy>()
                .Setup(v => v.GetIssueInfo(It.IsAny<string>(), true))
                .Returns<string, bool>((id, useCache) => Builder<Issue>
                .CreateNew()
                .With(b => b.VolumeMetadata = Builder<VolumeMetadata>.CreateNew().Build())
                .With(b => b.ForeignIssueId = "4321")
                .With(b => b.Editions = Builder<Edition>
                    .CreateListOfSize(1)
                    .TheFirst(1)
                    .With(e => e.ForeignEditionId = id.ToString())
                    .With(e => e.Monitored = true)
                    .BuildList())
                .Build());

            Mocker.GetMock<IImportListFactory>()
                .Setup(v => v.Get(It.IsAny<int>()))
                .Returns(new ImportListDefinition { ShouldMonitor = ImportListMonitorType.SpecificIssue });

            Mocker.GetMock<IImportListFactory>()
                .Setup(v => v.AutomaticAddEnabled(It.IsAny<bool>()))
                .Returns(new List<IImportList> { mockImportList.Object });

            Mocker.GetMock<IFetchAndParseImportList>()
                .Setup(v => v.Fetch())
                .Returns(_importListReports);

            Mocker.GetMock<IImportListExclusionService>()
                .Setup(v => v.All())
                .Returns(new List<ImportListExclusion>());

            Mocker.GetMock<IAddIssueService>()
                .Setup(v => v.AddIssues(It.IsAny<List<Issue>>(), false))
                .Returns<List<Issue>, bool>((x, y) => x);

            Mocker.GetMock<IAddVolumeService>()
                .Setup(v => v.AddVolumes(It.IsAny<List<Volume>>(), false))
                .Returns<List<Volume>, bool>((x, y) => x);
        }

        private void WithIssue()
        {
            _importListReports.First().Issue = "Meteora";
        }

        private void WithVolumeId()
        {
            _importListReports.First().VolumeGoodreadsId = "f59c5520-5f46-4d2c-b2c4-822eabf53419";
        }

        private void WithIssueId()
        {
            _importListReports.First().EditionGoodreadsId = "1234";
        }

        private void WithSecondIssue()
        {
            var importListItem2 = new ImportListItemInfo
            {
                Volume = "Linkin Park",
                VolumeGoodreadsId = "f59c5520-5f46-4d2c-b2c4-822eabf53419",
                Issue = "Meteora 2",
                EditionGoodreadsId = "5678",
                IssueGoodreadsId = "8765"
            };
            _importListReports.Add(importListItem2);
        }

        private void WithExistingVolume()
        {
            Mocker.GetMock<IVolumeService>()
                .Setup(v => v.FindById(_importListReports.First().VolumeGoodreadsId))
                .Returns(new Volume { Id = 1, ForeignVolumeId = _importListReports.First().VolumeGoodreadsId });
        }

        private void WithExistingIssue()
        {
            Mocker.GetMock<IIssueService>()
                .Setup(v => v.FindById("4321"))
                .Returns(new Issue { Id = 1, ForeignIssueId = _importListReports.First().IssueGoodreadsId });
        }

        private void WithExcludedVolume()
        {
            Mocker.GetMock<IImportListExclusionService>()
                .Setup(v => v.All())
                .Returns(new List<ImportListExclusion>
                {
                    new ImportListExclusion
                    {
                        ForeignId = "f59c5520-5f46-4d2c-b2c4-822eabf53419"
                    }
                });
        }

        private void WithExcludedIssue()
        {
            Mocker.GetMock<IImportListExclusionService>()
                .Setup(v => v.All())
                .Returns(new List<ImportListExclusion>
                {
                    new ImportListExclusion
                    {
                        ForeignId = "4321"
                    }
                });
        }

        private void WithMonitorType(ImportListMonitorType monitor)
        {
            Mocker.GetMock<IImportListFactory>()
                .Setup(v => v.Get(It.IsAny<int>()))
                .Returns(new ImportListDefinition { ShouldMonitor = monitor });
        }

        [Test]
        public void should_search_if_volume_title_and_no_volume_id()
        {
            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IGoodreadsSearchProxy>()
                .Verify(v => v.Search(It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_not_search_if_volume_title_and_volume_id()
        {
            WithVolumeId();
            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<ISearchForNewVolume>()
                .Verify(v => v.SearchForNewVolume(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_search_if_issue_title_and_no_issue_id()
        {
            WithIssue();
            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IGoodreadsSearchProxy>()
                .Verify(v => v.Search(It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_not_search_if_issue_title_and_issue_id()
        {
            WithVolumeId();
            WithIssueId();
            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IGoodreadsSearchProxy>()
                .Verify(v => v.Search(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_not_search_if_all_info()
        {
            WithVolumeId();
            WithIssue();
            WithIssueId();
            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IGoodreadsSearchProxy>()
                .Verify(v => v.Search(It.IsAny<string>()), Times.Never());

            Mocker.GetMock<IGoodreadsSearchProxy>()
                .Verify(v => v.Search(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_not_add_if_existing_volume()
        {
            WithVolumeId();
            WithExistingVolume();

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddVolumeService>()
                .Verify(v => v.AddVolumes(It.Is<List<Volume>>(t => t.Count == 0), false));
        }

        [Test]
        public void should_not_add_if_existing_issue()
        {
            WithIssueId();
            WithExistingIssue();

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddVolumeService>()
                .Verify(v => v.AddVolumes(It.Is<List<Volume>>(t => t.Count == 0), false));
        }

        [Test]
        public void should_add_if_existing_volume_but_new_issue()
        {
            WithIssueId();
            WithVolumeId();
            WithExistingVolume();

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddIssueService>()
                .Verify(v => v.AddIssues(It.Is<List<Issue>>(t => t.Count == 1), false));
        }

        [TestCase(ImportListMonitorType.None, false)]
        [TestCase(ImportListMonitorType.SpecificIssue, true)]
        [TestCase(ImportListMonitorType.EntireVolume, true)]
        public void should_add_if_not_existing_volume(ImportListMonitorType monitor, bool expectedVolumeMonitored)
        {
            WithVolumeId();
            WithMonitorType(monitor);

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddVolumeService>()
                .Verify(v => v.AddVolumes(It.Is<List<Volume>>(t => t.Count == 1 && t.First().Monitored == expectedVolumeMonitored), false));
        }

        [TestCase(ImportListMonitorType.None, false)]
        [TestCase(ImportListMonitorType.SpecificIssue, true)]
        [TestCase(ImportListMonitorType.EntireVolume, true)]
        public void should_add_if_not_existing_issue(ImportListMonitorType monitor, bool expectedIssueMonitored)
        {
            WithIssueId();
            WithMonitorType(monitor);

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddIssueService>()
                .Verify(v => v.AddIssues(It.Is<List<Issue>>(t => t.Count == 1 && t.First().Monitored == expectedIssueMonitored), false));
        }

        [Test]
        public void should_not_add_volume_if_excluded_volume()
        {
            WithVolumeId();
            WithExcludedVolume();

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddVolumeService>()
                .Verify(v => v.AddVolumes(It.Is<List<Volume>>(t => t.Count == 0), false));
        }

        [Test]
        public void should_not_add_issue_if_excluded_issue()
        {
            WithIssueId();
            WithExcludedIssue();

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddIssueService>()
                .Verify(v => v.AddIssues(It.Is<List<Issue>>(t => t.Count == 0), false));
        }

        [Test]
        public void should_not_add_issue_if_excluded_volume()
        {
            WithIssueId();
            WithVolumeId();
            WithExcludedVolume();

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddIssueService>()
                .Verify(v => v.AddIssues(It.Is<List<Issue>>(t => t.Count == 0), false));
        }

        [TestCase(ImportListMonitorType.None, 0, false)]
        [TestCase(ImportListMonitorType.SpecificIssue, 2, true)]
        [TestCase(ImportListMonitorType.EntireVolume, 0, true)]
        public void should_add_two_issues(ImportListMonitorType monitor, int expectedIssuesMonitored, bool expectedVolumeMonitored)
        {
            WithIssue();
            WithIssueId();
            WithSecondIssue();
            WithVolumeId();
            WithMonitorType(monitor);

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddIssueService>()
                .Verify(v => v.AddIssues(It.Is<List<Issue>>(t => t.Count == 2), false));
            Mocker.GetMock<IAddVolumeService>()
                .Verify(v => v.AddVolumes(It.Is<List<Volume>>(t => t.Count == 1 &&
                                                                   t.First().AddOptions.IssuesToMonitor.Count == expectedIssuesMonitored &&
                                                                   t.First().Monitored == expectedVolumeMonitored), false));
        }

        [Test]
        public void should_not_fetch_if_no_lists_are_enabled()
        {
            Mocker.GetMock<IImportListFactory>()
                .Setup(v => v.AutomaticAddEnabled(It.IsAny<bool>()))
                .Returns(new List<IImportList>());

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IFetchAndParseImportList>()
                .Verify(v => v.Fetch(), Times.Never);
        }

        [Test]
        public void should_not_process_if_no_items_are_returned()
        {
            Mocker.GetMock<IFetchAndParseImportList>()
                .Setup(v => v.Fetch())
                .Returns(new List<ImportListItemInfo>());

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IImportListExclusionService>()
                .Verify(v => v.All(), Times.Never);
        }
    }
}
