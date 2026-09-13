using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using NLog;
using NzbDrone.Common;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMediaFileService
    {
        IssueFile Add(IssueFile issueFile);
        void AddMany(List<IssueFile> issueFiles);
        void Update(IssueFile issueFile);
        void Update(List<IssueFile> issueFiles);
        void Delete(IssueFile issueFile, DeleteMediaFileReason reason);
        void DeleteMany(List<IssueFile> issueFiles, DeleteMediaFileReason reason);
        List<IssueFile> GetFilesByVolume(int volumeId);
        List<IssueFile> GetFilesByVolumeMetadataId(int volumeMetadataId);
        List<IssueFile> GetFilesByIssue(int issueId);
        List<IssueFile> GetFilesByEdition(int editionId);
        List<IssueFile> GetUnmappedFiles();
        List<IFileInfo> FilterUnchangedFiles(List<IFileInfo> files, FilterFilesType filter);
        IssueFile Get(int id);
        List<IssueFile> Get(IEnumerable<int> ids);
        List<IssueFile> GetFilesWithBasePath(string path);
        List<IssueFile> GetFileWithPath(List<string> path);
        IssueFile GetFileWithPath(string path);
        void UpdateMediaInfo(List<IssueFile> issueFiles);
    }

    public class MediaFileService : IMediaFileService,
        IHandle<VolumeMovedEvent>,
        IHandleAsync<IssueDeletedEvent>,
        IHandleAsync<ModelEvent<RootFolder>>
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IMediaFileRepository _mediaFileRepository;
        private readonly Logger _logger;

        public MediaFileService(IMediaFileRepository mediaFileRepository, IEventAggregator eventAggregator, Logger logger)
        {
            _mediaFileRepository = mediaFileRepository;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public IssueFile Add(IssueFile issueFile)
        {
            var addedFile = _mediaFileRepository.Insert(issueFile);
            _eventAggregator.PublishEvent(new IssueFileAddedEvent(addedFile));
            return addedFile;
        }

        public void AddMany(List<IssueFile> issueFiles)
        {
            _mediaFileRepository.InsertMany(issueFiles);
            foreach (var addedFile in issueFiles)
            {
                _eventAggregator.PublishEvent(new IssueFileAddedEvent(addedFile));
            }
        }

        public void Update(IssueFile issueFile)
        {
            _mediaFileRepository.Update(issueFile);
        }

        public void Update(List<IssueFile> issueFiles)
        {
            _mediaFileRepository.UpdateMany(issueFiles);
        }

        public void Delete(IssueFile issueFile, DeleteMediaFileReason reason)
        {
            _mediaFileRepository.Delete(issueFile);

            // If the trackfile wasn't mapped to a track, don't publish an event
            if (issueFile.EditionId > 0)
            {
                _eventAggregator.PublishEvent(new IssueFileDeletedEvent(issueFile, reason));
            }
        }

        public void DeleteMany(List<IssueFile> issueFiles, DeleteMediaFileReason reason)
        {
            _mediaFileRepository.DeleteMany(issueFiles);

            // publish events where trackfile was mapped to a track
            foreach (var issueFile in issueFiles.Where(x => x.EditionId > 0))
            {
                _eventAggregator.PublishEvent(new IssueFileDeletedEvent(issueFile, reason));
            }
        }

        public List<IFileInfo> FilterUnchangedFiles(List<IFileInfo> files, FilterFilesType filter)
        {
            if (filter == FilterFilesType.None)
            {
                return files;
            }

            _logger.Debug($"Filtering {files.Count} files for unchanged files");

            var knownFiles = GetFileWithPath(files.Select(x => x.FullName).ToList());
            _logger.Trace($"Got {knownFiles.Count} existing files");

            if (!knownFiles.Any())
            {
                return files;
            }

            var combined = files
                .Join(knownFiles,
                      f => f.FullName,
                      af => af.Path,
                      (f, af) => new { DiskFile = f, DbFile = af },
                      PathEqualityComparer.Instance)
                .ToList();
            _logger.Trace($"Matched paths for {combined.Count} files");

            List<IFileInfo> unwanted = null;
            if (filter == FilterFilesType.Known)
            {
                unwanted = combined
                    .Where(x => x.DiskFile.Length == x.DbFile.Size &&
                           Math.Abs((x.DiskFile.LastWriteTimeUtc - x.DbFile.Modified.ToUniversalTime()).TotalSeconds) <= 1)
                    .Select(x => x.DiskFile)
                    .ToList();
                _logger.Trace($"{unwanted.Count} unchanged existing files");
            }
            else if (filter == FilterFilesType.Matched)
            {
                unwanted = combined
                    .Where(x => x.DiskFile.Length == x.DbFile.Size &&
                           Math.Abs((x.DiskFile.LastWriteTimeUtc - x.DbFile.Modified.ToUniversalTime()).TotalSeconds) <= 1 &&
                           (x.DbFile.Edition == null || (x.DbFile.Edition.IsLoaded && x.DbFile.Edition.Value != null)))
                    .Select(x => x.DiskFile)
                    .ToList();
                _logger.Trace($"{unwanted.Count} unchanged and matched files");
            }
            else
            {
                throw new ArgumentException("Unrecognised value of FilterFilesType filter");
            }

            return files.Except(unwanted).ToList();
        }

        public IssueFile Get(int id)
        {
            return _mediaFileRepository.Get(id);
        }

        public List<IssueFile> Get(IEnumerable<int> ids)
        {
            return _mediaFileRepository.Get(ids).ToList();
        }

        public List<IssueFile> GetFilesWithBasePath(string path)
        {
            return _mediaFileRepository.GetFilesWithBasePath(path);
        }

        public List<IssueFile> GetFileWithPath(List<string> path)
        {
            return _mediaFileRepository.GetFileWithPath(path);
        }

        public IssueFile GetFileWithPath(string path)
        {
            return _mediaFileRepository.GetFileWithPath(path);
        }

        public List<IssueFile> GetFilesByVolume(int volumeId)
        {
            return _mediaFileRepository.GetFilesByVolume(volumeId);
        }

        public List<IssueFile> GetFilesByVolumeMetadataId(int volumeMetadataId)
        {
            return _mediaFileRepository.GetFilesByVolumeMetadataId(volumeMetadataId);
        }

        public List<IssueFile> GetFilesByIssue(int issueId)
        {
            return _mediaFileRepository.GetFilesByIssue(issueId);
        }

        public List<IssueFile> GetFilesByEdition(int editionId)
        {
            return _mediaFileRepository.GetFilesByEdition(editionId);
        }

        public List<IssueFile> GetUnmappedFiles()
        {
            return _mediaFileRepository.GetUnmappedFiles();
        }

        public void UpdateMediaInfo(List<IssueFile> issueFiles)
        {
            _mediaFileRepository.SetFields(issueFiles, t => t.MediaInfo);
        }

        public void Handle(VolumeMovedEvent message)
        {
            var files = _mediaFileRepository.GetFilesWithBasePath(message.SourcePath);

            foreach (var file in files)
            {
                var newPath = message.DestinationPath + file.Path.Substring(message.SourcePath.Length);
                file.Path = newPath;
            }

            Update(files);
        }

        public void HandleAsync(IssueDeletedEvent message)
        {
            if (message.DeleteFiles)
            {
                _mediaFileRepository.DeleteFilesByIssue(message.Issue.Id);
            }
            else
            {
                _mediaFileRepository.UnlinkFilesByIssue(message.Issue.Id);
            }
        }

        public void HandleAsync(ModelEvent<RootFolder> message)
        {
            if (message.Action == ModelAction.Deleted)
            {
                var files = GetFilesWithBasePath(message.Model.Path);
                DeleteMany(files, DeleteMediaFileReason.Manual);
            }
        }
    }
}
