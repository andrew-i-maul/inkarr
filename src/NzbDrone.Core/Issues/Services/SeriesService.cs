using System.Collections.Generic;

namespace NzbDrone.Core.Issues
{
    public interface ISeriesService
    {
        Series FindById(string foreignSeriesId);
        List<Series> FindById(List<string> foreignSeriesId);
        List<Series> GetByVolumeMetadataId(int volumeMetadataId);
        List<Series> GetByVolumeId(int volumeId);
        void Delete(int seriesId);
        void InsertMany(IList<Series> series);
        void UpdateMany(IList<Series> series);
    }

    public class SeriesService : ISeriesService
    {
        private readonly ISeriesRepository _seriesRepository;

        public SeriesService(ISeriesRepository seriesRepository)
        {
            _seriesRepository = seriesRepository;
        }

        public Series FindById(string foreignSeriesId)
        {
            return _seriesRepository.FindById(foreignSeriesId);
        }

        public List<Series> FindById(List<string> foreignSeriesId)
        {
            return _seriesRepository.FindById(foreignSeriesId);
        }

        public List<Series> GetByVolumeMetadataId(int volumeMetadataId)
        {
            return _seriesRepository.GetByVolumeMetadataId(volumeMetadataId);
        }

        public List<Series> GetByVolumeId(int volumeId)
        {
            return _seriesRepository.GetByVolumeId(volumeId);
        }

        public void Delete(int seriesId)
        {
            _seriesRepository.Delete(seriesId);
        }

        public void InsertMany(IList<Series> series)
        {
            _seriesRepository.InsertMany(series);
        }

        public void UpdateMany(IList<Series> series)
        {
            _seriesRepository.UpdateMany(series);
        }
    }
}
