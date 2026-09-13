using System.Collections.Generic;
using System.IO;
using System.Linq;
using NzbDrone.Common;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMediaFileRepository : IBasicRepository<IssueFile>
    {
        List<IssueFile> GetFilesByVolume(int volumeId);
        List<IssueFile> GetFilesByVolumeMetadataId(int volumeMetadataId);
        List<IssueFile> GetFilesByIssue(int issueId);
        List<IssueFile> GetFilesByEdition(int editionId);
        List<IssueFile> GetUnmappedFiles();
        List<IssueFile> GetFilesWithBasePath(string path);
        List<IssueFile> GetFileWithPath(List<string> paths);
        IssueFile GetFileWithPath(string path);
        void DeleteFilesByIssue(int issueId);
        void UnlinkFilesByIssue(int issueId);
    }

    public class MediaFileRepository : BasicRepository<IssueFile>, IMediaFileRepository
    {
        public MediaFileRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        // always join with all the other good stuff
        // needed more often than not so better to load it all now
        protected override SqlBuilder Builder() => new SqlBuilder(_database.DatabaseType)
            .LeftJoin<IssueFile, Edition>((b, e) => b.EditionId == e.Id)
            .LeftJoin<Edition, Issue>((e, b) => e.IssueId == b.Id)
            .LeftJoin<Issue, Volume>((issue, volume) => issue.VolumeMetadataId == volume.VolumeMetadataId)
            .LeftJoin<Volume, VolumeMetadata>((a, m) => a.VolumeMetadataId == m.Id);

        protected override List<IssueFile> Query(SqlBuilder builder) => Query(_database, builder).ToList();

        public static IEnumerable<IssueFile> Query(IDatabase database, SqlBuilder builder)
        {
            return database.QueryJoined<IssueFile, Edition, Issue, Volume, VolumeMetadata>(builder, (file, edition, issue, volume, metadata) => Map(file, edition, issue, volume, metadata));
        }

        private static IssueFile Map(IssueFile file, Edition edition, Issue issue, Volume volume, VolumeMetadata metadata)
        {
            file.Edition = edition;

            if (edition != null)
            {
                edition.Issue = issue;
            }

            if (volume != null)
            {
                volume.Metadata = metadata;
            }

            file.Volume = volume;

            return file;
        }

        public List<IssueFile> GetFilesByVolume(int volumeId)
        {
            return Query(Builder().Where<Volume>(a => a.Id == volumeId));
        }

        public List<IssueFile> GetFilesByVolumeMetadataId(int volumeMetadataId)
        {
            return Query(Builder().Where<Issue>(b => b.VolumeMetadataId == volumeMetadataId));
        }

        public List<IssueFile> GetFilesByIssue(int issueId)
        {
            return Query(Builder().Where<Issue>(b => b.Id == issueId));
        }

        public List<IssueFile> GetFilesByEdition(int editionId)
        {
            return Query(Builder().Where<IssueFile>(f => f.EditionId == editionId));
        }

        public List<IssueFile> GetUnmappedFiles()
        {
            return _database.Query<IssueFile>(new SqlBuilder(_database.DatabaseType).Select(typeof(IssueFile))
                                              .Where<IssueFile>(t => t.EditionId == 0)).ToList();
        }

        public void DeleteFilesByIssue(int issueId)
        {
            var fileIds = GetFilesByIssue(issueId).Select(x => x.Id).ToList();
            Delete(x => fileIds.Contains(x.Id));
        }

        public void UnlinkFilesByIssue(int issueId)
        {
            var files = GetFilesByIssue(issueId);
            files.ForEach(x => x.EditionId = 0);
            SetFields(files, f => f.EditionId);
        }

        public List<IssueFile> GetFilesWithBasePath(string path)
        {
            // ensure path ends with a single trailing path separator to avoid matching partial paths
            var safePath = path.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return _database.Query<IssueFile>(new SqlBuilder(_database.DatabaseType).Where<IssueFile>(x => x.Path.StartsWith(safePath))).ToList();
        }

        public IssueFile GetFileWithPath(string path)
        {
            return Query(x => x.Path == path).SingleOrDefault();
        }

        public List<IssueFile> GetFileWithPath(List<string> paths)
        {
            // use more limited join for speed
            var builder = new SqlBuilder(_database.DatabaseType)
                .LeftJoin<IssueFile, Edition>((f, t) => f.EditionId == t.Id);

            var all = _database.QueryJoined<IssueFile, Edition>(builder, (file, issue) => MapTrack(file, issue)).ToList();

            var joined = all.Join(paths, x => x.Path, x => x, (file, path) => file, PathEqualityComparer.Instance).ToList();
            return joined;
        }

        private IssueFile MapTrack(IssueFile file, Edition issue)
        {
            file.Edition = issue;
            return file;
        }
    }
}
