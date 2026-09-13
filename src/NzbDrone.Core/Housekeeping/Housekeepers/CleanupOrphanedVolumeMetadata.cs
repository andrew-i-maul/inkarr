using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedVolumeMetadata : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedVolumeMetadata(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""VolumeMetadata""
                             WHERE ""Id"" IN (
                             SELECT ""VolumeMetadata"".""Id"" FROM ""VolumeMetadata""
                             LEFT OUTER JOIN ""Issues"" ON ""Issues"".""VolumeMetadataId"" = ""VolumeMetadata"".""Id""
                             LEFT OUTER JOIN ""Volumes"" ON ""Volumes"".""VolumeMetadataId"" = ""VolumeMetadata"".""Id""
                             WHERE ""Issues"".""Id"" IS NULL AND ""Volumes"".""Id"" IS NULL)");
        }
    }
}
