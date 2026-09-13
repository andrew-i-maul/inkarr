using System.Collections.Generic;

namespace NzbDrone.Core.Issues
{
    public interface IVolumeMetadataService
    {
        bool Upsert(VolumeMetadata volume);
        bool UpsertMany(List<VolumeMetadata> volumes);
    }

    public class VolumeMetadataService : IVolumeMetadataService
    {
        private readonly IVolumeMetadataRepository _volumeMetadataRepository;

        public VolumeMetadataService(IVolumeMetadataRepository volumeMetadataRepository)
        {
            _volumeMetadataRepository = volumeMetadataRepository;
        }

        public bool Upsert(VolumeMetadata volume)
        {
            return _volumeMetadataRepository.UpsertMany(new List<VolumeMetadata> { volume });
        }

        public bool UpsertMany(List<VolumeMetadata> volumes)
        {
            return _volumeMetadataRepository.UpsertMany(volumes);
        }
    }
}
