using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Download.Pending.PendingReleaseServiceTests
{
    [TestFixture]
    public class RemoveRejectedFixture : CoreTest<PendingReleaseService>
    {
        private DownloadDecision _temporarilyRejected;
        private Volume _volume;
        private Issue _issue;
        private QualityProfile _profile;
        private ReleaseInfo _release;
        private ParsedIssueInfo _parsedIssueInfo;
        private RemoteIssue _remoteIssue;

        [SetUp]
        public void Setup()
        {
            _volume = Builder<Volume>.CreateNew()
                                     .Build();

            _issue = Builder<Issue>.CreateNew()
                                       .Build();

            _profile = new QualityProfile
            {
                Name = "Test",
                Cutoff = Quality.MP3.Id,
                Items = new List<QualityProfileQualityItem>
                                   {
                                       new QualityProfileQualityItem { Allowed = true, Quality = Quality.MP3 },
                                       new QualityProfileQualityItem { Allowed = true, Quality = Quality.MP3 },
                                       new QualityProfileQualityItem { Allowed = true, Quality = Quality.MP3 }
                                   },
            };

            _volume.QualityProfile = new LazyLoaded<QualityProfile>(_profile);

            _release = Builder<ReleaseInfo>.CreateNew().Build();

            _parsedIssueInfo = Builder<ParsedIssueInfo>.CreateNew().Build();
            _parsedIssueInfo.Quality = new QualityModel(Quality.MP3);

            _remoteIssue = new RemoteIssue();
            _remoteIssue.Issues = new List<Issue> { _issue };
            _remoteIssue.Volume = _volume;
            _remoteIssue.ParsedIssueInfo = _parsedIssueInfo;
            _remoteIssue.Release = _release;

            _temporarilyRejected = new DownloadDecision(_remoteIssue, new Rejection("Temp Rejected", RejectionType.Temporary));

            Mocker.GetMock<IPendingReleaseRepository>()
                  .Setup(s => s.All())
                  .Returns(new List<PendingRelease>());

            Mocker.GetMock<IVolumeService>()
                  .Setup(s => s.GetVolume(It.IsAny<int>()))
                  .Returns(_volume);

            Mocker.GetMock<IVolumeService>()
                  .Setup(s => s.GetVolumes(It.IsAny<IEnumerable<int>>()))
                  .Returns(new List<Volume> { _volume });

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.GetIssues(It.IsAny<ParsedIssueInfo>(), _volume, null))
                  .Returns(new List<Issue> { _issue });

            Mocker.GetMock<IPrioritizeDownloadDecision>()
                  .Setup(s => s.PrioritizeDecisions(It.IsAny<List<DownloadDecision>>()))
                  .Returns((List<DownloadDecision> d) => d);
        }

        private void GivenHeldRelease(string title, string indexer, DateTime publishDate)
        {
            var release = _release.JsonClone();
            release.Indexer = indexer;
            release.PublishDate = publishDate;

            var heldReleases = Builder<PendingRelease>.CreateListOfSize(1)
                                                   .All()
                                                   .With(h => h.VolumeId = _volume.Id)
                                                   .With(h => h.Title = title)
                                                   .With(h => h.Release = release)
                                                   .Build();

            Mocker.GetMock<IPendingReleaseRepository>()
                  .Setup(s => s.All())
                  .Returns(heldReleases);
        }

        [Test]
        public void should_remove_if_it_is_the_same_release_from_the_same_indexer()
        {
            GivenHeldRelease(_release.Title, _release.Indexer, _release.PublishDate);

            Subject.Handle(new RssSyncCompleteEvent(new ProcessedDecisions(new List<DownloadDecision>(),
                                                                           new List<DownloadDecision>(),
                                                                           new List<DownloadDecision> { _temporarilyRejected })));

            VerifyDelete();
        }

        [Test]
        public void should_not_remove_if_title_is_different()
        {
            GivenHeldRelease(_release.Title + "-RP", _release.Indexer, _release.PublishDate);

            Subject.Handle(new RssSyncCompleteEvent(new ProcessedDecisions(new List<DownloadDecision>(),
                                                                           new List<DownloadDecision>(),
                                                                           new List<DownloadDecision> { _temporarilyRejected })));

            VerifyNoDelete();
        }

        [Test]
        public void should_not_remove_if_indexer_is_different()
        {
            GivenHeldRelease(_release.Title, "AnotherIndexer", _release.PublishDate);

            Subject.Handle(new RssSyncCompleteEvent(new ProcessedDecisions(new List<DownloadDecision>(),
                                                                           new List<DownloadDecision>(),
                                                                           new List<DownloadDecision> { _temporarilyRejected })));

            VerifyNoDelete();
        }

        [Test]
        public void should_not_remove_if_publish_date_is_different()
        {
            GivenHeldRelease(_release.Title, _release.Indexer, _release.PublishDate.AddHours(1));

            Subject.Handle(new RssSyncCompleteEvent(new ProcessedDecisions(new List<DownloadDecision>(),
                                                                           new List<DownloadDecision>(),
                                                                           new List<DownloadDecision> { _temporarilyRejected })));

            VerifyNoDelete();
        }

        private void VerifyDelete()
        {
            Mocker.GetMock<IPendingReleaseRepository>()
                .Verify(v => v.Delete(It.IsAny<PendingRelease>()), Times.Once());
        }

        private void VerifyNoDelete()
        {
            Mocker.GetMock<IPendingReleaseRepository>()
                .Verify(v => v.Delete(It.IsAny<PendingRelease>()), Times.Never());
        }
    }
}
