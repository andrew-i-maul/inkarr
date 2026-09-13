using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class CleanupOrphanedIssuesFixture : DbTest<CleanupOrphanedIssues, Issue>
    {
        [Test]
        public void should_delete_orphaned_issues()
        {
            var issue = Builder<Issue>.CreateNew()
                .BuildNew();

            Db.Insert(issue);
            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_not_delete_unorphaned_issues()
        {
            var volume = Builder<Volume>.CreateNew()
                .With(e => e.Metadata = new VolumeMetadata { Id = 1 })
                .BuildNew();

            Db.Insert(volume);

            var issues = Builder<Issue>.CreateListOfSize(2)
                .TheFirst(1)
                .With(e => e.VolumeMetadataId = volume.Metadata.Value.Id)
                .BuildListOfNew();

            Db.InsertMany(issues);
            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
            AllStoredModels.Should().Contain(e => e.VolumeMetadataId == volume.Metadata.Value.Id);
        }
    }
}
