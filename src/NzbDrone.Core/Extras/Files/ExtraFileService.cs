using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Extras.Files
{
    public interface IExtraFileService<TExtraFile>
        where TExtraFile : ExtraFile, new()
    {
        List<TExtraFile> GetFilesByVolume(int volumeId);
        List<TExtraFile> GetFilesByIssueFile(int issueFileId);
        TExtraFile FindByPath(int volumeId, string path);
        void Upsert(TExtraFile extraFile);
        void Upsert(List<TExtraFile> extraFiles);
        void Delete(int id);
        void DeleteMany(IEnumerable<int> ids);
    }

    public abstract class ExtraFileService<TExtraFile> : IExtraFileService<TExtraFile>,
                                                         IHandleAsync<VolumeDeletedEvent>,
                                                         IHandle<IssueFileDeletedEvent>
        where TExtraFile : ExtraFile, new()
    {
        private readonly IExtraFileRepository<TExtraFile> _repository;
        private readonly IVolumeService _volumeService;
        private readonly IDiskProvider _diskProvider;
        private readonly IRecycleBinProvider _recycleBinProvider;
        private readonly Logger _logger;

        public ExtraFileService(IExtraFileRepository<TExtraFile> repository,
                                IVolumeService volumeService,
                                IDiskProvider diskProvider,
                                IRecycleBinProvider recycleBinProvider,
                                Logger logger)
        {
            _repository = repository;
            _volumeService = volumeService;
            _diskProvider = diskProvider;
            _recycleBinProvider = recycleBinProvider;
            _logger = logger;
        }

        public List<TExtraFile> GetFilesByVolume(int volumeId)
        {
            return _repository.GetFilesByVolume(volumeId);
        }

        public List<TExtraFile> GetFilesByIssueFile(int issueFileId)
        {
            return _repository.GetFilesByIssueFile(issueFileId);
        }

        public TExtraFile FindByPath(int volumeId, string path)
        {
            return _repository.FindByPath(volumeId, path);
        }

        public void Upsert(TExtraFile extraFile)
        {
            Upsert(new List<TExtraFile> { extraFile });
        }

        public void Upsert(List<TExtraFile> extraFiles)
        {
            extraFiles.ForEach(m =>
            {
                m.LastUpdated = DateTime.UtcNow;

                if (m.Id == 0)
                {
                    m.Added = m.LastUpdated;
                }
            });

            _repository.InsertMany(extraFiles.Where(m => m.Id == 0).ToList());
            _repository.UpdateMany(extraFiles.Where(m => m.Id > 0).ToList());
        }

        public void Delete(int id)
        {
            _repository.Delete(id);
        }

        public void DeleteMany(IEnumerable<int> ids)
        {
            _repository.DeleteMany(ids);
        }

        public void HandleAsync(VolumeDeletedEvent message)
        {
            _logger.Debug("Deleting Extra from database for volume: {0}", message.Volume);
            _repository.DeleteForVolume(message.Volume.Id);
        }

        public void Handle(IssueFileDeletedEvent message)
        {
            var issueFile = message.IssueFile;

            if (message.Reason == DeleteMediaFileReason.NoLinkedEpisodes)
            {
                _logger.Debug("Removing track file from DB as part of cleanup routine, not deleting extra files from disk.");
            }
            else
            {
                var volume = issueFile.Volume.Value;

                foreach (var extra in _repository.GetFilesByIssueFile(issueFile.Id))
                {
                    var path = Path.Combine(volume.Path, extra.RelativePath);

                    if (_diskProvider.FileExists(path))
                    {
                        // Send to the recycling bin so they can be recovered if necessary
                        var subfolder = _diskProvider.GetParentFolder(volume.Path).GetRelativePath(_diskProvider.GetParentFolder(path));
                        _recycleBinProvider.DeleteFile(path, subfolder);
                    }
                }
            }

            _logger.Debug("Deleting Extra from database for track file: {0}", issueFile);
            _repository.DeleteForIssueFile(issueFile.Id);
        }
    }
}
