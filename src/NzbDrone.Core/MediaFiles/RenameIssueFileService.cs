using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Organizer;

namespace NzbDrone.Core.MediaFiles
{
    public interface IRenameIssueFileService
    {
        List<RenameIssueFilePreview> GetRenamePreviews(int volumeId);
        List<RenameIssueFilePreview> GetRenamePreviews(int volumeId, int issueId);
    }

    public class RenameIssueFileService : IRenameIssueFileService, IExecute<RenameFilesCommand>, IExecute<RenameVolumeCommand>
    {
        private readonly IVolumeService _volumeService;
        private readonly IMediaFileService _mediaFileService;
        private readonly IMoveIssueFiles _issueFileMover;
        private readonly IEventAggregator _eventAggregator;
        private readonly IBuildFileNames _filenameBuilder;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public RenameIssueFileService(IVolumeService volumeService,
                                        IMediaFileService mediaFileService,
                                        IMoveIssueFiles issueFileMover,
                                        IEventAggregator eventAggregator,
                                        IBuildFileNames filenameBuilder,
                                        IDiskProvider diskProvider,
                                        Logger logger)
        {
            _volumeService = volumeService;
            _mediaFileService = mediaFileService;
            _issueFileMover = issueFileMover;
            _eventAggregator = eventAggregator;
            _filenameBuilder = filenameBuilder;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public List<RenameIssueFilePreview> GetRenamePreviews(int volumeId)
        {
            var volume = _volumeService.GetVolume(volumeId);
            var files = _mediaFileService.GetFilesByVolume(volumeId);

            _logger.Trace($"got {files.Count} files");

            return GetPreviews(volume, files)
                .OrderByDescending(e => e.IssueId)
                .ThenBy(e => e.ExistingPath)
                .ToList();
        }

        public List<RenameIssueFilePreview> GetRenamePreviews(int volumeId, int issueId)
        {
            var volume = _volumeService.GetVolume(volumeId);
            var files = _mediaFileService.GetFilesByIssue(issueId);

            return GetPreviews(volume, files)
                .OrderBy(e => e.ExistingPath).ToList();
        }

        private IEnumerable<RenameIssueFilePreview> GetPreviews(Volume volume, List<IssueFile> files)
        {
            var counts = files.GroupBy(x => x.EditionId).ToDictionary(g => g.Key, g => g.Count());

            // Don't rename Calibre files
            foreach (var f in files.Where(x => x.CalibreId == 0))
            {
                var file = f;
                file.PartCount = counts[file.EditionId];

                var issue = file.Edition.Value;
                var issueFilePath = file.Path;

                if (issue == null)
                {
                    _logger.Warn("File ({0}) is not linked to a issue", issueFilePath);
                    continue;
                }

                var newName = _filenameBuilder.BuildIssueFileName(volume, issue, file);

                _logger.Trace($"got name {newName}");

                var newPath = _filenameBuilder.BuildIssueFilePath(volume, issue, newName, Path.GetExtension(issueFilePath));

                _logger.Trace($"got path {newPath}");

                if (!issueFilePath.PathEquals(newPath, StringComparison.Ordinal))
                {
                    yield return new RenameIssueFilePreview
                    {
                        VolumeId = volume.Id,
                        IssueId = issue.Id,
                        IssueFileId = file.Id,
                        ExistingPath = file.Path,
                        NewPath = newPath
                    };
                }
            }
        }

        private void RenameFiles(List<IssueFile> issueFiles, Volume volume)
        {
            var allFiles = _mediaFileService.GetFilesByVolume(volume.Id);
            var counts = allFiles.GroupBy(x => x.EditionId).ToDictionary(g => g.Key, g => g.Count());
            var renamed = new List<RenamedIssueFile>();

            // Don't rename Calibre files
            foreach (var issueFile in issueFiles.Where(x => x.CalibreId == 0))
            {
                var previousPath = issueFile.Path;
                issueFile.PartCount = counts[issueFile.EditionId];

                try
                {
                    _logger.Debug("Renaming issue file: {0}", issueFile);
                    _issueFileMover.MoveIssueFile(issueFile, volume);

                    _mediaFileService.Update(issueFile);

                    renamed.Add(new RenamedIssueFile
                    {
                        IssueFile = issueFile,
                        PreviousPath = previousPath
                    });

                    _logger.Debug("Renamed issue file: {0}", issueFile);

                    _eventAggregator.PublishEvent(new IssueFileRenamedEvent(volume, issueFile, previousPath));
                }
                catch (FileAlreadyExistsException ex)
                {
                    _logger.Warn("File not renamed, there is already a file at the destination: {0}", ex.Filename);
                }
                catch (SameFilenameException ex)
                {
                    _logger.Debug("File not renamed, source and destination are the same: {0}", ex.Filename);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to rename file {0}", previousPath);
                }
            }

            if (renamed.Any())
            {
                _eventAggregator.PublishEvent(new VolumeRenamedEvent(volume, renamed));

                _logger.Debug("Removing Empty Subfolders from: {0}", volume.Path);
                _diskProvider.RemoveEmptySubfolders(volume.Path);
            }
        }

        public void Execute(RenameFilesCommand message)
        {
            var volume = _volumeService.GetVolume(message.VolumeId);
            var issueFiles = _mediaFileService.Get(message.Files);

            _logger.ProgressInfo("Renaming {0} files for {1}", issueFiles.Count, volume.Name);
            RenameFiles(issueFiles, volume);
            _logger.ProgressInfo("Selected issue files renamed for {0}", volume.Name);
        }

        public void Execute(RenameVolumeCommand message)
        {
            _logger.Debug("Renaming all files for selected volume");
            var volumeToRename = _volumeService.GetVolumes(message.VolumeIds);

            foreach (var volume in volumeToRename)
            {
                var issueFiles = _mediaFileService.GetFilesByVolume(volume.Id);
                _logger.ProgressInfo("Renaming all files in volume: {0}", volume.Name);
                RenameFiles(issueFiles, volume);
                _logger.ProgressInfo("All issue files renamed for {0}", volume.Name);
            }
        }
    }
}
