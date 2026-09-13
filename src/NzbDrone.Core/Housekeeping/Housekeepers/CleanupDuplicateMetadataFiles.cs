using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupDuplicateMetadataFiles : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupDuplicateMetadataFiles(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            DeleteDuplicateVolumeMetadata();
            DeleteDuplicateIssueMetadata();
            DeleteDuplicateIssueFileMetadata();
        }

        private void DeleteDuplicateVolumeMetadata()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                             WHERE ""Id"" IN (
                                 SELECT MIN(""Id"") FROM ""MetadataFiles""
                                 WHERE ""Type"" = 1
                                 GROUP BY ""VolumeId"", ""Consumer""
                                 HAVING COUNT(""VolumeId"") > 1
                             )");
        }

        private void DeleteDuplicateIssueMetadata()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                             WHERE ""Id"" IN (
                                 SELECT MIN(""Id"") FROM ""MetadataFiles""
                                 WHERE ""Type"" IN (2, 4)
                                 GROUP BY ""IssueId"", ""Consumer""
                                 HAVING COUNT(""IssueId"") > 1
                             )");
        }

        private void DeleteDuplicateIssueFileMetadata()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                             WHERE ""Id"" IN (
                                 SELECT MIN(""Id"") FROM ""MetadataFiles""
                                 WHERE ""Type"" IN (2, 4)
                                 GROUP BY ""IssueFileId"", ""Consumer""
                                 HAVING COUNT(""IssueFileId"") > 1
                             )");
        }
    }
}
