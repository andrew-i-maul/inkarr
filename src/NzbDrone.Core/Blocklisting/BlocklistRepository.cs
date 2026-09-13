using System.Collections.Generic;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Blocklisting
{
    public interface IBlocklistRepository : IBasicRepository<Blocklist>
    {
        List<Blocklist> BlocklistedByTitle(int volumeId, string sourceTitle);
        List<Blocklist> BlocklistedByTorrentInfoHash(int volumeId, string torrentInfoHash);
        List<Blocklist> BlocklistedByVolume(int volumeId);
    }

    public class BlocklistRepository : BasicRepository<Blocklist>, IBlocklistRepository
    {
        public BlocklistRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<Blocklist> BlocklistedByTitle(int volumeId, string sourceTitle)
        {
            return Query(e => e.VolumeId == volumeId && e.SourceTitle.Contains(sourceTitle));
        }

        public List<Blocklist> BlocklistedByTorrentInfoHash(int volumeId, string torrentInfoHash)
        {
            return Query(e => e.VolumeId == volumeId && e.TorrentInfoHash.Contains(torrentInfoHash));
        }

        public List<Blocklist> BlocklistedByVolume(int volumeId)
        {
            return Query(b => b.VolumeId == volumeId);
        }

        protected override SqlBuilder PagedBuilder() => new SqlBuilder(_database.DatabaseType)
            .Join<Blocklist, Volume>((b, m) => b.VolumeId == m.Id)
            .Join<Volume, VolumeMetadata>((l, r) => l.VolumeMetadataId == r.Id);
        protected override IEnumerable<Blocklist> PagedQuery(SqlBuilder builder) => _database.QueryJoined<Blocklist, Volume, VolumeMetadata>(builder,
            (bl, volume, metadata) =>
                    {
                        volume.Metadata = metadata;
                        bl.Volume = volume;
                        return bl;
                    });
    }
}
