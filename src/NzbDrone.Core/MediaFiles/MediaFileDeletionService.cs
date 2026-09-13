using System;
using System.Net;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Calibre;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.MediaFiles
{
    public interface IDeleteMediaFiles
    {
        void DeleteTrackFile(Volume volume, IssueFile issueFile);
        void DeleteTrackFile(IssueFile issueFile, string subfolder = "");
    }

    public class MediaFileDeletionService : IDeleteMediaFiles,
                                            IHandle<VolumeDeletedEvent>,
                                            IHandleAsync<VolumeDeletedEvent>,
                                            IHandleAsync<IssueDeletedEvent>,
                                            IHandle<IssueFileDeletedEvent>
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IRecycleBinProvider _recycleBinProvider;
        private readonly IMediaFileService _mediaFileService;
        private readonly IVolumeService _volumeService;
        private readonly IConfigService _configService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IRootFolderService _rootFolderService;
        private readonly ICalibreProxy _calibre;
        private readonly Logger _logger;

        public MediaFileDeletionService(IDiskProvider diskProvider,
                                        IRecycleBinProvider recycleBinProvider,
                                        IMediaFileService mediaFileService,
                                        IVolumeService volumeService,
                                        IConfigService configService,
                                        IEventAggregator eventAggregator,
                                        IRootFolderService rootFolderService,
                                        ICalibreProxy calibre,
                                        Logger logger)
        {
            _diskProvider = diskProvider;
            _recycleBinProvider = recycleBinProvider;
            _mediaFileService = mediaFileService;
            _volumeService = volumeService;
            _configService = configService;
            _eventAggregator = eventAggregator;
            _rootFolderService = rootFolderService;
            _calibre = calibre;
            _logger = logger;
        }

        public void DeleteTrackFile(Volume volume, IssueFile issueFile)
        {
            var fullPath = issueFile.Path;
            var rootFolder = _diskProvider.GetParentFolder(volume.Path);

            if (!_diskProvider.FolderExists(rootFolder))
            {
                _logger.Warn("Volume's root folder ({0}) doesn't exist.", rootFolder);
                throw new NzbDroneClientException(HttpStatusCode.Conflict, "Volume's root folder ({0}) doesn't exist.", rootFolder);
            }

            if (_diskProvider.GetDirectories(rootFolder).Empty())
            {
                _logger.Warn("Volume's root folder ({0}) is empty.", rootFolder);
                throw new NzbDroneClientException(HttpStatusCode.Conflict, "Volume's root folder ({0}) is empty.", rootFolder);
            }

            if (_diskProvider.FolderExists(volume.Path))
            {
                var subfolder = _diskProvider.GetParentFolder(volume.Path).GetRelativePath(_diskProvider.GetParentFolder(fullPath));
                DeleteTrackFile(issueFile, subfolder);
            }
            else
            {
                // delete from db even if the volume folder is missing
                _mediaFileService.Delete(issueFile, DeleteMediaFileReason.Manual);
            }
        }

        public void DeleteTrackFile(IssueFile issueFile, string subfolder = "")
        {
            var fullPath = issueFile.Path;

            if (_diskProvider.FileExists(fullPath))
            {
                _logger.Info("Deleting issue file: {0}", fullPath);
                DeleteFile(issueFile, subfolder);
            }

            // Delete the track file from the database to clean it up even if the file was already deleted
            _mediaFileService.Delete(issueFile, DeleteMediaFileReason.Manual);

            _eventAggregator.PublishEvent(new DeleteCompletedEvent());
        }

        private void DeleteFile(IssueFile issueFile, string subfolder = "")
        {
            var rootFolder = _rootFolderService.GetBestRootFolder(issueFile.Path);
            var isCalibre = rootFolder.IsCalibreLibrary && rootFolder.CalibreSettings != null;

            try
            {
                if (!isCalibre)
                {
                    _recycleBinProvider.DeleteFile(issueFile.Path, subfolder);
                }
                else
                {
                    _calibre.DeleteIssue(issueFile, rootFolder.CalibreSettings);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Unable to delete issue file");
                throw new NzbDroneClientException(HttpStatusCode.InternalServerError, "Unable to delete issue file");
            }
        }

        [EventHandleOrder(EventHandleOrder.First)]
        public void Handle(VolumeDeletedEvent message)
        {
            if (message.DeleteFiles)
            {
                var volume = message.Volume;

                var rootFolder = _rootFolderService.GetBestRootFolder(message.Volume.Path);
                var isCalibre = rootFolder.IsCalibreLibrary && rootFolder.CalibreSettings != null;

                if (isCalibre)
                {
                    // use metadataId instead of volumeId so that query works even after volume deleted
                    var issues = _mediaFileService.GetFilesByVolumeMetadataId(volume.VolumeMetadataId);
                    _calibre.DeleteIssues(issues, rootFolder.CalibreSettings);
                }
            }
        }

        public void HandleAsync(VolumeDeletedEvent message)
        {
            if (message.DeleteFiles)
            {
                var volume = message.Volume;

                var rootFolder = _rootFolderService.GetBestRootFolder(message.Volume.Path);
                var isCalibre = rootFolder.IsCalibreLibrary && rootFolder.CalibreSettings != null;

                if (!isCalibre)
                {
                    var allVolumes = _volumeService.AllVolumePaths();

                    foreach (var s in allVolumes)
                    {
                        if (s.Key == volume.Id)
                        {
                            continue;
                        }

                        if (volume.Path.IsParentPath(s.Value))
                        {
                            _logger.Error("Volume path: '{0}' is a parent of another volume, not deleting files.", volume.Path);
                            return;
                        }

                        if (volume.Path.PathEquals(s.Value))
                        {
                            _logger.Error("Volume path: '{0}' is the same as another volume, not deleting files.", volume.Path);
                            return;
                        }
                    }

                    if (_diskProvider.FolderExists(message.Volume.Path))
                    {
                        _recycleBinProvider.DeleteFolder(message.Volume.Path);
                    }

                    _eventAggregator.PublishEvent(new DeleteCompletedEvent());
                }
            }
        }

        public void HandleAsync(IssueDeletedEvent message)
        {
            if (message.DeleteFiles)
            {
                var files = _mediaFileService.GetFilesByIssue(message.Issue.Id);
                foreach (var file in files)
                {
                    DeleteFile(file);
                }
            }
        }

        [EventHandleOrder(EventHandleOrder.Last)]
        public void Handle(IssueFileDeletedEvent message)
        {
            if (message.Reason == DeleteMediaFileReason.Upgrade)
            {
                return;
            }

            if (_configService.DeleteEmptyFolders)
            {
                var volume = message.IssueFile.Volume.Value;
                var issueFolder = message.IssueFile.Path.GetParentPath();

                if (_diskProvider.GetFiles(volume.Path, true).Empty())
                {
                    _diskProvider.DeleteFolder(volume.Path, true);
                }
                else if (_diskProvider.GetFiles(issueFolder, true).Empty())
                {
                    _diskProvider.RemoveEmptySubfolders(issueFolder);
                }
            }
        }
    }
}
