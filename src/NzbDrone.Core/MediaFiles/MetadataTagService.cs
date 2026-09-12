using System.Collections.Generic;
using System.IO.Abstractions;
using NLog;
using NzbDrone.Core.Books;
using NzbDrone.Core.MediaFiles.Comics;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMetadataTagService
    {
        ParsedTrackInfo ReadTags(IFileInfo file);
        void WriteTags(BookFile trackfile, bool newDownload, bool force = false);
        void SyncTags(List<Edition> books);
        List<RetagBookFilePreview> GetRetagPreviewsByAuthor(int authorId);
        List<RetagBookFilePreview> GetRetagPreviewsByBook(int authorId);
    }

    public class MetadataTagService : IMetadataTagService,
        IExecute<RetagFilesCommand>,
        IExecute<RetagAuthorCommand>
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

        public void WriteTags(BookFile bookFile, bool newDownload, bool force = false)
        {
            _comicTagService.WriteTags(bookFile, newDownload, force);
        }

        public void SyncTags(List<Edition> editions)
        {
            _comicTagService.SyncTags(editions);
        }

        public List<RetagBookFilePreview> GetRetagPreviewsByAuthor(int authorId)
        {
            return _comicTagService.GetRetagPreviewsByAuthor(authorId);
        }

        public List<RetagBookFilePreview> GetRetagPreviewsByBook(int bookId)
        {
            return _comicTagService.GetRetagPreviewsByBook(bookId);
        }

        public void Execute(RetagFilesCommand message)
        {
            _comicTagService.RetagFiles(message);
        }

        public void Execute(RetagAuthorCommand message)
        {
            _comicTagService.RetagAuthor(message);
        }
    }
}
