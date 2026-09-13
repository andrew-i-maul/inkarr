using System.Collections.Generic;
using System.IO.Abstractions;
using NLog;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.Comics;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMetadataTagService
    {
        ParsedTrackInfo ReadTags(IFileInfo file);
        void WriteTags(IssueFile trackfile, bool newDownload, bool force = false);
        void SyncTags(List<Edition> issues);
        List<RetagIssueFilePreview> GetRetagPreviewsByVolume(int volumeId);
        List<RetagIssueFilePreview> GetRetagPreviewsByIssue(int volumeId);
    }

    public class MetadataTagService : IMetadataTagService,
        IExecute<RetagFilesCommand>,
        IExecute<RetagVolumeCommand>
    {
        private readonly IComicTagService _comicTagService;
        private readonly Logger _logger;

        public MetadataTagService(IComicTagService comicTagService,
            Logger logger)
        {
            _comicTagService = comicTagService;

            _logger = logger;
        }

        public ParsedTrackInfo ReadTags(IFileInfo file)
        {
            return _comicTagService.ReadTags(file);
        }

        public void WriteTags(IssueFile issueFile, bool newDownload, bool force = false)
        {
            _comicTagService.WriteTags(issueFile, newDownload, force);
        }

        public void SyncTags(List<Edition> editions)
        {
            _comicTagService.SyncTags(editions);
        }

        public List<RetagIssueFilePreview> GetRetagPreviewsByVolume(int volumeId)
        {
            return _comicTagService.GetRetagPreviewsByVolume(volumeId);
        }

        public List<RetagIssueFilePreview> GetRetagPreviewsByIssue(int issueId)
        {
            return _comicTagService.GetRetagPreviewsByIssue(issueId);
        }

        public void Execute(RetagFilesCommand message)
        {
            _comicTagService.RetagFiles(message);
        }

        public void Execute(RetagVolumeCommand message)
        {
            _comicTagService.RetagVolume(message);
        }
    }
}
