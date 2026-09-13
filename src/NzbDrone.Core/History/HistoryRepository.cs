using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.History
{
    public interface IHistoryRepository : IBasicRepository<EntityHistory>
    {
        EntityHistory MostRecentForIssue(int issueId);
        EntityHistory MostRecentForDownloadId(string downloadId);
        List<EntityHistory> FindByDownloadId(string downloadId);
        List<EntityHistory> GetByVolume(int volumeId, EntityHistoryEventType? eventType);
        List<EntityHistory> GetByIssue(int issueId, EntityHistoryEventType? eventType);
        List<EntityHistory> FindDownloadHistory(int idVolumeId, QualityModel quality);
        void DeleteForVolume(int volumeId);
        List<EntityHistory> Since(DateTime date, EntityHistoryEventType? eventType);
    }

    public class HistoryRepository : BasicRepository<EntityHistory>, IHistoryRepository
    {
        public HistoryRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public EntityHistory MostRecentForIssue(int issueId)
        {
            return Query(h => h.IssueId == issueId).MaxBy(h => h.Date);
        }

        public EntityHistory MostRecentForDownloadId(string downloadId)
        {
            return Query(h => h.DownloadId == downloadId).MaxBy(h => h.Date);
        }

        public List<EntityHistory> FindByDownloadId(string downloadId)
        {
            return _database.QueryJoined<EntityHistory, Volume, Issue>(
                Builder()
                .Join<EntityHistory, Volume>((h, a) => h.VolumeId == a.Id)
                .Join<EntityHistory, Issue>((h, a) => h.IssueId == a.Id)
                .Where<EntityHistory>(h => h.DownloadId == downloadId),
                (history, volume, issue) =>
                {
                    history.Volume = volume;
                    history.Issue = issue;
                    return history;
                }).ToList();
        }

        public List<EntityHistory> GetByVolume(int volumeId, EntityHistoryEventType? eventType)
        {
            var builder = Builder().Where<EntityHistory>(h => h.VolumeId == volumeId);

            if (eventType.HasValue)
            {
                builder.Where<EntityHistory>(h => h.EventType == eventType);
            }

            return Query(builder).OrderByDescending(h => h.Date).ToList();
        }

        public List<EntityHistory> GetByIssue(int issueId, EntityHistoryEventType? eventType)
        {
            var builder = Builder()
                .Join<EntityHistory, Issue>((h, a) => h.IssueId == a.Id)
                .Where<EntityHistory>(h => h.IssueId == issueId);

            if (eventType.HasValue)
            {
                builder.Where<EntityHistory>(h => h.EventType == eventType);
            }

            return _database.QueryJoined<EntityHistory, Issue>(
                builder,
                (history, issue) =>
                {
                    history.Issue = issue;
                    return history;
                }).OrderByDescending(h => h.Date).ToList();
        }

        public List<EntityHistory> FindDownloadHistory(int idVolumeId, QualityModel quality)
        {
            var allowed = new[] { (int)EntityHistoryEventType.Grabbed, (int)EntityHistoryEventType.DownloadFailed, (int)EntityHistoryEventType.IssueFileImported };

            return Query(h => h.VolumeId == idVolumeId &&
                         h.Quality == quality &&
                         allowed.Contains((int)h.EventType));
        }

        public void DeleteForVolume(int volumeId)
        {
            Delete(c => c.VolumeId == volumeId);
        }

        protected override SqlBuilder PagedBuilder() => new SqlBuilder(_database.DatabaseType)
            .Join<EntityHistory, Volume>((h, a) => h.VolumeId == a.Id)
            .Join<Volume, VolumeMetadata>((l, r) => l.VolumeMetadataId == r.Id)
            .Join<EntityHistory, Issue>((h, a) => h.IssueId == a.Id);

        protected override IEnumerable<EntityHistory> PagedQuery(SqlBuilder builder) =>
            _database.QueryJoined<EntityHistory, Volume, VolumeMetadata, Issue>(builder, (history, volume, metadata, issue) =>
                    {
                        volume.Metadata = metadata;
                        history.Volume = volume;
                        history.Issue = issue;
                        return history;
                    });

        public List<EntityHistory> Since(DateTime date, EntityHistoryEventType? eventType)
        {
            var builder = Builder()
                .Join<EntityHistory, Volume>((h, a) => h.VolumeId == a.Id)
                .LeftJoin<EntityHistory, Issue>((h, b) => h.IssueId == b.Id)
                .Where<EntityHistory>(x => x.Date >= date);

            if (eventType.HasValue)
            {
                builder.Where<EntityHistory>(h => h.EventType == eventType);
            }

            return _database.QueryJoined<EntityHistory, Volume, Issue>(builder, (history, volume, issue) =>
            {
                history.Volume = volume;
                history.Issue = issue;
                return history;
            }).OrderBy(h => h.Date).ToList();
        }
    }
}
