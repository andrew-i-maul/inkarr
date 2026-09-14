using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(042)]
    public class fix_volume_issue_rename_gaps : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Migration 041 renamed the core Author/Book/Edition/Series tables and their
            // direct columns, but missed columns on tables outside that core set whose C#
            // properties were also renamed as part of the same Volume/Issue domain rename.
            Rename.Column("RenameBooks").OnTable("NamingConfig").To("RenameIssues");
            Rename.Column("StandardBookFormat").OnTable("NamingConfig").To("StandardIssueFormat");
            Rename.Column("AuthorFolderFormat").OnTable("NamingConfig").To("VolumeFolderFormat");

            Rename.Column("AuthorId").OnTable("ExtraFiles").To("VolumeId");
            Rename.Column("BookId").OnTable("ExtraFiles").To("IssueId");
            Rename.Column("BookFileId").OnTable("ExtraFiles").To("IssueFileId");

            Rename.Column("AuthorId").OnTable("MetadataFiles").To("VolumeId");
            Rename.Column("BookId").OnTable("MetadataFiles").To("IssueId");
            Rename.Column("BookFileId").OnTable("MetadataFiles").To("IssueFileId");

            Rename.Column("BookId").OnTable("History").To("IssueId");

            Rename.Column("ParsedBookInfo").OnTable("PendingReleases").To("ParsedIssueInfo");

            Rename.Column("BookIds").OnTable("Blocklist").To("IssueIds");

            Rename.Column("OnAuthorAdded").OnTable("Notifications").To("OnVolumeAdded");
            Rename.Column("OnAuthorDelete").OnTable("Notifications").To("OnVolumeDelete");
            Rename.Column("OnBookDelete").OnTable("Notifications").To("OnIssueDelete");
            Rename.Column("OnBookFileDelete").OnTable("Notifications").To("OnIssueFileDelete");
            Rename.Column("OnBookFileDeleteForUpgrade").OnTable("Notifications").To("OnIssueFileDeleteForUpgrade");
            Rename.Column("OnBookRetag").OnTable("Notifications").To("OnIssueRetag");

            Rename.Column("IsEbook").OnTable("Editions").To("IsEissue");
        }
    }
}
