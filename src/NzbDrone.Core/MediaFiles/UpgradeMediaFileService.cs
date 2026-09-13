using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues.Calibre;
using NzbDrone.Core.MediaFiles.IssueImport;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.MediaFiles
{
    public interface IUpgradeMediaFiles
    {
        IssueFileMoveResult UpgradeIssueFile(IssueFile issueFile, LocalIssue localIssue, bool copyOnly = false);
    }

    public class UpgradeMediaFileService : IUpgradeMediaFiles
    {
        private readonly IRecycleBinProvider _recycleBinProvider;
        private readonly IMediaFileService _mediaFileService;
        private readonly IMetadataTagService _metadataTagService;
        private readonly IMoveIssueFiles _issueFileMover;
        private readonly IDiskProvider _diskProvider;
        private readonly IRootFolderService _rootFolderService;
        private readonly ICalibreProxy _calibre;
        private readonly Logger _logger;

        public UpgradeMediaFileService(IRecycleBinProvider recycleBinProvider,
                                       IMediaFileService mediaFileService,
                                       IMetadataTagService metadataTagService,
                                       IMoveIssueFiles issueFileMover,
                                       IDiskProvider diskProvider,
                                       IRootFolderService rootFolderService,
                                       ICalibreProxy calibre,
                                       Logger logger)
        {
            _recycleBinProvider = recycleBinProvider;
            _mediaFileService = mediaFileService;
            _metadataTagService = metadataTagService;
            _issueFileMover = issueFileMover;
            _diskProvider = diskProvider;
            _rootFolderService = rootFolderService;
            _calibre = calibre;
            _logger = logger;
        }

        public IssueFileMoveResult UpgradeIssueFile(IssueFile issueFile, LocalIssue localIssue, bool copyOnly = false)
        {
            var moveFileResult = new IssueFileMoveResult();
            var existingFiles = localIssue.Issue.IssueFiles.Value;

            var rootFolderPath = _diskProvider.GetParentFolder(localIssue.Volume.Path);
            var rootFolder = _rootFolderService.GetBestRootFolder(rootFolderPath);
            var isCalibre = rootFolder.IsCalibreLibrary && rootFolder.CalibreSettings != null;

            var settings = rootFolder.CalibreSettings;

            // If there are existing issue files and the root folder is missing, throw, so the old file isn't left behind during the import process.
            if (existingFiles.Any() && !_diskProvider.FolderExists(rootFolderPath))
            {
                throw new RootFolderNotFoundException($"Root folder '{rootFolderPath}' was not found.");
            }

            foreach (var file in existingFiles)
            {
                var issueFilePath = file.Path;
                var subfolder = rootFolderPath.GetRelativePath(_diskProvider.GetParentFolder(issueFilePath));

                issueFile.CalibreId = file.CalibreId;

                if (_diskProvider.FileExists(issueFilePath))
                {
                    _logger.Debug("Removing existing issue file: {0} CalibreId: {1}", file, file.CalibreId);

                    if (!isCalibre)
                    {
                        _recycleBinProvider.DeleteFile(issueFilePath, subfolder);
                    }
                    else
                    {
                        var existing = _calibre.GetIssue(file.CalibreId, settings);
                        var existingFormats = existing.Formats.Keys;
                        _logger.Debug($"Removing existing formats {existingFormats.ConcatToString()} from calibre");
                        _calibre.RemoveFormats(file.CalibreId, existingFormats, settings);
                    }
                }

                moveFileResult.OldFiles.Add(file);
                _mediaFileService.Delete(file, DeleteMediaFileReason.Upgrade);
            }

            if (!isCalibre)
            {
                if (copyOnly)
                {
                    moveFileResult.IssueFile = _issueFileMover.CopyIssueFile(issueFile, localIssue);
                }
                else
                {
                    moveFileResult.IssueFile = _issueFileMover.MoveIssueFile(issueFile, localIssue);
                }

                _metadataTagService.WriteTags(issueFile, true);
            }
            else
            {
                var source = issueFile.Path;

                moveFileResult.IssueFile = _calibre.AddAndConvert(issueFile, settings);

                if (!copyOnly)
                {
                    _diskProvider.DeleteFile(source);
                }
            }

            return moveFileResult;
        }
    }
}
