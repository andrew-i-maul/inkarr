using System.Collections.Generic;
using System.Linq;
using Dapper;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Issues
{
    public interface IVolumeRepository : IBasicRepository<Volume>
    {
        bool VolumePathExists(string path);
        Volume FindByName(string cleanName);
        Volume FindById(string foreignVolumeId);
        Dictionary<int, string> AllVolumePaths();
        Dictionary<int, List<int>> AllVolumeTags();
        Volume GetVolumeByMetadataId(int volumeMetadataId);
        List<Volume> GetVolumesByMetadataId(IEnumerable<int> volumeMetadataId);
    }

    public class VolumeRepository : BasicRepository<Volume>, IVolumeRepository
    {
        public VolumeRepository(IMainDatabase database,
                                IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        protected override SqlBuilder Builder() => new SqlBuilder(_database.DatabaseType)
            .Join<Volume, VolumeMetadata>((a, m) => a.VolumeMetadataId == m.Id);

        protected override List<Volume> Query(SqlBuilder builder) => Query(_database, builder).ToList();

        public static IEnumerable<Volume> Query(IDatabase database, SqlBuilder builder)
        {
            return database.QueryJoined<Volume, VolumeMetadata>(builder, (volume, metadata) =>
                    {
                        volume.Metadata = metadata;
                        return volume;
                    });
        }

        public bool VolumePathExists(string path)
        {
            return Query(c => c.Path == path).Any();
        }

        public Volume FindById(string foreignVolumeId)
        {
            return Query(Builder().Where<VolumeMetadata>(m => m.ForeignVolumeId == foreignVolumeId)).SingleOrDefault();
        }

        public Volume FindByName(string cleanName)
        {
            cleanName = cleanName.ToLowerInvariant();

            return Query(s => s.CleanName == cleanName).ExclusiveOrDefault();
        }

        public Dictionary<int, string> AllVolumePaths()
        {
            using (var conn = _database.OpenConnection())
            {
                var strSql = "SELECT \"Id\" AS \"Key\", \"Path\" AS \"Value\" FROM \"Volumes\"";
                return conn.Query<KeyValuePair<int, string>>(strSql).ToDictionary(x => x.Key, x => x.Value);
            }
        }

        public Dictionary<int, List<int>> AllVolumeTags()
        {
            using (var conn = _database.OpenConnection())
            {
                var strSql = "SELECT \"Id\" AS \"Key\", \"Tags\" AS \"Value\" FROM \"Volumes\" WHERE \"Tags\" IS NOT NULL";
                return conn.Query<KeyValuePair<int, List<int>>>(strSql).ToDictionary(x => x.Key, x => x.Value);
            }
        }

        public Volume GetVolumeByMetadataId(int volumeMetadataId)
        {
            return Query(s => s.VolumeMetadataId == volumeMetadataId).SingleOrDefault();
        }

        public List<Volume> GetVolumesByMetadataId(IEnumerable<int> volumeMetadataIds)
        {
            return Query(s => volumeMetadataIds.Contains(s.VolumeMetadataId));
        }
    }
}
