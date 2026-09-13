using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Issues
{
    public interface IVolumeMetadataRepository : IBasicRepository<VolumeMetadata>
    {
        List<VolumeMetadata> FindById(List<string> foreignIds);
        bool UpsertMany(List<VolumeMetadata> data);
    }

    public class VolumeMetadataRepository : BasicRepository<VolumeMetadata>, IVolumeMetadataRepository
    {
        private readonly Logger _logger;

        public VolumeMetadataRepository(IMainDatabase database, IEventAggregator eventAggregator, Logger logger)
            : base(database, eventAggregator)
        {
            _logger = logger;
        }

        public List<VolumeMetadata> FindById(List<string> foreignIds)
        {
            return Query(x => Enumerable.Contains(foreignIds, x.ForeignVolumeId));
        }

        public bool UpsertMany(List<VolumeMetadata> data)
        {
            var existingMetadata = FindById(data.Select(x => x.ForeignVolumeId).ToList());
            var updateMetadataList = new List<VolumeMetadata>();
            var addMetadataList = new List<VolumeMetadata>();
            var upToDateMetadataCount = 0;

            foreach (var meta in data)
            {
                var existing = existingMetadata.SingleOrDefault(x => x.ForeignVolumeId == meta.ForeignVolumeId);
                if (existing != null)
                {
                    // populate Id in remote data
                    meta.UseDbFieldsFrom(existing);

                    // responses vary, so try adding remote to what we have
                    if (!meta.Equals(existing))
                    {
                        updateMetadataList.Add(meta);
                    }
                    else
                    {
                        upToDateMetadataCount++;
                    }
                }
                else
                {
                    addMetadataList.Add(meta);
                }
            }

            UpdateMany(updateMetadataList);
            InsertMany(addMetadataList);

            _logger.Debug($"{upToDateMetadataCount} volume metadata up to date; Updating {updateMetadataList.Count}, Adding {addMetadataList.Count} volume metadata entries.");

            return updateMetadataList.Count > 0 || addMetadataList.Count > 0;
        }
    }
}
