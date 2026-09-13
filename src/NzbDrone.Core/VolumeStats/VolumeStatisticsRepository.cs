using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.VolumeStats
{
    public interface IVolumeStatisticsRepository
    {
        List<IssueStatistics> VolumeStatistics();
        List<IssueStatistics> VolumeStatistics(int volumeId);
    }

    public class VolumeStatisticsRepository : IVolumeStatisticsRepository
    {
        private const string _selectTemplate = "SELECT /**select**/ FROM \"Editions\" /**join**/ /**innerjoin**/ /**leftjoin**/ /**where**/ /**groupby**/ /**having**/ /**orderby**/";

        private readonly IMainDatabase _database;

        public VolumeStatisticsRepository(IMainDatabase database)
        {
            _database = database;
        }

        public List<IssueStatistics> VolumeStatistics()
        {
            return Query(Builder());
        }

        public List<IssueStatistics> VolumeStatistics(int volumeId)
        {
            return Query(Builder().Where<Volume>(x => x.Id == volumeId));
        }

        private List<IssueStatistics> Query(SqlBuilder builder)
        {
            var sql = builder.AddTemplate(_selectTemplate).LogQuery();

            using (var conn = _database.OpenConnection())
            {
                return conn.Query<IssueStatistics>(sql.RawSql, sql.Parameters).ToList();
            }
        }

        private SqlBuilder Builder()
        {
            var trueIndicator = _database.DatabaseType == DatabaseType.PostgreSQL ? "true" : "1";

            return new SqlBuilder(_database.DatabaseType)
            .Select($@"""Volumes"".""Id"" AS ""VolumeId"",
                     ""Issues"".""Id"" AS ""IssueId"",
                     SUM(COALESCE(""IssueFiles"".""Size"", 0)) AS ""SizeOnDisk"",
                     1 AS ""TotalIssueCount"",
                     CASE WHEN MIN(""IssueFiles"".""Id"") IS NULL THEN 0 ELSE 1 END AS ""AvailableIssueCount"",
                     CASE WHEN (""Issues"".""Monitored"" = {trueIndicator} AND (""Issues"".""ReleaseDate"" < @currentDate) OR ""Issues"".""ReleaseDate"" IS NULL) OR MIN(""IssueFiles"".""Id"") IS NOT NULL THEN 1 ELSE 0 END AS ""IssueCount"",
                     CASE WHEN MIN(""IssueFiles"".""Id"") IS NULL THEN 0 ELSE COUNT(""IssueFiles"".""Id"") END AS ""IssueFileCount""")
            .Join<Edition, Issue>((e, b) => e.IssueId == b.Id)
            .Join<Issue, Volume>((issue, volume) => issue.VolumeMetadataId == volume.VolumeMetadataId)
            .LeftJoin<Edition, IssueFile>((t, f) => t.Id == f.EditionId)
            .Where<Edition>(x => x.Monitored == true)
            .GroupBy<Volume>(x => x.Id)
            .GroupBy<Issue>(x => x.Id)
            .AddParameters(new Dictionary<string, object> { { "currentDate", DateTime.UtcNow } });
        }
    }
}
