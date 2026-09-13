using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.MediaFiles.Comics
{
    public interface IComicTagService
    {
        ParsedTrackInfo ReadTags(IFileInfo file);
        void WriteTags(IssueFile issueFile, bool newDownload, bool force = false);
        void SyncTags(List<Edition> editions);
        List<RetagIssueFilePreview> GetRetagPreviewsByVolume(int volumeId);
        List<RetagIssueFilePreview> GetRetagPreviewsByIssue(int issueId);
        void RetagFiles(RetagFilesCommand message);
        void RetagVolume(RetagVolumeCommand message);
    }

    // Replaces EIssueTagService/AudioTagService for comic archives. Unlike those, this does not write
    // metadata back into the file: cbz/cbr don't have a widely-supported "embed and re-save" workflow
    // comparable to ID3/EPUB-OPF tag writing, and ComicVine (Phase 1) is the source of truth for
    // metadata anyway. WriteTags/SyncTags/Retag* are deliberate no-ops, not unfinished work.
    public class ComicTagService : IComicTagService
    {
        private readonly IComicArchiveReader _archiveReader;
        private readonly Logger _logger;

        public ComicTagService(IComicArchiveReader archiveReader, Logger logger)
        {
            _archiveReader = archiveReader;
            _logger = logger;
        }

        public ParsedTrackInfo ReadTags(IFileInfo file)
        {
            var extension = file.Extension?.ToLowerInvariant();
            var quality = MediaFileExtensions.GetQualityForExtension(extension);

            var result = new ParsedTrackInfo
            {
                Quality = new QualityModel
                {
                    Quality = quality,
                    QualityDetectionSource = QualityDetectionSource.Extension
                }
            };

            try
            {
                var contents = _archiveReader.Read(file.FullName, readCoverImage: false);
                var comicInfo = contents.ComicInfo;

                if (comicInfo != null)
                {
                    result.IssueTitle = comicInfo.Title.IsNotNullOrWhiteSpace() ? comicInfo.Title : null;
                    result.SeriesTitle = comicInfo.Series;
                    result.SeriesIndex = comicInfo.Number;
                    result.Publisher = comicInfo.Imprint.IsNotNullOrWhiteSpace() ? comicInfo.Imprint : comicInfo.Publisher;
                    result.Disambiguation = comicInfo.Summary;
                    result.Language = comicInfo.LanguageISO;

                    if (comicInfo.Writer.IsNotNullOrWhiteSpace())
                    {
                        result.Volumes = comicInfo.Writer.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
                    }

                    if (comicInfo.Year.HasValue)
                    {
                        result.Year = (uint)comicInfo.Year.Value;
                    }

                    result.Quality.QualityDetectionSource = QualityDetectionSource.TagLib;
                }
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, $"Unable to read comic archive contents for '{file.FullName}'");
            }

            return result;
        }

        public void WriteTags(IssueFile issueFile, bool newDownload, bool force = false)
        {
        }

        public void SyncTags(List<Edition> editions)
        {
        }

        public List<RetagIssueFilePreview> GetRetagPreviewsByVolume(int volumeId)
        {
            return new List<RetagIssueFilePreview>();
        }

        public List<RetagIssueFilePreview> GetRetagPreviewsByIssue(int issueId)
        {
            return new List<RetagIssueFilePreview>();
        }

        public void RetagFiles(RetagFilesCommand message)
        {
        }

        public void RetagVolume(RetagVolumeCommand message)
        {
        }
    }
}
