using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Issues
{
    public interface IEditionRepository : IBasicRepository<Edition>
    {
        List<Edition> GetAllMonitoredEditions();
        Edition FindByForeignEditionId(string foreignEditionId);
        List<Edition> FindByIssue(IEnumerable<int> ids);
        List<Edition> FindByVolume(int id);
        List<Edition> FindByVolumeMetadataId(int id, bool onlyMonitored);
        Edition FindByTitle(int volumeMetadataId, string title);
        List<Edition> GetEditionsForRefresh(int issueId, List<string> foreignEditionIds);
        List<Edition> SetMonitored(Edition edition);
    }

    public class EditionRepository : BasicRepository<Edition>, IEditionRepository
    {
        public EditionRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<Edition> GetAllMonitoredEditions()
        {
            return Query(x => x.Monitored == true);
        }

        public Edition FindByForeignEditionId(string foreignEditionId)
        {
            var edition = Query(x => x.ForeignEditionId == foreignEditionId).SingleOrDefault();

            return edition;
        }

        public List<Edition> GetEditionsForRefresh(int issueId, List<string> foreignEditionIds)
        {
            return Query(r => r.IssueId == issueId || foreignEditionIds.Contains(r.ForeignEditionId));
        }

        public List<Edition> FindByIssue(IEnumerable<int> ids)
        {
            // populate the issues and volume metadata also
            // this hopefully speeds up the track matching a lot
            var builder = new SqlBuilder(_database.DatabaseType)
                .LeftJoin<Edition, Issue>((e, b) => e.IssueId == b.Id)
                .LeftJoin<Issue, VolumeMetadata>((b, a) => b.VolumeMetadataId == a.Id)
                .Where<Edition>(r => ids.Contains(r.IssueId));

            return _database.QueryJoined<Edition, Issue, VolumeMetadata>(builder, (edition, issue, metadata) =>
                    {
                        if (issue != null)
                        {
                            issue.VolumeMetadata = metadata;
                            edition.Issue = issue;
                        }

                        return edition;
                    }).ToList();
        }

        public List<Edition> FindByVolume(int id)
        {
            return Query(Builder().Join<Edition, Issue>((e, b) => e.IssueId == b.Id)
                         .Join<Issue, Volume>((b, a) => b.VolumeMetadataId == a.VolumeMetadataId)
                         .Where<Volume>(a => a.Id == id));
        }

        public List<Edition> FindByVolumeMetadataId(int volumeMetadataId, bool onlyMonitored)
        {
            var builder = Builder().Join<Edition, Issue>((e, b) => e.IssueId == b.Id)
                .Where<Issue>(b => b.VolumeMetadataId == volumeMetadataId);

            if (onlyMonitored)
            {
                builder = builder.OrWhere<Edition>(e => e.Monitored == true);
                builder = builder.OrWhere<Issue>(b => b.AnyEditionOk == true);
            }

            return Query(builder);
        }

        public Edition FindByTitle(int volumeMetadataId, string title)
        {
            return Query(Builder().Join<Edition, Issue>((e, b) => e.IssueId == b.Id)
                .Where<Issue>(b => b.VolumeMetadataId == volumeMetadataId)
                .Where<Edition>(e => e.Monitored == true)
                .Where<Edition>(e => e.Title == title))
                .FirstOrDefault();
        }

        public List<Edition> SetMonitored(Edition edition)
        {
            var allEditions = FindByIssue(new[] { edition.IssueId });
            allEditions.ForEach(r => r.Monitored = r.Id == edition.Id);
            Ensure.That(allEditions.Count(x => x.Monitored) == 1).IsTrue();
            UpdateMany(allEditions);
            return allEditions;
        }
    }
}
