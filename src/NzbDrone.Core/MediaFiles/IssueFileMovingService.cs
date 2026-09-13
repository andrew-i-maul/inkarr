using System;
using System.IO;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.MediaFiles.IssueImport;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMoveIssueFiles
    {
        IssueFile MoveIssueFile(IssueFile issueFile, Volume volume);
        IssueFile MoveIssueFile(IssueFile issueFile, LocalIssue localIssue);
        IssueFile CopyIssueFile(IssueFile issueFile, LocalIssue localIssue);
    }

    public class IssueFileMovingService : IMoveIssueFiles
    {
        private readonly IEditionService _editionService;
        private readonly IUpdateIssueFileService _updateIssueFileService;
        private readonly IBuildFileNames _buildFileNames;
        private readonly IDiskTransferService _diskTransferService;
        private readonly IDiskProvider _diskProvider;
        private readonly IRootFolderWatchingService _rootFolderWatchingService;
        private readonly IMediaFileAttributeService _mediaFileAttributeService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public IssueFileMovingService(IEditionService editionService,
                                      IUpdateIssueFileService updateIssueFileService,
                                      IBuildFileNames buildFileNames,
                                      IDiskTransferService diskTransferService,
                                      IDiskProvider diskProvider,
                                      IRootFolderWatchingService rootFolderWatchingService,
                                      IMediaFileAttributeService mediaFileAttributeService,
                                      IEventAggregator eventAggregator,
                                      IConfigService configService,
                                      Logger logger)
        {
            _editionService = editionService;
            _updateIssueFileService = updateIssueFileService;
            _buildFileNames = buildFileNames;
            _diskTransferService = diskTransferService;
            _diskProvider = diskProvider;
            _rootFolderWatchingService = rootFolderWatchingService;
            _mediaFileAttributeService = mediaFileAttributeService;
            _eventAggregator = eventAggregator;
            _configService = configService;
            _logger = logger;
        }

        public IssueFile MoveIssueFile(IssueFile issueFile, Volume volume)
        {
            var edition = _editionService.GetEdition(issueFile.EditionId);
            var newFileName = _buildFileNames.BuildIssueFileName(volume, edition, issueFile);
            var filePath = _buildFileNames.BuildIssueFilePath(volume, edition, newFileName, Path.GetExtension(issueFile.Path));

            EnsureIssueFolder(issueFile, volume, edition.Issue.Value, filePath);

            _logger.Debug("Renaming issue file: {0} to {1}", issueFile, filePath);

            return TransferFile(issueFile, volume, issueFile.Edition.Value.Issue.Value, filePath, TransferMode.Move);
        }

        public IssueFile MoveIssueFile(IssueFile issueFile, LocalIssue localIssue)
        {
            var newFileName = _buildFileNames.BuildIssueFileName(localIssue.Volume, localIssue.Edition, issueFile);
            var filePath = _buildFileNames.BuildIssueFilePath(localIssue.Volume, localIssue.Edition, newFileName, Path.GetExtension(localIssue.Path));

            EnsureTrackFolder(issueFile, localIssue, filePath);

            _logger.Debug("Moving issue file: {0} to {1}", issueFile.Path, filePath);

            return TransferFile(issueFile, localIssue.Volume, localIssue.Issue, filePath, TransferMode.Move);
        }

        public IssueFile CopyIssueFile(IssueFile issueFile, LocalIssue localIssue)
        {
            var newFileName = _buildFileNames.BuildIssueFileName(localIssue.Volume, localIssue.Edition, issueFile);
            var filePath = _buildFileNames.BuildIssueFilePath(localIssue.Volume, localIssue.Edition, newFileName, Path.GetExtension(localIssue.Path));

            EnsureTrackFolder(issueFile, localIssue, filePath);

            if (_configService.CopyUsingHardlinks)
            {
                _logger.Debug("Hardlinking issue file: {0} to {1}", issueFile.Path, filePath);
                return TransferFile(issueFile, localIssue.Volume, localIssue.Issue, filePath, TransferMode.HardLinkOrCopy);
            }

            _logger.Debug("Copying issue file: {0} to {1}", issueFile.Path, filePath);
            return TransferFile(issueFile, localIssue.Volume, localIssue.Issue, filePath, TransferMode.Copy);
        }

        private IssueFile TransferFile(IssueFile issueFile, Volume volume, Issue issue, string destinationFilePath, TransferMode mode)
        {
            Ensure.That(issueFile, () => issueFile).IsNotNull();
            Ensure.That(volume, () => volume).IsNotNull();
            Ensure.That(destinationFilePath, () => destinationFilePath).IsValidPath(PathValidationType.CurrentOs);

            var issueFilePath = issueFile.Path;

            if (!_diskProvider.FileExists(issueFilePath))
            {
                throw new FileNotFoundException("Issue file path does not exist", issueFilePath);
            }

            if (issueFilePath == destinationFilePath)
            {
                throw new SameFilenameException("File not moved, source and destination are the same", issueFilePath);
            }

            _rootFolderWatchingService.ReportFileSystemChangeBeginning(issueFilePath, destinationFilePath);
            _diskTransferService.TransferFile(issueFilePath, destinationFilePath, mode);

            issueFile.Path = destinationFilePath;

            _updateIssueFileService.ChangeFileDateForFile(issueFile, volume, issue);

            try
            {
                _mediaFileAttributeService.SetFolderLastWriteTime(volume.Path, issueFile.DateAdded);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to set last write time");
            }

            _mediaFileAttributeService.SetFilePermissions(destinationFilePath);

            return issueFile;
        }

        private void EnsureTrackFolder(IssueFile issueFile, LocalIssue localIssue, string filePath)
        {
            EnsureIssueFolder(issueFile, localIssue.Volume, localIssue.Issue, filePath);
        }

        private void EnsureIssueFolder(IssueFile issueFile, Volume volume, Issue issue, string filePath)
        {
            var trackFolder = Path.GetDirectoryName(filePath);
            var issueFolder = _buildFileNames.BuildIssuePath(volume);
            var volumeFolder = volume.Path;
            var rootFolder = new OsPath(volumeFolder).Directory.FullPath;

            if (!_diskProvider.FolderExists(rootFolder))
            {
                throw new RootFolderNotFoundException(string.Format("Root folder '{0}' was not found.", rootFolder));
            }

            var changed = false;
            var newEvent = new TrackFolderCreatedEvent(volume, issueFile);

            _rootFolderWatchingService.ReportFileSystemChangeBeginning(volumeFolder, issueFolder, trackFolder);

            if (!_diskProvider.FolderExists(volumeFolder))
            {
                CreateFolder(volumeFolder);
                newEvent.VolumeFolder = volumeFolder;
                changed = true;
            }

            if (volumeFolder != issueFolder && !_diskProvider.FolderExists(issueFolder))
            {
                CreateFolder(issueFolder);
                newEvent.IssueFolder = issueFolder;
                changed = true;
            }

            if (issueFolder != trackFolder && !_diskProvider.FolderExists(trackFolder))
            {
                CreateFolder(trackFolder);
                newEvent.TrackFolder = trackFolder;
                changed = true;
            }

            if (changed)
            {
                _eventAggregator.PublishEvent(newEvent);
            }
        }

        private void CreateFolder(string directoryName)
        {
            Ensure.That(directoryName, () => directoryName).IsNotNullOrWhiteSpace();

            var parentFolder = new OsPath(directoryName).Directory.FullPath;
            if (!_diskProvider.FolderExists(parentFolder))
            {
                CreateFolder(parentFolder);
            }

            try
            {
                _diskProvider.CreateFolder(directoryName);
            }
            catch (IOException ex)
            {
                _logger.Error(ex, "Unable to create directory: {0}", directoryName);
            }

            _mediaFileAttributeService.SetFolderPermissions(directoryName);
        }
    }
}
