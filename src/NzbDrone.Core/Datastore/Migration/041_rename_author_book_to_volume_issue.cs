using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(041)]
    public class rename_author_book_to_volume_issue : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Comic domain-model rename (Phase 4): Author -> Volume, Book -> Issue.
            // Edition and Series keep their names -- both already read fine for comics.
            Rename.Table("Authors").To("Volumes");
            Rename.Table("Books").To("Issues");
            Rename.Table("AuthorMetadata").To("VolumeMetadata");
            Rename.Table("BookFiles").To("IssueFiles");
            Rename.Table("SeriesBookLink").To("SeriesIssueLink");

            Rename.Column("AuthorMetadataId").OnTable("Volumes").To("VolumeMetadataId");
            Rename.Column("AuthorMetadataId").OnTable("Issues").To("VolumeMetadataId");
            Rename.Column("ForeignBookId").OnTable("Issues").To("ForeignIssueId");
            Rename.Column("RelatedBooks").OnTable("Issues").To("RelatedIssues");

            Rename.Column("ForeignAuthorId").OnTable("VolumeMetadata").To("ForeignVolumeId");

            Rename.Column("BookId").OnTable("Editions").To("IssueId");

            Rename.Column("BookId").OnTable("SeriesIssueLink").To("IssueId");
        }
    }
}
