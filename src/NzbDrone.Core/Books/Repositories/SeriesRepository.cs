using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Issues
{
    public interface ISeriesRepository : IBasicRepository<Series>
    {
        Series FindById(string foreignSeriesId);
        List<Series> FindById(List<string> foreignSeriesId);
        List<Series> GetByVolumeMetadataId(int volumeMetadataId);
        List<Series> GetByVolumeId(int volumeId);
    }

    public class SeriesRepository : BasicRepository<Series>, ISeriesRepository
    {
        public SeriesRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public Series FindById(string foreignSeriesId)
        {
            return Query(x => x.ForeignSeriesId == foreignSeriesId).SingleOrDefault();
        }

        public List<Series> FindById(List<string> foreignSeriesId)
        {
            return Query(x => foreignSeriesId.Contains(x.ForeignSeriesId));
        }

        public List<Series> GetByVolumeMetadataId(int volumeMetadataId)
        {
            return QueryDistinct(Builder().Join<Series, SeriesIssueLink>((l, r) => l.Id == r.SeriesId)
                                 .Join<SeriesIssueLink, Issue>((l, r) => l.IssueId == r.Id)
                                 .Where<Issue>(x => x.VolumeMetadataId == volumeMetadataId));
        }

        public List<Series> GetByVolumeId(int volumeId)
        {
            return QueryDistinct(Builder().Join<Series, SeriesIssueLink>((l, r) => l.Id == r.SeriesId)
                                 .Join<SeriesIssueLink, Issue>((l, r) => l.IssueId == r.Id)
                                 .Join<Issue, Volume>((l, r) => l.VolumeMetadataId == r.VolumeMetadataId)
                                 .Where<Volume>(x => x.Id == volumeId));
        }
    }
}
