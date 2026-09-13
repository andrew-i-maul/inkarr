using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.TrackFileMovingServiceTests
{
    [TestFixture]
    public class MediaFileServiceFixture : CoreTest<MediaFileService>
    {
        private Issue _issue;
        private List<IssueFile> _trackFiles;

        [SetUp]
        public void Setup()
        {
            _issue = Builder<Issue>.CreateNew()
                         .Build();

            _trackFiles = Builder<IssueFile>.CreateListOfSize(3)
                                               .TheFirst(2)
                                               .With(f => f.EditionId = _issue.Id)
                                               .TheNext(1)
                                               .With(f => f.EditionId = 0)
                                               .Build().ToList();
        }

        [Test]
        public void should_throw_trackFileDeletedEvent_for_each_mapped_track_on_deletemany()
        {
            Subject.DeleteMany(_trackFiles, DeleteMediaFileReason.Manual);

            VerifyEventPublished<IssueFileDeletedEvent>(Times.Exactly(2));
        }

        [Test]
        public void should_throw_trackFileDeletedEvent_for_mapped_track_on_delete()
        {
            Subject.Delete(_trackFiles[0], DeleteMediaFileReason.Manual);

            VerifyEventPublished<IssueFileDeletedEvent>(Times.Once());
        }

        [Test]
        public void should_throw_trackFileAddedEvent_for_each_track_added_on_addmany()
        {
            Subject.AddMany(_trackFiles);

            VerifyEventPublished<IssueFileAddedEvent>(Times.Exactly(3));
        }

        [Test]
        public void should_throw_trackFileAddedEvent_for_track_added()
        {
            Subject.Add(_trackFiles[0]);

            VerifyEventPublished<IssueFileAddedEvent>(Times.Once());
        }
    }
}
