using System.IO;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Issues.Commands;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Organizer;

namespace NzbDrone.Core.Issues
{
    public class MoveVolumeService : IExecute<MoveVolumeCommand>, IExecute<BulkMoveVolumeCommand>
    {
        private readonly IVolumeService _volumeService;
        private readonly IBuildFileNames _filenameBuilder;
        private readonly IDiskProvider _diskProvider;
        private readonly IRootFolderWatchingService _rootFolderWatchingService;
        private readonly IDiskTransferService _diskTransferService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public MoveVolumeService(IVolumeService volumeService,
                                 IBuildFileNames filenameBuilder,
                                 IDiskProvider diskProvider,
                                 IRootFolderWatchingService rootFolderWatchingService,
                                 IDiskTransferService diskTransferService,
                                 IEventAggregator eventAggregator,
                                 Logger logger)
        {
            _volumeService = volumeService;
            _filenameBuilder = filenameBuilder;
            _diskProvider = diskProvider;
            _rootFolderWatchingService = rootFolderWatchingService;
            _diskTransferService = diskTransferService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        private void MoveSingleVolume(Volume volume, string sourcePath, string destinationPath, int? index = null, int? total = null)
        {
            if (!_diskProvider.FolderExists(sourcePath))
            {
                _logger.Debug("Folder '{0}' for '{1}' does not exist, not moving.", sourcePath, volume.Name);
                return;
            }

            if (index != null && total != null)
            {
                _logger.ProgressInfo("Moving {0} from '{1}' to '{2}' ({3}/{4})", volume.Name, sourcePath, destinationPath, index + 1, total);
            }
            else
            {
                _logger.ProgressInfo("Moving {0} from '{1}' to '{2}'", volume.Name, sourcePath, destinationPath);
            }

            if (sourcePath.PathEquals(destinationPath))
            {
                _logger.ProgressInfo("{0} is already in the specified location '{1}'.", volume, destinationPath);
                return;
            }

            try
            {
                _rootFolderWatchingService.ReportFileSystemChangeBeginning(sourcePath, destinationPath);

                _diskTransferService.TransferFolder(sourcePath, destinationPath, TransferMode.Move);

                _logger.ProgressInfo("{0} moved successfully to {1}", volume.Name, destinationPath);

                _eventAggregator.PublishEvent(new VolumeMovedEvent(volume, sourcePath, destinationPath));
            }
            catch (IOException ex)
            {
                _logger.Error(ex, "Unable to move volume from '{0}' to '{1}'. Try moving files manually", sourcePath, destinationPath);

                RevertPath(volume.Id, sourcePath);
            }
        }

        private void RevertPath(int volumeId, string path)
        {
            var volume = _volumeService.GetVolume(volumeId);

            volume.Path = path;
            _volumeService.UpdateVolume(volume);
        }

        public void Execute(MoveVolumeCommand message)
        {
            var volume = _volumeService.GetVolume(message.VolumeId);
            MoveSingleVolume(volume, message.SourcePath, message.DestinationPath);
        }

        public void Execute(BulkMoveVolumeCommand message)
        {
            var volumeToMove = message.Volume;
            var destinationRootFolder = message.DestinationRootFolder;

            _logger.ProgressInfo("Moving {0} volume to '{1}'", volumeToMove.Count, destinationRootFolder);

            for (var index = 0; index < volumeToMove.Count; index++)
            {
                var s = volumeToMove[index];
                var volume = _volumeService.GetVolume(s.VolumeId);
                var destinationPath = Path.Combine(destinationRootFolder, _filenameBuilder.GetVolumeFolder(volume));

                MoveSingleVolume(volume, s.SourcePath, destinationPath, index, volumeToMove.Count);
            }

            _logger.ProgressInfo("Finished moving {0} volume to '{1}'", volumeToMove.Count, destinationRootFolder);
        }
    }
}
