using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.VolumeStats;

namespace NzbDrone.Core.Test.VolumeStatsTests
{
    [TestFixture]
    public class VolumeStatisticsFixture : DbTest<VolumeStatisticsRepository, Volume>
    {
        private Volume _volume;
        private Issue _issue;
        private Edition _edition;
        private List<IssueFile> _issueFiles;

        [SetUp]
        public void Setup()
        {
            _volume = Builder<Volume>.CreateNew()
                .With(a => a.VolumeMetadataId = 10)
                .BuildNew();
            Db.Insert(_volume);

            _issue = Builder<Issue>.CreateNew()
                .With(e => e.ReleaseDate = DateTime.Today.AddDays(-5))
                .With(e => e.VolumeMetadataId = 10)
                .BuildNew();
            Db.Insert(_issue);

            _edition = Builder<Edition>.CreateNew()
                .With(e => e.IssueId = _issue.Id)
                .With(e => e.Monitored = true)
                .BuildNew();
            Db.Insert(_edition);

            _issueFiles = Builder<IssueFile>.CreateListOfSize(2)
                .All()
                .With(x => x.Id = 0)
                .With(e => e.Volume = _volume)
                .With(e => e.Edition = _edition)
                .With(e => e.EditionId = _edition.Id)
                .With(e => e.Quality = new QualityModel(Quality.MP3))
                .BuildList();
        }

        private void GivenIssueFile()
        {
            Db.Insert(_issueFiles[0]);
        }

        private void GivenTwoIssueFiles()
        {
            Db.InsertMany(_issueFiles);
        }

        [Test]
        public void should_get_stats_for_volume()
        {
            var stats = Subject.VolumeStatistics();

            stats.Should().HaveCount(1);
        }

        [Test]
        public void should_not_include_unmonitored_issue_in_issue_count()
        {
            var stats = Subject.VolumeStatistics();

            stats.Should().HaveCount(1);
            stats.First().IssueCount.Should().Be(0);
        }

        [Test]
        public void should_include_unmonitored_issue_with_file_in_issue_count()
        {
            GivenIssueFile();

            var stats = Subject.VolumeStatistics();

            stats.Should().HaveCount(1);
            stats.First().IssueCount.Should().Be(1);
        }

        [Test]
        public void should_have_size_on_disk_of_zero_when_no_issue_file()
        {
            var stats = Subject.VolumeStatistics();

            stats.Should().HaveCount(1);
            stats.First().SizeOnDisk.Should().Be(0);
        }

        [Test]
        public void should_have_size_on_disk_when_issue_file_exists()
        {
            GivenIssueFile();

            var stats = Subject.VolumeStatistics();

            stats.Should().HaveCount(1);
            stats.First().SizeOnDisk.Should().Be(_issueFiles[0].Size);
        }

        [Test]
        public void should_count_issue_with_two_files_as_one_issue()
        {
            GivenTwoIssueFiles();

            var stats = Subject.VolumeStatistics();

            Db.All<IssueFile>().Should().HaveCount(2);
            stats.Should().HaveCount(1);

            var issueStats = stats.First();

            issueStats.TotalIssueCount.Should().Be(1);
            issueStats.IssueCount.Should().Be(1);
            issueStats.AvailableIssueCount.Should().Be(1);
            issueStats.SizeOnDisk.Should().Be(_issueFiles.Sum(x => x.Size));
            issueStats.IssueFileCount.Should().Be(2);
        }
    }
}
