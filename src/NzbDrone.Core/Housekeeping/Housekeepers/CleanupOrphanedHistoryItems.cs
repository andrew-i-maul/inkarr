using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedHistoryItems : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedHistoryItems(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            CleanupOrphanedByVolume();
            CleanupOrphanedByIssue();
        }

        private void CleanupOrphanedByVolume()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""History""
                             WHERE ""Id"" IN (
                             SELECT ""History"".""Id"" FROM ""History""
                             LEFT OUTER JOIN ""Volumes""
                             ON ""History"".""VolumeId"" = ""Volumes"".""Id""
                             WHERE ""Volumes"".""Id"" IS NULL)");
        }

        private void CleanupOrphanedByIssue()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""History""
                             WHERE ""Id"" IN (
                             SELECT ""History"".""Id"" FROM ""History""
                             LEFT OUTER JOIN ""Issues""
                             ON ""History"".""IssueId"" = ""Issues"".""Id""
                             WHERE ""Issues"".""Id"" IS NULL)");
        }
    }
}
