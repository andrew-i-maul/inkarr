using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using FluentValidation;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MusicTests
{
    [TestFixture]
    public class AddIssueFixture : CoreTest<AddIssueService>
    {
        private Volume _fakeVolume;
        private Issue _fakeIssue;

        [SetUp]
        public void Setup()
        {
            _fakeVolume = Builder<Volume>
                .CreateNew()
                .With(s => s.Path = null)
                .With(s => s.Metadata = Builder<VolumeMetadata>.CreateNew().Build())
                .Build();
        }

        private void GivenValidIssue(string issueId, string editionId)
        {
            _fakeIssue = Builder<Issue>
                .CreateNew()
                .With(x => x.Editions = Builder<Edition>
                      .CreateListOfSize(1)
                      .TheFirst(1)
                      .With(e => e.ForeignEditionId = editionId)
                      .With(e => e.Monitored = true)
                      .BuildList())
                .Build();

            Mocker.GetMock<IProvideIssueInfo>()
                .Setup(s => s.GetIssueInfo(issueId))
                .Returns(Tuple.Create(_fakeVolume.Metadata.Value.ForeignVolumeId,
                                      _fakeIssue,
                                      new List<VolumeMetadata> { _fakeVolume.Metadata.Value }));

            Mocker.GetMock<IAddVolumeService>()
                .Setup(s => s.AddVolume(It.IsAny<Volume>(), It.IsAny<bool>()))
                .Returns(_fakeVolume);
        }

        private void GivenValidPath()
        {
            Mocker.GetMock<IBuildFileNames>()
                  .Setup(s => s.GetVolumeFolder(It.IsAny<Volume>(), null))
                  .Returns<Volume, NamingConfig>((c, n) => c.Name);
        }

        private Issue IssueToAdd(string editionId, string issueId, string volumeId)
        {
            return new Issue
            {
                ForeignIssueId = issueId,
                Editions = new List<Edition>
                {
                    new Edition
                    {
                        ForeignEditionId = editionId,
                        Monitored = true
                    }
                },
                VolumeMetadata = new VolumeMetadata
                {
                    ForeignVolumeId = volumeId
                }
            };
        }

        [Test]
        public void should_be_able_to_add_a_issue_without_passing_in_name()
        {
            var newIssue = IssueToAdd("edition", "issue", "volume");

            GivenValidIssue("issue", "edition");
            GivenValidPath();

            var issue = Subject.AddIssue(newIssue);

            issue.Title.Should().Be(_fakeIssue.Title);
        }

        [Test]
        public void should_throw_if_issue_cannot_be_found()
        {
            var newIssue = IssueToAdd("edition", "issue", "volume");

            Mocker.GetMock<IProvideIssueInfo>()
                  .Setup(s => s.GetIssueInfo("issue"))
                  .Throws(new IssueNotFoundException("edition"));

            Assert.Throws<ValidationException>(() => Subject.AddIssue(newIssue));

            ExceptionVerification.ExpectedErrors(1);
        }
    }
}
