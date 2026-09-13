using System.Linq;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class UpdateCleanTitleForVolume : IHousekeepingTask
    {
        private readonly IVolumeRepository _volumeRepository;

        public UpdateCleanTitleForVolume(IVolumeRepository volumeRepository)
        {
            _volumeRepository = volumeRepository;
        }

        public void Clean()
        {
            var volumes = _volumeRepository.All().ToList();

            volumes.ForEach(s =>
            {
                var cleanName = s.Name.CleanVolumeName();
                if (s.CleanName != cleanName)
                {
                    s.CleanName = cleanName;
                    _volumeRepository.Update(s);
                }
            });
        }
    }
}
