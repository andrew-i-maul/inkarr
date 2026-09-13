using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedIssueFiles : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedIssueFiles(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            using var mapper = _database.OpenConnection();

            // Unlink where issues no longer exists
            mapper.Execute(@"UPDATE ""IssueFiles""
                             SET ""EditionId"" = 0
                             WHERE ""Id"" IN (
                             SELECT ""IssueFiles"".""Id"" FROM ""IssueFiles""
                             LEFT OUTER JOIN ""Editions""
                             ON ""IssueFiles"".""EditionId"" = ""Editions"".""Id""
                             WHERE ""Editions"".""Id"" IS NULL)");
        }
    }
}
