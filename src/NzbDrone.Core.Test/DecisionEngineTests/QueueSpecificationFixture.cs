using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Queue;
using NzbDrone.Core.Test.CustomFormats;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class QueueSpecificationFixture : CoreTest<QueueSpecification>
    {
        private Volume _volume;
        private Issue _issue;
        private RemoteIssue _remoteIssue;

        private Volume _otherVolume;
        private Issue _otherIssue;

        private ReleaseInfo _releaseInfo;

        [SetUp]
        public void Setup()
        {
            Mocker.Resolve<UpgradableSpecification>();

            CustomFormatsTestHelpers.GivenCustomFormats();

            _volume = Builder<Volume>.CreateNew()
                                     .With(e => e.QualityProfile = new QualityProfile
                                     {
                                         UpgradeAllowed = true,
                                         Items = Qualities.QualityFixture.GetDefaultQualities(),
                                         FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems(),
                                         MinFormatScore = 0
                                     })
                                     .Build();

            _issue = Builder<Issue>.CreateNew()
                                       .With(e => e.VolumeId = _volume.Id)
                                       .Build();

            _otherVolume = Builder<Volume>.CreateNew()
                                          .With(s => s.Id = 2)
                                          .Build();

            _otherIssue = Builder<Issue>.CreateNew()
                                            .With(e => e.VolumeId = _otherVolume.Id)
                                            .With(e => e.Id = 2)
                                            .Build();

            _releaseInfo = Builder<ReleaseInfo>.CreateNew()
                                   .Build();

            _remoteIssue = Builder<RemoteIssue>.CreateNew()
                                                   .With(r => r.Volume = _volume)
                                                   .With(r => r.Issues = new List<Issue> { _issue })
                                                   .With(r => r.ParsedIssueInfo = new ParsedIssueInfo { Quality = new QualityModel(Quality.MP3) })
                                                   .With(r => r.CustomFormats = new List<CustomFormat>())
                                                   .Build();

            Mocker.GetMock<ICustomFormatCalculationService>()
                  .Setup(x => x.ParseCustomFormat(It.IsAny<RemoteIssue>(), It.IsAny<long>()))
                  .Returns(new List<CustomFormat>());
        }

        private void GivenEmptyQueue()
        {
            Mocker.GetMock<IQueueService>()
                .Setup(s => s.GetQueue())
                .Returns(new List<Queue.Queue>());
        }

        private void GivenQueueFormats(List<CustomFormat> formats)
        {
            Mocker.GetMock<ICustomFormatCalculationService>()
                  .Setup(x => x.ParseCustomFormat(It.IsAny<RemoteIssue>(), It.IsAny<long>()))
                  .Returns(formats);
        }

        private void GivenQueue(IEnumerable<RemoteIssue> remoteIssues, TrackedDownloadState trackedDownloadState = TrackedDownloadState.Downloading)
        {
            var queue = remoteIssues.Select(remoteIssue => new Queue.Queue
            {
                RemoteIssue = remoteIssue,
                TrackedDownloadState = trackedDownloadState
            });

            Mocker.GetMock<IQueueService>()
                .Setup(s => s.GetQueue())
                .Returns(queue.ToList());
        }

        [Test]
        public void should_return_true_when_queue_is_empty()
        {
            GivenEmptyQueue();
            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_when_volume_doesnt_match()
        {
            var remoteIssue = Builder<RemoteIssue>.CreateNew()
                                                       .With(r => r.Volume = _otherVolume)
                                                       .With(r => r.Issues = new List<Issue> { _issue })
                                                       .With(r => r.Release = _releaseInfo)
                                                       .With(r => r.CustomFormats = new List<CustomFormat>())
                                                       .Build();

            GivenQueue(new List<RemoteIssue> { remoteIssue });
            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_everything_is_the_same()
        {
            _volume.QualityProfile.Value.Cutoff = Quality.FLAC.Id;

            var remoteIssue = Builder<RemoteIssue>.CreateNew()
                .With(r => r.Volume = _volume)
                .With(r => r.Issues = new List<Issue> { _issue })
                .With(r => r.ParsedIssueInfo = new ParsedIssueInfo
                {
                    Quality = new QualityModel(Quality.MP3)
                })
                .With(r => r.CustomFormats = new List<CustomFormat>())
                .With(r => r.Release = _releaseInfo)
                .Build();

            GivenQueue(new List<RemoteIssue> { remoteIssue });

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_when_quality_in_queue_is_lower()
        {
            _volume.QualityProfile.Value.Cutoff = Quality.MP3.Id;

            var remoteIssue = Builder<RemoteIssue>.CreateNew()
                                                      .With(r => r.Volume = _volume)
                                                      .With(r => r.Issues = new List<Issue> { _issue })
                                                      .With(r => r.ParsedIssueInfo = new ParsedIssueInfo
                                                      {
                                                          Quality = new QualityModel(Quality.AZW3)
                                                      })
                                                      .With(r => r.Release = _releaseInfo)
                                                      .With(r => r.CustomFormats = new List<CustomFormat>())
                                                      .Build();

            GivenQueue(new List<RemoteIssue> { remoteIssue });
            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_when_issue_doesnt_match()
        {
            var remoteIssue = Builder<RemoteIssue>.CreateNew()
                                                      .With(r => r.Volume = _volume)
                                                      .With(r => r.Issues = new List<Issue> { _otherIssue })
                                                      .With(r => r.ParsedIssueInfo = new ParsedIssueInfo
                                                      {
                                                          Quality = new QualityModel(Quality.MP3)
                                                      })
                                                      .With(r => r.Release = _releaseInfo)
                                                      .With(r => r.CustomFormats = new List<CustomFormat>())
                                                      .Build();

            GivenQueue(new List<RemoteIssue> { remoteIssue });
            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_when_qualities_are_the_same_with_higher_custom_format_score()
        {
            _remoteIssue.CustomFormats = new List<CustomFormat> { new CustomFormat("My Format", new ReleaseTitleSpecification { Value = "MP3" }) { Id = 1 } };

            var lowFormat = new List<CustomFormat> { new CustomFormat("Bad Format", new ReleaseTitleSpecification { Value = "MP3" }) { Id = 2 } };

            CustomFormatsTestHelpers.GivenCustomFormats(_remoteIssue.CustomFormats.First(), lowFormat.First());

            _volume.QualityProfile.Value.FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems("My Format");

            GivenQueueFormats(lowFormat);

            var remoteIssue = Builder<RemoteIssue>.CreateNew()
                .With(r => r.Volume = _volume)
                .With(r => r.Issues = new List<Issue> { _issue })
                .With(r => r.ParsedIssueInfo = new ParsedIssueInfo
                {
                    Quality = new QualityModel(Quality.MP3)
                })
                .With(r => r.Release = _releaseInfo)
                .With(r => r.CustomFormats = lowFormat)
                .Build();

            GivenQueue(new List<RemoteIssue> { remoteIssue });
            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_when_qualities_are_the_same()
        {
            var remoteIssue = Builder<RemoteIssue>.CreateNew()
                                                      .With(r => r.Volume = _volume)
                                                      .With(r => r.Issues = new List<Issue> { _issue })
                                                      .With(r => r.ParsedIssueInfo = new ParsedIssueInfo
                                                      {
                                                          Quality = new QualityModel(Quality.MP3)
                                                      })
                                                      .With(r => r.Release = _releaseInfo)
                                                      .With(r => r.CustomFormats = new List<CustomFormat>())
                                                      .Build();

            GivenQueue(new List<RemoteIssue> { remoteIssue });
            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_quality_in_queue_is_better()
        {
            _volume.QualityProfile.Value.Cutoff = Quality.FLAC.Id;

            var remoteIssue = Builder<RemoteIssue>.CreateNew()
                                                      .With(r => r.Volume = _volume)
                                                      .With(r => r.Issues = new List<Issue> { _issue })
                                                      .With(r => r.ParsedIssueInfo = new ParsedIssueInfo
                                                      {
                                                          Quality = new QualityModel(Quality.MP3)
                                                      })
                                                      .With(r => r.Release = _releaseInfo)
                                                      .With(r => r.CustomFormats = new List<CustomFormat>())
                                                      .Build();

            GivenQueue(new List<RemoteIssue> { remoteIssue });
            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_matching_multi_issue_is_in_queue()
        {
            var remoteIssue = Builder<RemoteIssue>.CreateNew()
                                                      .With(r => r.Volume = _volume)
                                                      .With(r => r.Issues = new List<Issue> { _issue, _otherIssue })
                                                      .With(r => r.ParsedIssueInfo = new ParsedIssueInfo
                                                      {
                                                          Quality = new QualityModel(Quality.MP3)
                                                      })
                                                      .With(r => r.Release = _releaseInfo)
                                                      .With(r => r.CustomFormats = new List<CustomFormat>())
                                                      .Build();

            GivenQueue(new List<RemoteIssue> { remoteIssue });
            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_multi_issue_has_one_issue_in_queue()
        {
            var remoteIssue = Builder<RemoteIssue>.CreateNew()
                                                      .With(r => r.Volume = _volume)
                                                      .With(r => r.Issues = new List<Issue> { _issue })
                                                      .With(r => r.ParsedIssueInfo = new ParsedIssueInfo
                                                      {
                                                          Quality = new QualityModel(Quality.MP3)
                                                      })
                                                      .With(r => r.Release = _releaseInfo)
                                                      .With(r => r.CustomFormats = new List<CustomFormat>())
                                                      .Build();

            _remoteIssue.Issues.Add(_otherIssue);

            GivenQueue(new List<RemoteIssue> { remoteIssue });
            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_multi_part_issue_is_already_in_queue()
        {
            var remoteIssue = Builder<RemoteIssue>.CreateNew()
                                                      .With(r => r.Volume = _volume)
                                                      .With(r => r.Issues = new List<Issue> { _issue, _otherIssue })
                                                      .With(r => r.ParsedIssueInfo = new ParsedIssueInfo
                                                      {
                                                          Quality = new QualityModel(Quality.MP3)
                                                      })
                                                      .With(r => r.Release = _releaseInfo)
                                                      .With(r => r.CustomFormats = new List<CustomFormat>())
                                                      .Build();

            _remoteIssue.Issues.Add(_otherIssue);

            GivenQueue(new List<RemoteIssue> { remoteIssue });
            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_multi_part_issue_has_two_issues_in_queue()
        {
            var remoteIssues = Builder<RemoteIssue>.CreateListOfSize(2)
                                                       .All()
                                                       .With(r => r.Volume = _volume)
                                                       .With(r => r.CustomFormats = new List<CustomFormat>())
                                                       .With(r => r.ParsedIssueInfo = new ParsedIssueInfo
                                                       {
                                                           Quality = new QualityModel(Quality.MP3)
                                                       })
                                                       .With(r => r.Release = _releaseInfo)
                                                       .TheFirst(1)
                                                       .With(r => r.Issues = new List<Issue> { _issue })
                                                       .TheNext(1)
                                                       .With(r => r.Issues = new List<Issue> { _otherIssue })
                                                       .Build();

            _remoteIssue.Issues.Add(_otherIssue);
            GivenQueue(remoteIssues);
            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_quality_is_better_and_upgrade_allowed_is_false_for_quality_profile()
        {
            _volume.QualityProfile.Value.Cutoff = Quality.FLAC.Id;
            _volume.QualityProfile.Value.UpgradeAllowed = false;

            var remoteIssue = Builder<RemoteIssue>.CreateNew()
                .With(r => r.Volume = _volume)
                .With(r => r.Issues = new List<Issue> { _issue })
                .With(r => r.ParsedIssueInfo = new ParsedIssueInfo
                {
                    Quality = new QualityModel(Quality.FLAC)
                })
                .With(r => r.Release = _releaseInfo)
                .With(r => r.CustomFormats = new List<CustomFormat>())
                .Build();

            GivenQueue(new List<RemoteIssue> { remoteIssue });
            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_if_everything_is_the_same_for_failed_pending()
        {
            _volume.QualityProfile.Value.Cutoff = Quality.FLAC.Id;

            var remoteIssue = Builder<RemoteIssue>.CreateNew()
                .With(r => r.Volume = _volume)
                .With(r => r.Issues = new List<Issue> { _issue })
                .With(r => r.ParsedIssueInfo = new ParsedIssueInfo
                {
                    Quality = new QualityModel(Quality.MP3)
                })
                .With(r => r.Release = _releaseInfo)
                .With(r => r.CustomFormats = new List<CustomFormat>())
                .Build();

            GivenQueue(new List<RemoteIssue> { remoteIssue }, TrackedDownloadState.DownloadFailedPending);

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_same_quality_non_proper_in_queue_and_download_propers_is_do_not_upgrade()
        {
            _remoteIssue.ParsedIssueInfo.Quality = new QualityModel(Quality.FLAC, new Revision(2));
            _volume.QualityProfile.Value.Cutoff = _remoteIssue.ParsedIssueInfo.Quality.Quality.Id;

            Mocker.GetMock<IConfigService>()
                .Setup(s => s.DownloadPropersAndRepacks)
                .Returns(ProperDownloadTypes.DoNotUpgrade);

            var remoteIssue = Builder<RemoteIssue>.CreateNew()
                .With(r => r.Volume = _volume)
                .With(r => r.Issues = new List<Issue> { _issue })
                .With(r => r.ParsedIssueInfo = new ParsedIssueInfo
                {
                    Quality = new QualityModel(Quality.FLAC)
                })
                .With(r => r.Release = _releaseInfo)
                .With(r => r.CustomFormats = new List<CustomFormat>())
                .Build();

            GivenQueue(new List<RemoteIssue> { remoteIssue });

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeFalse();
        }
    }
}
