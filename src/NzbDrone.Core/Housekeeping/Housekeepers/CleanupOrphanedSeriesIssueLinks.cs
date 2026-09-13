using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedSeriesIssueLinks : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedSeriesIssueLinks(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""SeriesIssueLink""
                            WHERE ""Id"" IN (
                            SELECT ""SeriesIssueLink"".""Id"" FROM ""SeriesIssueLink""
                            LEFT OUTER JOIN ""Issues""
                            ON ""SeriesIssueLink"".""IssueId"" = ""Issues"".""Id""
                            WHERE ""Issues"".""Id"" IS NULL)");

            mapper.Execute(@"DELETE FROM ""SeriesIssueLink""
                             WHERE ""Id"" IN (
                             SELECT ""SeriesIssueLink"".""Id"" FROM ""SeriesIssueLink""
                             LEFT OUTER JOIN ""Series""
                             ON ""SeriesIssueLink"".""SeriesId"" = ""Series"".""Id""
                             WHERE ""Series"".""Id"" IS NULL)");
        }
    }
}
