using System.Collections.Generic;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.History;
using NzbDrone.Core.ImportLists.Exclusions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Commands;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Profiles.Metadata;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MusicTests
{
    [TestFixture]
    public class RefreshVolumeServiceFixture : CoreTest<RefreshVolumeService>
    {
        private Volume _volume;
        private Issue _issue1;
        private Issue _issue2;
        private List<Issue> _issues;
        private List<Issue> _remoteIssues;

        [SetUp]
        public void Setup()
        {
            _issue1 = Builder<Issue>.CreateNew()
                .With(s => s.ForeignIssueId = "1")
                .Build();

            _issue2 = Builder<Issue>.CreateNew()
                .With(s => s.ForeignIssueId = "2")
                .Build();

            _issues = new List<Issue> { _issue1, _issue2 };

            _remoteIssues = _issues.JsonClone();
            _remoteIssues.ForEach(x => x.Id = 0);

            var metadata = Builder<VolumeMetadata>.CreateNew().Build();
            var series = Builder<Series>.CreateListOfSize(1).BuildList();
            var profile = Builder<MetadataProfile>.CreateNew().Build();

            _volume = Builder<Volume>.CreateNew()
                .With(a => a.Metadata = metadata)
                .With(a => a.Series = series)
                .With(a => a.MetadataProfile = profile)
                .Build();

            Mocker.GetMock<IVolumeService>(MockBehavior.Strict)
                  .Setup(s => s.GetVolumes(new List<int> { _volume.Id }))
                  .Returns(new List<Volume> { _volume });

            Mocker.GetMock<IIssueService>(MockBehavior.Strict)
                .Setup(s => s.InsertMany(It.IsAny<List<Issue>>()));

            Mocker.GetMock<IMetadataProfileService>()
                .Setup(s => s.FilterIssues(It.IsAny<Volume>(), It.IsAny<int>()))
                .Returns(_remoteIssues);

            Mocker.GetMock<IProvideVolumeInfo>()
                .Setup(s => s.GetVolumeInfo(It.IsAny<string>(), true))
                .Callback(() => { throw new VolumeNotFoundException(_volume.ForeignVolumeId); });

            Mocker.GetMock<IMediaFileService>()
                .Setup(x => x.GetFilesByVolume(It.IsAny<int>()))
                .Returns(new List<IssueFile>());

            Mocker.GetMock<IHistoryService>()
                .Setup(x => x.GetByVolume(It.IsAny<int>(), It.IsAny<EntityHistoryEventType?>()))
                .Returns(new List<EntityHistory>());

            Mocker.GetMock<IImportListExclusionService>()
                .Setup(x => x.FindByForeignId(It.IsAny<List<string>>()))
                .Returns(new List<ImportListExclusion>());

            Mocker.GetMock<IRootFolderService>()
                .Setup(x => x.All())
                .Returns(new List<RootFolder>());

            Mocker.GetMock<IMonitorNewIssueService>()
                .Setup(x => x.ShouldMonitorNewIssue(It.IsAny<Issue>(), It.IsAny<List<Issue>>(), It.IsAny<NewItemMonitorTypes>()))
                .Returns(true);
        }

        private void GivenNewVolumeInfo(Volume volume)
        {
            Mocker.GetMock<IProvideVolumeInfo>()
                .Setup(s => s.GetVolumeInfo(_volume.ForeignVolumeId, true))
                .Returns(volume);
        }

        private void GivenVolumeFiles()
        {
            Mocker.GetMock<IMediaFileService>()
                  .Setup(x => x.GetFilesByVolume(It.IsAny<int>()))
                  .Returns(Builder<IssueFile>.CreateListOfSize(1).BuildList());
        }

        private void GivenIssuesForRefresh(List<Issue> issues)
        {
            Mocker.GetMock<IIssueService>(MockBehavior.Strict)
                .Setup(s => s.GetIssuesForRefresh(It.IsAny<int>(), It.IsAny<List<string>>()))
                .Returns(issues);
        }

        private void AllowVolumeUpdate()
        {
            Mocker.GetMock<IVolumeService>(MockBehavior.Strict)
                .Setup(x => x.UpdateVolume(It.IsAny<Volume>()))
                .Returns((Volume a) => a);
        }

        [Test]
        public void should_not_publish_volume_updated_event_if_metadata_not_updated()
        {
            var newVolumeInfo = _volume.JsonClone();
            newVolumeInfo.Metadata = _volume.Metadata.Value.JsonClone();
            newVolumeInfo.Issues = _remoteIssues;

            GivenNewVolumeInfo(newVolumeInfo);
            GivenIssuesForRefresh(_issues);
            AllowVolumeUpdate();

            Subject.Execute(new RefreshVolumeCommand(_volume.Id));

            VerifyEventNotPublished<VolumeUpdatedEvent>();
            VerifyEventPublished<VolumeRefreshCompleteEvent>();
        }

        [Test]
        public void should_publish_volume_updated_event_if_metadata_updated()
        {
            var newVolumeInfo = _volume.JsonClone();
            newVolumeInfo.Metadata = _volume.Metadata.Value.JsonClone();
            newVolumeInfo.Metadata.Value.Images = new List<MediaCover.MediaCover>
            {
                new MediaCover.MediaCover(MediaCover.MediaCoverTypes.Logo, "dummy")
            };
            newVolumeInfo.Issues = _remoteIssues;

            GivenNewVolumeInfo(newVolumeInfo);
            GivenIssuesForRefresh(new List<Issue>());
            AllowVolumeUpdate();

            Subject.Execute(new RefreshVolumeCommand(_volume.Id));

            VerifyEventPublished<VolumeUpdatedEvent>();
            VerifyEventPublished<VolumeRefreshCompleteEvent>();
        }

        [Test]
        public void should_call_new_issue_monitor_service_when_adding_issue()
        {
            var newIssue = Builder<Issue>.CreateNew()
                .With(x => x.Id = 0)
                .With(x => x.ForeignIssueId = "3")
                .Build();
            _remoteIssues.Add(newIssue);

            var newVolumeInfo = _volume.JsonClone();
            newVolumeInfo.Metadata = _volume.Metadata.Value.JsonClone();
            newVolumeInfo.Issues = _remoteIssues;

            GivenNewVolumeInfo(newVolumeInfo);
            GivenIssuesForRefresh(_issues);
            AllowVolumeUpdate();

            Subject.Execute(new RefreshVolumeCommand(_volume.Id));

            Mocker.GetMock<IMonitorNewIssueService>()
                .Verify(x => x.ShouldMonitorNewIssue(newIssue, _issues, _volume.MonitorNewItems), Times.Once());
        }

        [Test]
        public void should_log_error_and_delete_if_musicbrainz_id_not_found_and_volume_has_no_files()
        {
            Mocker.GetMock<IVolumeService>()
                .Setup(x => x.DeleteVolume(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()));

            Subject.Execute(new RefreshVolumeCommand(_volume.Id));

            Mocker.GetMock<IVolumeService>()
                .Verify(v => v.UpdateVolume(It.IsAny<Volume>()), Times.Never());

            Mocker.GetMock<IVolumeService>()
                .Verify(v => v.DeleteVolume(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once());

            ExceptionVerification.ExpectedErrors(1);
            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_log_error_but_not_delete_if_musicbrainz_id_not_found_and_volume_has_files()
        {
            GivenVolumeFiles();
            GivenIssuesForRefresh(new List<Issue>());

            Subject.Execute(new RefreshVolumeCommand(_volume.Id));

            Mocker.GetMock<IVolumeService>()
                .Verify(v => v.UpdateVolume(It.IsAny<Volume>()), Times.Never());

            Mocker.GetMock<IVolumeService>()
                .Verify(v => v.DeleteVolume(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never());

            ExceptionVerification.ExpectedErrors(2);
        }

        [Test]
        public void should_update_if_musicbrainz_id_changed_and_no_clash()
        {
            var newVolumeInfo = _volume.JsonClone();
            newVolumeInfo.Metadata = _volume.Metadata.Value.JsonClone();
            newVolumeInfo.Issues = _remoteIssues;
            newVolumeInfo.ForeignVolumeId = _volume.ForeignVolumeId + 1;
            newVolumeInfo.Metadata.Value.Id = 100;

            GivenNewVolumeInfo(newVolumeInfo);

            var seq = new MockSequence();

            Mocker.GetMock<IVolumeService>(MockBehavior.Strict)
                .Setup(x => x.FindById(newVolumeInfo.ForeignVolumeId))
                .Returns(default(Volume));

            // Make sure that the volume is updated before we refresh the issues
            Mocker.GetMock<IVolumeService>(MockBehavior.Strict)
                .InSequence(seq)
                .Setup(x => x.UpdateVolume(It.IsAny<Volume>()))
                .Returns((Volume a) => a);

            Mocker.GetMock<IIssueService>(MockBehavior.Strict)
                .InSequence(seq)
                .Setup(x => x.GetIssuesForRefresh(It.IsAny<int>(), It.IsAny<List<string>>()))
                .Returns(new List<Issue>());

            // Update called twice for a move/merge
            Mocker.GetMock<IVolumeService>(MockBehavior.Strict)
                .InSequence(seq)
                .Setup(x => x.UpdateVolume(It.IsAny<Volume>()))
                .Returns((Volume a) => a);

            Subject.Execute(new RefreshVolumeCommand(_volume.Id));

            Mocker.GetMock<IVolumeService>()
                .Verify(v => v.UpdateVolume(It.Is<Volume>(s => s.VolumeMetadataId == 100 && s.ForeignVolumeId == newVolumeInfo.ForeignVolumeId)),
                        Times.Exactly(2));
        }

        [Test]
        public void should_merge_if_musicbrainz_id_changed_and_new_id_already_exists()
        {
            var existing = _volume;

            var clash = _volume.JsonClone();
            clash.Id = 100;
            clash.Metadata = existing.Metadata.Value.JsonClone();
            clash.Metadata.Value.Id = 101;
            clash.Metadata.Value.ForeignVolumeId = clash.Metadata.Value.ForeignVolumeId + 1;

            Mocker.GetMock<IVolumeService>(MockBehavior.Strict)
                .Setup(x => x.FindById(clash.Metadata.Value.ForeignVolumeId))
                .Returns(clash);

            var newVolumeInfo = clash.JsonClone();
            newVolumeInfo.Metadata = clash.Metadata.Value.JsonClone();
            newVolumeInfo.Issues = _remoteIssues;

            GivenNewVolumeInfo(newVolumeInfo);

            var seq = new MockSequence();

            // Make sure that the volume is updated before we refresh the issues
            Mocker.GetMock<IIssueService>(MockBehavior.Strict)
                .InSequence(seq)
                .Setup(x => x.GetIssuesByVolume(existing.Id))
                .Returns(_issues);

            Mocker.GetMock<IIssueService>(MockBehavior.Strict)
                .InSequence(seq)
                .Setup(x => x.UpdateMany(It.IsAny<List<Issue>>()));

            Mocker.GetMock<IVolumeService>(MockBehavior.Strict)
                .InSequence(seq)
                .Setup(x => x.DeleteVolume(existing.Id, It.IsAny<bool>(), false));

            Mocker.GetMock<IVolumeService>(MockBehavior.Strict)
                .InSequence(seq)
                .Setup(x => x.UpdateVolume(It.Is<Volume>(a => a.Id == clash.Id)))
                .Returns((Volume a) => a);

            Mocker.GetMock<IIssueService>(MockBehavior.Strict)
                .InSequence(seq)
                .Setup(x => x.GetIssuesForRefresh(clash.VolumeMetadataId, It.IsAny<List<string>>()))
                .Returns(_issues);

            // Update called twice for a move/merge
            Mocker.GetMock<IVolumeService>(MockBehavior.Strict)
                .InSequence(seq)
                .Setup(x => x.UpdateVolume(It.IsAny<Volume>()))
                .Returns((Volume a) => a);

            Subject.Execute(new RefreshVolumeCommand(_volume.Id));

            // the retained volume gets updated
            Mocker.GetMock<IVolumeService>()
                .Verify(v => v.UpdateVolume(It.Is<Volume>(s => s.Id == clash.Id)), Times.Exactly(2));

            // the old one gets removed
            Mocker.GetMock<IVolumeService>()
                .Verify(v => v.DeleteVolume(existing.Id, false, false));

            Mocker.GetMock<IIssueService>()
                .Verify(v => v.UpdateMany(It.Is<List<Issue>>(x => x.Count == _issues.Count)));

            ExceptionVerification.ExpectedWarns(1);
        }
    }
}
