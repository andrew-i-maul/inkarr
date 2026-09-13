using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Calibre;
using NzbDrone.Core.MediaFiles.Azw;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.RootFolders;
using PdfSharpCore.Pdf.IO;
using VersOne.Epub;
using VersOne.Epub.Schema;

namespace NzbDrone.Core.MediaFiles
{
    public interface IEIssueTagService
    {
        ParsedTrackInfo ReadTags(IFileInfo file);
        void WriteTags(IssueFile trackfile, bool newDownload, bool force = false);
        void SyncTags(List<Edition> issues);
        List<RetagIssueFilePreview> GetRetagPreviewsByVolume(int volumeId);
        List<RetagIssueFilePreview> GetRetagPreviewsByIssue(int issueId);
        void RetagFiles(RetagFilesCommand message);
        void RetagVolume(RetagVolumeCommand message);
    }

    public class EIssueTagService : IEIssueTagService
    {
        private readonly IVolumeService _volumeService;
        private readonly IMediaFileService _mediaFileService;
        private readonly IRootFolderService _rootFolderService;
        private readonly IConfigService _configService;
        private readonly ICalibreProxy _calibre;
        private readonly Logger _logger;

        public EIssueTagService(IVolumeService volumeService,
            IMediaFileService mediaFileService,
            IRootFolderService rootFolderService,
            IConfigService configService,
            ICalibreProxy calibre,
            Logger logger)
        {
            _volumeService = volumeService;
            _mediaFileService = mediaFileService;
            _rootFolderService = rootFolderService;
            _configService = configService;
            _calibre = calibre;

            _logger = logger;
        }

        public ParsedTrackInfo ReadTags(IFileInfo file)
        {
            var extension = file.Extension.ToLower();
            _logger.Trace($"Got extension '{extension}'");

            switch (extension)
            {
                case ".pdf":
                    return ReadPdf(file.FullName);
                case ".epub":
                case ".kepub":
                    return ReadEpub(file.FullName);
                case ".azw3":
                case ".mobi":
                    return ReadAzw3(file.FullName);
                default:
                    return Parser.Parser.ParseTitle(file.FullName);
            }
        }

        public void WriteTags(IssueFile issueFile, bool newDownload, bool force = false)
        {
            if (!force)
            {
                if (_configService.WriteIssueTags == WriteIssueTagsType.NewFiles && !newDownload)
                {
                    return;
                }
            }

            _logger.Debug($"Writing tags for {issueFile}");

            WriteTagsInternal(issueFile, _configService.UpdateCovers, _configService.EmbedMetadata);
        }

        public void SyncTags(List<Edition> editions)
        {
            if (_configService.WriteIssueTags != WriteIssueTagsType.Sync)
            {
                return;
            }

            // get the tracks to update
            foreach (var edition in editions)
            {
                var issueFiles = edition.IssueFiles.Value;

                _logger.Debug($"Syncing eissue tags for {edition}");

                foreach (var file in issueFiles.Where(x => x.CalibreId != 0))
                {
                    // populate tracks (which should also have release/issue/volume set) because
                    // not all of the updates will have been committed to the database yet
                    file.Edition = edition;

                    WriteTagsInternal(file, _configService.UpdateCovers, _configService.EmbedMetadata);
                }
            }
        }

        public List<RetagIssueFilePreview> GetRetagPreviewsByVolume(int volumeId)
        {
            var files = _mediaFileService.GetFilesByVolume(volumeId);

            return GetPreviews(files).ToList();
        }

        public List<RetagIssueFilePreview> GetRetagPreviewsByIssue(int issueId)
        {
            var files = _mediaFileService.GetFilesByIssue(issueId);

            return GetPreviews(files).ToList();
        }

        public void RetagFiles(RetagFilesCommand message)
        {
            var volume = _volumeService.GetVolume(message.VolumeId);
            var files = _mediaFileService.Get(message.Files);

            _logger.ProgressInfo("Re-tagging {0} eissue files for {1}", files.Count, volume.Name);

            foreach (var file in files.Where(x => x.CalibreId != 0))
            {
                WriteTagsInternal(file, message.UpdateCovers, message.EmbedMetadata);
            }

            _logger.ProgressInfo("Selected eissue files re-tagged for {0}", volume.Name);
        }

        public void RetagVolume(RetagVolumeCommand message)
        {
            _logger.Debug("Re-tagging all eissue files for selected volumes");
            var volumesToRename = _volumeService.GetVolumes(message.VolumeIds);

            foreach (var volume in volumesToRename)
            {
                var files = _mediaFileService.GetFilesByVolume(volume.Id);

                _logger.ProgressInfo("Re-tagging all eissue files for volume: {0}", volume.Name);

                foreach (var file in files.Where(x => x.CalibreId != 0))
                {
                    WriteTagsInternal(file, message.UpdateCovers, message.EmbedMetadata);
                }

                _logger.ProgressInfo("All eissue files re-tagged for {0}", volume.Name);
            }
        }

        private void WriteTagsInternal(IssueFile file, bool updateCover, bool embedMetadata)
        {
            if (file.CalibreId == 0)
            {
                _logger.Trace($"No calibre id for {file.Path}, skipping writing tags");
            }

            var rootFolder = _rootFolderService.GetBestRootFolder(file.Path);

            if (rootFolder == null)
            {
                throw new Exception($"File '{file.Path}' is not in a root folder.");
            }

            _calibre.SetFields(file, rootFolder.CalibreSettings, updateCover, embedMetadata);
        }

        private IEnumerable<RetagIssueFilePreview> GetPreviews(List<IssueFile> files)
        {
            var calibreFiles = files.Where(x => x.CalibreId > 0).OrderBy(x => x.Edition.Value.Title).ToList();

            var rootFolderPairs = calibreFiles.Select(x => Tuple.Create(x, _rootFolderService.GetBestRootFolder(x.Path)));

            var rootFolderGroups = rootFolderPairs.GroupBy(x => x.Item2.Path);

            var calibreIssues = new List<CalibreIssue>();
            foreach (var group in rootFolderGroups)
            {
                var rootFolder = group.First().Item2;
                var issues = _calibre.GetIssues(group.Select(x => x.Item1.CalibreId).ToList(), rootFolder.CalibreSettings);
                calibreIssues.AddRange(issues);
            }

            var dict = calibreIssues.ToDictionary(x => x.Id);

            foreach (var file in calibreFiles)
            {
                var edition = file.Edition.Value;
                var issue = edition.Issue.Value;
                var serieslink = issue.SeriesLinks.Value.OrderBy(x => x.SeriesPosition).FirstOrDefault(x => x.Series.Value.Title.IsNotNullOrWhiteSpace());

                var series = serieslink?.Series.Value;
                double? seriesIndex = null;
                if (double.TryParse(serieslink?.Position, out var index))
                {
                    _logger.Trace($"Parsed {serieslink?.Position} as {index}");
                    seriesIndex = index;
                }

                var oldTags = dict[file.CalibreId];

                var textInfo = CultureInfo.InvariantCulture.TextInfo;
                var genres = issue.Genres.Select(x => textInfo.ToTitleCase(x.Replace('-', ' '))).ToList();

                var newTags = new CalibreIssue
                {
                    Title = edition.Title,
                    Volumes = new List<string> { file.Volume.Value.Name },
                    PubDate = issue.ReleaseDate,
                    Publisher = edition.Publisher,
                    Languages = new List<string> { edition.Language.CanonicalizeLanguage() },
                    Tags = genres,
                    Comments = edition.Overview,
                    Rating = (int)(edition.Ratings.Value * 2) / 2.0,
                    Identifiers = new Dictionary<string, string>
                    {
                        { "isbn", edition.Isbn13 },
                        { "asin", edition.Asin },
                        { "goodreads", edition.ForeignEditionId }
                    },
                    Series = series?.Title,
                    Position = seriesIndex
                };

                var diff = oldTags.Diff(newTags);

                if (diff.Any())
                {
                    yield return new RetagIssueFilePreview
                    {
                        VolumeId = file.Volume.Value.Id,
                        IssueId = file.Edition.Value.Id,
                        IssueFileId = file.Id,
                        Path = file.Path,
                        Changes = diff
                    };
                }
            }
        }

        private ParsedTrackInfo ReadEpub(string file)
        {
            _logger.Trace($"Reading {file}");
            var result = new ParsedTrackInfo
            {
                Quality = new QualityModel
                {
                    Quality = Quality.EPUB,
                    QualityDetectionSource = QualityDetectionSource.TagLib
                }
            };

            try
            {
                using (var issueRef = EpubReader.OpenIssue(file))
                {
                    result.Volumes = issueRef.VolumeList;
                    result.IssueTitle = issueRef.Title;

                    var meta = issueRef.Schema.Package.Metadata;

                    _logger.Trace(meta.ToJson());

                    result.Isbn = GetIsbn(meta?.Identifiers);
                    result.Asin = meta?.Identifiers?.FirstOrDefault(x => x.Scheme?.ToLower().Contains("asin") ?? false)?.Identifier;
                    result.Language = meta?.Languages?.FirstOrDefault();
                    result.Publisher = meta?.Publishers?.FirstOrDefault();
                    result.Disambiguation = meta?.Description;

                    result.SeriesTitle = meta?.MetaItems?.FirstOrDefault(x => x.Name == "calibre:series")?.Content;
                    result.SeriesIndex = meta?.MetaItems?.FirstOrDefault(x => x.Name == "calibre:series_index")?.Content;
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error reading epub");
                result.Quality.QualityDetectionSource = QualityDetectionSource.Extension;
            }

            _logger.Trace($"Got:\n{result.ToJson()}");

            return result;
        }

        private ParsedTrackInfo ReadAzw3(string file)
        {
            _logger.Trace($"Reading {file}");
            var result = new ParsedTrackInfo();

            try
            {
                var issue = new Azw3File(file);
                result.Volumes = issue.Volumes;
                result.IssueTitle = issue.Title;
                result.Isbn = StripIsbn(issue.Isbn);
                result.Asin = issue.Asin;
                result.Language = issue.Language;
                result.Disambiguation = issue.Description;
                result.Publisher = issue.Publisher;
                result.Label = issue.Imprint;
                result.Source = issue.Source;

                result.Quality = new QualityModel
                {
                    Quality = issue.Version <= 6 ? Quality.MOBI : Quality.AZW3,
                    QualityDetectionSource = QualityDetectionSource.TagLib
                };
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error reading file");

                result.Quality = new QualityModel
                {
                    Quality = Path.GetExtension(file) == ".mobi" ? Quality.MOBI : Quality.AZW3,
                    QualityDetectionSource = QualityDetectionSource.Extension
                };
            }

            _logger.Trace($"Got {result.ToJson()}");

            return result;
        }

        private ParsedTrackInfo ReadPdf(string file)
        {
            _logger.Trace($"Reading {file}");
            var result = new ParsedTrackInfo
            {
                Quality = new QualityModel
                {
                    Quality = Quality.PDF,
                    QualityDetectionSource = QualityDetectionSource.TagLib
                }
            };

            try
            {
                var issue = PdfReader.Open(file, PdfDocumentOpenMode.InformationOnly);
                if (issue.Info != null)
                {
                    result.Volumes = new List<string> { issue.Info.Author };
                    result.IssueTitle = issue.Info.Title;

                    _logger.Trace(issue.Info.ToJson());
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error reading pdf");
                result.Quality.QualityDetectionSource = QualityDetectionSource.Extension;
            }

            _logger.Trace($"Got:\n{result.ToJson()}");

            return result;
        }

        public string GetIsbn(IEnumerable<EpubMetadataIdentifier> ids)
        {
            var candidates = ids.Select(x => StripIsbn(x?.Identifier))
                .Where(x => x != null)
                .OrderByDescending(x => x.Length);

            return candidates.FirstOrDefault(x => x.StartsWith("978"))
                ?? candidates.FirstOrDefault(x => x.StartsWith("979"))
                ?? candidates.FirstOrDefault();
        }

        private string GetIsbnChars(string input)
        {
            if (input == null)
            {
                return null;
            }

            return new string(input.Where(c => char.IsDigit(c) || c == 'X' || c == 'x').ToArray());
        }

        private string StripIsbn(string input)
        {
            var isbn = GetIsbnChars(input);

            if (isbn == null)
            {
                return null;
            }
            else if ((isbn.Length == 10 && ValidateIsbn10(isbn)) ||
                (isbn.Length == 13 && ValidateIsbn13(isbn)))
            {
                return isbn;
            }

            return null;
        }

        private static char Isbn10Checksum(string isbn)
        {
            var sum = 0;
            for (var i = 0; i < 9; i++)
            {
                sum += int.Parse(isbn[i].ToString()) * (10 - i);
            }

            var result = sum % 11;

            if (result == 0)
            {
                return '0';
            }
            else if (result == 1)
            {
                return 'X';
            }

            return (11 - result).ToString()[0];
        }

        private static char Isbn13Checksum(string isbn)
        {
            var result = 0;
            for (var i = 0; i < 12; i++)
            {
                result += int.Parse(isbn[i].ToString()) * ((i % 2 == 0) ? 1 : 3);
            }

            result %= 10;

            return result == 0 ? '0' : (10 - result).ToString()[0];
        }

        private static bool ValidateIsbn10(string isbn)
        {
            return ulong.TryParse(isbn.Substring(0, 9), out _) && isbn[9] == Isbn10Checksum(isbn);
        }

        private static bool ValidateIsbn13(string isbn)
        {
            return ulong.TryParse(isbn, out _) && isbn[12] == Isbn13Checksum(isbn);
        }
    }
}
