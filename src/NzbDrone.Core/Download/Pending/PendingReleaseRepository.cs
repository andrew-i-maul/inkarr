using System.Collections.Generic;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Download.Pending
{
    public interface IPendingReleaseRepository : IBasicRepository<PendingRelease>
    {
        void DeleteByVolumeId(int volumeId);
        List<PendingRelease> AllByVolumeId(int volumeId);
        List<PendingRelease> WithoutFallback();
    }

    public class PendingReleaseRepository : BasicRepository<PendingRelease>, IPendingReleaseRepository
    {
        public PendingReleaseRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public void DeleteByVolumeId(int volumeId)
        {
            Delete(x => x.VolumeId == volumeId);
        }

        public List<PendingRelease> AllByVolumeId(int volumeId)
        {
            return Query(p => p.VolumeId == volumeId);
        }

        public List<PendingRelease> WithoutFallback()
        {
            return Query(p => p.Reason != PendingReleaseReason.Fallback);
        }
    }
}
