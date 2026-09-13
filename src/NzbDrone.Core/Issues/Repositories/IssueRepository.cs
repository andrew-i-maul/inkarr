using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Issues
{
    public interface IIssueRepository : IBasicRepository<Issue>
    {
        List<Issue> GetIssues(int volumeId);
        List<Issue> GetLastIssues(IEnumerable<int> volumeMetadataIds);
        List<Issue> GetNextIssues(IEnumerable<int> volumeMetadataIds);
        List<Issue> GetIssuesByVolumeMetadataId(int volumeMetadataId);
        List<Issue> GetIssuesForRefresh(int volumeMetadataId, List<string> foreignIds);
        List<Issue> GetIssuesByFileIds(IEnumerable<int> fileIds);
        Issue FindByTitle(int volumeMetadataId, string title);
        Issue FindById(string foreignIssueId);
        Issue FindBySlug(string titleSlug);
        PagingSpec<Issue> IssuesWithoutFiles(PagingSpec<Issue> pagingSpec);
        PagingSpec<Issue> IssuesWhereCutoffUnmet(PagingSpec<Issue> pagingSpec, List<QualitiesBelowCutoff> qualitiesBelowCutoff);
        List<Issue> IssuesBetweenDates(DateTime startDate, DateTime endDate, bool includeUnmonitored);
        List<Issue> VolumeIssuesBetweenDates(Volume volume, DateTime startDate, DateTime endDate, bool includeUnmonitored);
        void SetMonitoredFlat(Issue issue, bool monitored);
        void SetMonitored(IEnumerable<int> ids, bool monitored);
        List<Issue> GetVolumeIssuesWithFiles(Volume volume);
    }

    public class IssueRepository : BasicRepository<Issue>, IIssueRepository
    {
        public IssueRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<Issue> GetIssues(int volumeId)
        {
            return Query(Builder().Join<Issue, Volume>((l, r) => l.VolumeMetadataId == r.VolumeMetadataId).Where<Volume>(a => a.Id == volumeId));
        }

        public List<Issue> GetLastIssues(IEnumerable<int> volumeMetadataIds)
        {
            var now = DateTime.UtcNow;

            var inner = Builder()
                .Select("MIN(\"Issues\".\"Id\") as id, MAX(\"Issues\".\"ReleaseDate\") as date")
                .Where<Issue>(x => volumeMetadataIds.Contains(x.VolumeMetadataId) && x.ReleaseDate < now)
                .GroupBy<Issue>(x => x.VolumeMetadataId)
                .AddSelectTemplate(typeof(Issue));

            var outer = Builder()
                .Join($"({inner.RawSql}) ids on ids.id = \"Issues\".\"Id\" and ids.date = \"Issues\".\"ReleaseDate\"")
                .AddParameters(inner.Parameters);

            return Query(outer);
        }

        public List<Issue> GetNextIssues(IEnumerable<int> volumeMetadataIds)
        {
            var now = DateTime.UtcNow;

            var inner = Builder()
                .Select("MIN(\"Issues\".\"Id\") as id, MIN(\"Issues\".\"ReleaseDate\") as date")
                .Where<Issue>(x => volumeMetadataIds.Contains(x.VolumeMetadataId) && x.ReleaseDate > now)
                .GroupBy<Issue>(x => x.VolumeMetadataId)
                .AddSelectTemplate(typeof(Issue));

            var outer = Builder()
                .Join($"({inner.RawSql}) ids on ids.id = \"Issues\".\"Id\" and ids.date = \"Issues\".\"ReleaseDate\"")
                .AddParameters(inner.Parameters);

            return Query(outer);
        }

        public List<Issue> GetIssuesByVolumeMetadataId(int volumeMetadataId)
        {
            return Query(s => s.VolumeMetadataId == volumeMetadataId);
        }

        public List<Issue> GetIssuesForRefresh(int volumeMetadataId, List<string> foreignIds)
        {
            return Query(a => a.VolumeMetadataId == volumeMetadataId || foreignIds.Contains(a.ForeignIssueId));
        }

        public List<Issue> GetIssuesByFileIds(IEnumerable<int> fileIds)
        {
            return Query(new SqlBuilder(_database.DatabaseType)
                         .Join<Issue, Edition>((b, e) => b.Id == e.IssueId)
                         .Join<Edition, IssueFile>((l, r) => l.Id == r.EditionId)
                         .Where<IssueFile>(f => fileIds.Contains(f.Id)))
                .DistinctBy(x => x.Id)
                .ToList();
        }

        public Issue FindById(string foreignIssueId)
        {
            return Query(s => s.ForeignIssueId == foreignIssueId).SingleOrDefault();
        }

        public Issue FindBySlug(string titleSlug)
        {
            return Query(s => s.TitleSlug == titleSlug).SingleOrDefault();
        }

        //x.Id == null is converted to SQL, so warning incorrect
#pragma warning disable CS0472
        private SqlBuilder IssuesWithoutFilesBuilder(DateTime currentTime) => Builder()
            .Join<Issue, Volume>((l, r) => l.VolumeMetadataId == r.VolumeMetadataId)
            .Join<Volume, VolumeMetadata>((l, r) => l.VolumeMetadataId == r.Id)
            .Join<Issue, Edition>((b, e) => b.Id == e.IssueId)
            .LeftJoin<Edition, IssueFile>((t, f) => t.Id == f.EditionId)
            .Where<IssueFile>(f => f.Id == null)
            .Where<Edition>(e => e.Monitored == true)
            .Where<Issue>(a => a.ReleaseDate <= currentTime);
#pragma warning restore CS0472

        public PagingSpec<Issue> IssuesWithoutFiles(PagingSpec<Issue> pagingSpec)
        {
            var currentTime = DateTime.UtcNow;

            pagingSpec.Records = GetPagedRecords(IssuesWithoutFilesBuilder(currentTime), pagingSpec, PagedQuery);
            pagingSpec.TotalRecords = GetPagedRecordCount(IssuesWithoutFilesBuilder(currentTime).SelectCountDistinct<Issue>(x => x.Id), pagingSpec);

            return pagingSpec;
        }

        private SqlBuilder IssuesWhereCutoffUnmetBuilder(List<QualitiesBelowCutoff> qualitiesBelowCutoff) => Builder()
            .Join<Issue, Volume>((l, r) => l.VolumeMetadataId == r.VolumeMetadataId)
            .Join<Volume, VolumeMetadata>((l, r) => l.VolumeMetadataId == r.Id)
            .Join<Issue, Edition>((b, e) => b.Id == e.IssueId)
            .LeftJoin<Edition, IssueFile>((t, f) => t.Id == f.EditionId)
            .Where<Edition>(e => e.Monitored == true)
            .Where(BuildQualityCutoffWhereClause(qualitiesBelowCutoff));

        private string BuildQualityCutoffWhereClause(List<QualitiesBelowCutoff> qualitiesBelowCutoff)
        {
            var clauses = new List<string>();

            foreach (var profile in qualitiesBelowCutoff)
            {
                foreach (var belowCutoff in profile.QualityIds)
                {
                    clauses.Add(string.Format("(\"Volumes\".\"QualityProfileId\" = {0} AND \"IssueFiles\".\"Quality\" LIKE '%_quality_: {1},%')", profile.ProfileId, belowCutoff));
                }
            }

            return string.Format("({0})", string.Join(" OR ", clauses));
        }

        public PagingSpec<Issue> IssuesWhereCutoffUnmet(PagingSpec<Issue> pagingSpec, List<QualitiesBelowCutoff> qualitiesBelowCutoff)
        {
            pagingSpec.Records = GetPagedRecords(IssuesWhereCutoffUnmetBuilder(qualitiesBelowCutoff), pagingSpec, PagedQuery);

            var countTemplate = $"SELECT COUNT(*) FROM (SELECT /**select**/ FROM \"{TableMapping.Mapper.TableNameMapping(typeof(Issue))}\" /**join**/ /**innerjoin**/ /**leftjoin**/ /**where**/ /**groupby**/ /**having**/) AS \"Inner\"";
            pagingSpec.TotalRecords = GetPagedRecordCount(IssuesWhereCutoffUnmetBuilder(qualitiesBelowCutoff).Select(typeof(Issue)), pagingSpec, countTemplate);

            return pagingSpec;
        }

        public List<Issue> IssuesBetweenDates(DateTime startDate, DateTime endDate, bool includeUnmonitored)
        {
            var builder = Builder().Where<Issue>(rg => rg.ReleaseDate >= startDate && rg.ReleaseDate <= endDate);

            if (!includeUnmonitored)
            {
                builder = builder.Where<Issue>(e => e.Monitored == true)
                    .Join<Issue, Volume>((l, r) => l.VolumeMetadataId == r.VolumeMetadataId)
                    .Where<Volume>(e => e.Monitored == true);
            }

            return Query(builder);
        }

        public List<Issue> VolumeIssuesBetweenDates(Volume volume, DateTime startDate, DateTime endDate, bool includeUnmonitored)
        {
            var builder = Builder().Where<Issue>(rg => rg.ReleaseDate >= startDate &&
                                                 rg.ReleaseDate <= endDate &&
                                                 rg.VolumeMetadataId == volume.VolumeMetadataId);

            if (!includeUnmonitored)
            {
                builder = builder.Where<Issue>(e => e.Monitored == true)
                    .Join<Issue, Volume>((l, r) => l.VolumeMetadataId == r.VolumeMetadataId)
                    .Where<Volume>(e => e.Monitored == true);
            }

            return Query(builder);
        }

        public void SetMonitoredFlat(Issue issue, bool monitored)
        {
            issue.Monitored = monitored;
            SetFields(issue, p => p.Monitored);

            ModelUpdated(issue, true);
        }

        public void SetMonitored(IEnumerable<int> ids, bool monitored)
        {
            var issues = ids.Select(x => new Issue { Id = x, Monitored = monitored }).ToList();
            SetFields(issues, p => p.Monitored);
        }

        public Issue FindByTitle(int volumeMetadataId, string title)
        {
            var cleanTitle = Parser.Parser.CleanVolumeName(title);

            if (string.IsNullOrEmpty(cleanTitle))
            {
                cleanTitle = title;
            }

            return Query(s => (s.CleanTitle == cleanTitle || s.Title == title) && s.VolumeMetadataId == volumeMetadataId)
                .ExclusiveOrDefault();
        }

        public List<Issue> GetVolumeIssuesWithFiles(Volume volume)
        {
            return Query(Builder()
                         .Join<Issue, Edition>((b, e) => b.Id == e.IssueId)
                         .Join<Edition, IssueFile>((t, f) => t.Id == f.EditionId)
                         .Where<Issue>(x => x.VolumeMetadataId == volume.VolumeMetadataId)
                         .Where<Edition>(e => e.Monitored == true));
        }
    }
}
