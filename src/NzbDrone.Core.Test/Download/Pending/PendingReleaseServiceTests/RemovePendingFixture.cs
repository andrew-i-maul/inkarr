using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Crypto;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Download.Pending.PendingReleaseServiceTests
{
    [TestFixture]
    public class RemovePendingFixture : CoreTest<PendingReleaseService>
    {
        private List<PendingRelease> _pending;
        private Issue _issue;

        [SetUp]
        public void Setup()
        {
            _pending = new List<PendingRelease>();

            _issue = Builder<Issue>.CreateNew()
                                       .Build();

            Mocker.GetMock<IPendingReleaseRepository>()
                 .Setup(s => s.AllByVolumeId(It.IsAny<int>()))
                 .Returns(_pending);

            Mocker.GetMock<IPendingReleaseRepository>()
                  .Setup(s => s.All())
                  .Returns(_pending);

            Mocker.GetMock<IVolumeService>()
                  .Setup(s => s.GetVolume(It.IsAny<int>()))
                  .Returns(new Volume());

            Mocker.GetMock<IVolumeService>()
                  .Setup(s => s.GetVolumes(It.IsAny<IEnumerable<int>>()))
                  .Returns(new List<Volume> { new Volume() });

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.GetIssues(It.IsAny<ParsedIssueInfo>(), It.IsAny<Volume>(), null))
                  .Returns(new List<Issue> { _issue });
        }

        private void AddPending(int id, string issue)
        {
            _pending.Add(new PendingRelease
            {
                Id = id,
                Title = "Volume.Title-Issue.Title.abc-Inkarr",
                ParsedIssueInfo = new ParsedIssueInfo { IssueTitle = issue },
                Release = Builder<ReleaseInfo>.CreateNew().Build()
            });
        }

        [Test]
        public void should_remove_same_release()
        {
            AddPending(id: 1, issue: "Issue");

            var queueId = HashConverter.GetHashInt31(string.Format("pending-{0}-issue{1}", 1, _issue.Id));

            Subject.RemovePendingQueueItems(queueId);

            AssertRemoved(1);
        }

        [Test]
        public void should_remove_multiple_releases_release()
        {
            AddPending(id: 1, issue: "Issue 1");
            AddPending(id: 2, issue: "Issue 2");
            AddPending(id: 3, issue: "Issue 3");
            AddPending(id: 4, issue: "Issue 3");

            var queueId = HashConverter.GetHashInt31(string.Format("pending-{0}-issue{1}", 3, _issue.Id));

            Subject.RemovePendingQueueItems(queueId);

            AssertRemoved(3, 4);
        }

        [Test]
        public void should_not_remove_diffrent_issues()
        {
            AddPending(id: 1, issue: "Issue 1");
            AddPending(id: 2, issue: "Issue 1");
            AddPending(id: 3, issue: "Issue 2");
            AddPending(id: 4, issue: "Issue 3");

            var queueId = HashConverter.GetHashInt31(string.Format("pending-{0}-issue{1}", 1, _issue.Id));

            Subject.RemovePendingQueueItems(queueId);

            AssertRemoved(1, 2);
        }

        private void AssertRemoved(params int[] ids)
        {
            Mocker.GetMock<IPendingReleaseRepository>().Verify(c => c.DeleteMany(It.Is<IEnumerable<int>>(s => s.SequenceEqual(ids))));
        }
    }
}
