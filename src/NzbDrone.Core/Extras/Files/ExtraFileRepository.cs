using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Extras.Files
{
    public interface IExtraFileRepository<TExtraFile> : IBasicRepository<TExtraFile>
        where TExtraFile : ExtraFile, new()
    {
        void DeleteForVolume(int volumeId);
        void DeleteForIssue(int volumeId, int issueId);
        void DeleteForIssueFile(int issueFileId);
        List<TExtraFile> GetFilesByVolume(int volumeId);
        List<TExtraFile> GetFilesByIssue(int volumeId, int issueId);
        List<TExtraFile> GetFilesByIssueFile(int issueFileId);
        TExtraFile FindByPath(int volumeId, string path);
    }

    public class ExtraFileRepository<TExtraFile> : BasicRepository<TExtraFile>, IExtraFileRepository<TExtraFile>
        where TExtraFile : ExtraFile, new()
    {
        public ExtraFileRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public void DeleteForVolume(int volumeId)
        {
            Delete(c => c.VolumeId == volumeId);
        }

        public void DeleteForIssue(int volumeId, int issueId)
        {
            Delete(c => c.VolumeId == volumeId && c.IssueId == issueId);
        }

        public void DeleteForIssueFile(int issueFileId)
        {
            Delete(c => c.IssueFileId == issueFileId);
        }

        public List<TExtraFile> GetFilesByVolume(int volumeId)
        {
            return Query(c => c.VolumeId == volumeId);
        }

        public List<TExtraFile> GetFilesByIssue(int volumeId, int issueId)
        {
            return Query(c => c.VolumeId == volumeId && c.IssueId == issueId);
        }

        public List<TExtraFile> GetFilesByIssueFile(int issueFileId)
        {
            return Query(c => c.IssueFileId == issueFileId);
        }

        public TExtraFile FindByPath(int volumeId, string path)
        {
            return Query(c => c.VolumeId == volumeId && c.RelativePath == path).SingleOrDefault();
        }
    }
}
