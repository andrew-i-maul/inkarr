using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Extras
{
    public interface IExtraService
    {
        void ImportTrack(LocalIssue localIssue, IssueFile issueFile, bool isReadOnly);
    }

    public class ExtraService : IExtraService,
                                IHandle<MediaCoversUpdatedEvent>,
                                IHandle<TrackFolderCreatedEvent>,
                                IHandle<VolumeRenamedEvent>
    {
        private readonly IMediaFileService _mediaFileService;
        private readonly IEditionService _editionService;
        private readonly IDiskProvider _diskProvider;
        private readonly IConfigService _configService;
        private readonly List<IManageExtraFiles> _extraFileManagers;
        private readonly Logger _logger;

        public ExtraService(IMediaFileService mediaFileService,
                            IEditionService editionService,
                            IDiskProvider diskProvider,
                            IConfigService configService,
                            IEnumerable<IManageExtraFiles> extraFileManagers,
                            Logger logger)
        {
            _mediaFileService = mediaFileService;
            _editionService = editionService;
            _diskProvider = diskProvider;
            _configService = configService;
            _extraFileManagers = extraFileManagers.OrderBy(e => e.Order).ToList();
            _logger = logger;
        }

        public void ImportTrack(LocalIssue localIssue, IssueFile issueFile, bool isReadOnly)
        {
            ImportExtraFiles(localIssue, issueFile, isReadOnly);

            CreateAfterImport(localIssue.Volume, issueFile);
        }

        public void ImportExtraFiles(LocalIssue localIssue, IssueFile issueFile, bool isReadOnly)
        {
            if (!_configService.ImportExtraFiles)
            {
                return;
            }

            var sourcePath = localIssue.Path;
            var sourceFolder = _diskProvider.GetParentFolder(sourcePath);
            var sourceFileName = Path.GetFileNameWithoutExtension(sourcePath);
            var files = _diskProvider.GetFiles(sourceFolder, false);

            var wantedExtensions = _configService.ExtraFileExtensions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                                     .Select(e => e.Trim(' ', '.'))
                                                                     .ToList();

            var matchingFilenames = files.Where(f => Path.GetFileNameWithoutExtension(f).StartsWith(sourceFileName, StringComparison.InvariantCultureIgnoreCase)).ToList();
            var filteredFilenames = new List<string>();
            var hasNfo = false;

            foreach (var matchingFilename in matchingFilenames)
            {
                // Filter out duplicate NFO files
                if (matchingFilename.EndsWith(".nfo", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (hasNfo)
                    {
                        continue;
                    }

                    hasNfo = true;
                }

                filteredFilenames.Add(matchingFilename);
            }

            foreach (var matchingFilename in filteredFilenames)
            {
                var matchingExtension = wantedExtensions.FirstOrDefault(e => matchingFilename.EndsWith(e));

                if (matchingExtension == null)
                {
                    continue;
                }

                try
                {
                    foreach (var extraFileManager in _extraFileManagers)
                    {
                        var extension = Path.GetExtension(matchingFilename);
                        var extraFile = extraFileManager.Import(localIssue.Volume, issueFile, matchingFilename, extension, isReadOnly);

                        if (extraFile != null)
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to import extra file: {0}", matchingFilename);
                }
            }
        }

        private void CreateAfterImport(Volume volume, IssueFile issueFile)
        {
            foreach (var extraFileManager in _extraFileManagers)
            {
                extraFileManager.CreateAfterIssueImport(volume, issueFile);
            }
        }

        public void Handle(MediaCoversUpdatedEvent message)
        {
            var volume = message.Volume;

            var issueFiles = GetIssueFiles(volume.Id);

            foreach (var extraFileManager in _extraFileManagers)
            {
                extraFileManager.CreateAfterVolumeScan(volume, issueFiles);
            }
        }

        public void Handle(TrackFolderCreatedEvent message)
        {
            var volume = message.Volume;
            var edition = _editionService.GetEdition(message.IssueFile.EditionId);

            foreach (var extraFileManager in _extraFileManagers)
            {
                extraFileManager.CreateAfterIssueImport(volume, edition.Issue.Value, message.VolumeFolder, message.IssueFolder);
            }
        }

        public void Handle(VolumeRenamedEvent message)
        {
            var volume = message.Volume;
            var issueFiles = GetIssueFiles(volume.Id);

            foreach (var extraFileManager in _extraFileManagers)
            {
                extraFileManager.MoveFilesAfterRename(volume, issueFiles);
            }
        }

        private List<IssueFile> GetIssueFiles(int volumeId)
        {
            return _mediaFileService.GetFilesByVolume(volumeId);
        }
    }
}
