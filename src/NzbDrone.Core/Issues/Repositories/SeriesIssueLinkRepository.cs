using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Issues
{
    public interface ISeriesIssueLinkRepository : IBasicRepository<SeriesIssueLink>
    {
        List<SeriesIssueLink> GetLinksBySeries(int seriesId);
        List<SeriesIssueLink> GetLinksBySeriesAndVolume(int seriesId, string foreignVolumeId);
        List<SeriesIssueLink> GetLinksByIssue(List<int> issueIds);
    }

    public class SeriesIssueLinkRepository : BasicRepository<SeriesIssueLink>, ISeriesIssueLinkRepository
    {
        public SeriesIssueLinkRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<SeriesIssueLink> GetLinksBySeries(int seriesId)
        {
            return Query(x => x.SeriesId == seriesId);
        }

        public List<SeriesIssueLink> GetLinksBySeriesAndVolume(int seriesId, string foreignVolumeId)
        {
            return _database.Query<SeriesIssueLink>(
                Builder()
                    .Join<SeriesIssueLink, Issue>((l, b) => l.IssueId == b.Id)
                    .Join<Issue, VolumeMetadata>((b, a) => b.VolumeMetadataId == a.Id)
                    .Where<SeriesIssueLink>(x => x.SeriesId == seriesId)
                    .Where<VolumeMetadata>(a => a.ForeignVolumeId == foreignVolumeId))
                .ToList();
        }

        public List<SeriesIssueLink> GetLinksByIssue(List<int> issueIds)
        {
            return _database.QueryJoined<SeriesIssueLink, Series>(
                Builder()
                .Join<SeriesIssueLink, Series>((l, s) => l.SeriesId == s.Id)
                .Where<SeriesIssueLink>(x => issueIds.Contains(x.IssueId)),
                (link, series) =>
                {
                    link.Series = series;
                    return link;
                })
                .ToList();
        }
    }
}
