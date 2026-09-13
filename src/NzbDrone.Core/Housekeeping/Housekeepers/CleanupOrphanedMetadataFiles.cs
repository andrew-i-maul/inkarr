using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedMetadataFiles : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedMetadataFiles(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            DeleteOrphanedByVolume();
            DeleteOrphanedByIssue();
            DeleteOrphanedByTrackFile();
            DeleteWhereIssueIdIsZero();
            DeleteWhereTrackFileIsZero();
        }

        private void DeleteOrphanedByVolume()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                             WHERE ""Id"" IN (
                             SELECT ""MetadataFiles"".""Id"" FROM ""MetadataFiles""
                             LEFT OUTER JOIN ""Volumes""
                             ON ""MetadataFiles"".""VolumeId"" = ""Volumes"".""Id""
                             WHERE ""Volumes"".""Id"" IS NULL)");
        }

        private void DeleteOrphanedByIssue()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                             WHERE ""Id"" IN (
                             SELECT ""MetadataFiles"".""Id"" FROM ""MetadataFiles""
                             LEFT OUTER JOIN ""Issues""
                             ON ""MetadataFiles"".""IssueId"" = ""Issues"".""Id""
                             WHERE ""MetadataFiles"".""IssueId"" > 0
                             AND ""Issues"".""Id"" IS NULL)");
        }

        private void DeleteOrphanedByTrackFile()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                             WHERE ""Id"" IN (
                             SELECT ""MetadataFiles"".""Id"" FROM ""MetadataFiles""
                             LEFT OUTER JOIN ""IssueFiles""
                             ON ""MetadataFiles"".""IssueFileId"" = ""IssueFiles"".""Id""
                             WHERE ""MetadataFiles"".""IssueFileId"" > 0
                             AND ""IssueFiles"".""Id"" IS NULL)");
        }

        private void DeleteWhereIssueIdIsZero()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                             WHERE ""Id"" IN (
                             SELECT ""Id"" FROM ""MetadataFiles""
                             WHERE ""Type"" IN (2, 4)
                             AND ""IssueId"" = 0)");
        }

        private void DeleteWhereTrackFileIsZero()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                             WHERE ""Id"" IN (
                             SELECT ""Id"" FROM ""MetadataFiles""
                             WHERE ""Type"" IN (2, 4)
                             AND ""IssueFileId"" = 0)");
        }
    }
}
