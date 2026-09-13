using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.Extras.Others;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Extras.Metadata
{
    public class MetadataService : ExtraFileManager<MetadataFile>
    {
        private readonly IMetadataFactory _metadataFactory;
        private readonly ICleanMetadataService _cleanMetadataService;
        private readonly IRecycleBinProvider _recycleBinProvider;
        private readonly IOtherExtraFileRenamer _otherExtraFileRenamer;
        private readonly IDiskTransferService _diskTransferService;
        private readonly IDiskProvider _diskProvider;
        private readonly IHttpClient _httpClient;
        private readonly IMediaFileAttributeService _mediaFileAttributeService;
        private readonly IMetadataFileService _metadataFileService;
        private readonly IIssueService _issueService;
        private readonly Logger _logger;

        public MetadataService(IConfigService configService,
                               IDiskProvider diskProvider,
                               IDiskTransferService diskTransferService,
                               IRecycleBinProvider recycleBinProvider,
                               IOtherExtraFileRenamer otherExtraFileRenamer,
                               IMetadataFactory metadataFactory,
                               ICleanMetadataService cleanMetadataService,
                               IHttpClient httpClient,
                               IMediaFileAttributeService mediaFileAttributeService,
                               IMetadataFileService metadataFileService,
                               IIssueService issueService,
                               Logger logger)
            : base(configService, diskProvider, diskTransferService, logger)
        {
            _metadataFactory = metadataFactory;
            _cleanMetadataService = cleanMetadataService;
            _otherExtraFileRenamer = otherExtraFileRenamer;
            _recycleBinProvider = recycleBinProvider;
            _diskTransferService = diskTransferService;
            _diskProvider = diskProvider;
            _httpClient = httpClient;
            _mediaFileAttributeService = mediaFileAttributeService;
            _metadataFileService = metadataFileService;
            _issueService = issueService;
            _logger = logger;
        }

        public override int Order => 0;

        public override IEnumerable<ExtraFile> CreateAfterVolumeScan(Volume volume, List<IssueFile> issueFiles)
        {
            var metadataFiles = _metadataFileService.GetFilesByVolume(volume.Id);
            _cleanMetadataService.Clean(volume);

            if (!_diskProvider.FolderExists(volume.Path))
            {
                _logger.Info("Volume folder does not exist, skipping metadata creation");
                return Enumerable.Empty<MetadataFile>();
            }

            var files = new List<MetadataFile>();

            foreach (var consumer in _metadataFactory.Enabled())
            {
                var consumerFiles = GetMetadataFilesForConsumer(consumer, metadataFiles);

                files.AddIfNotNull(ProcessVolumeMetadata(consumer, volume, consumerFiles));
                files.AddRange(ProcessVolumeImages(consumer, volume, consumerFiles));

                foreach (var issueFile in issueFiles)
                {
                    files.AddIfNotNull(ProcessIssueMetadata(consumer, volume, issueFile, consumerFiles));
                }
            }

            _metadataFileService.Upsert(files);

            return files;
        }

        public override IEnumerable<ExtraFile> CreateAfterIssueImport(Volume volume, IssueFile issueFile)
        {
            var files = new List<MetadataFile>();

            foreach (var consumer in _metadataFactory.Enabled())
            {
                files.AddIfNotNull(ProcessIssueMetadata(consumer, volume, issueFile, new List<MetadataFile>()));
            }

            _metadataFileService.Upsert(files);

            return files;
        }

        public override IEnumerable<ExtraFile> CreateAfterIssueImport(Volume volume, Issue issue, string volumeFolder, string issueFolder)
        {
            var metadataFiles = _metadataFileService.GetFilesByVolume(volume.Id);

            if (volumeFolder.IsNullOrWhiteSpace() && issueFolder.IsNullOrWhiteSpace())
            {
                return new List<MetadataFile>();
            }

            var files = new List<MetadataFile>();

            foreach (var consumer in _metadataFactory.Enabled())
            {
                var consumerFiles = GetMetadataFilesForConsumer(consumer, metadataFiles);

                if (volumeFolder.IsNotNullOrWhiteSpace())
                {
                    files.AddIfNotNull(ProcessVolumeMetadata(consumer, volume, consumerFiles));
                    files.AddRange(ProcessVolumeImages(consumer, volume, consumerFiles));
                }
            }

            _metadataFileService.Upsert(files);

            return files;
        }

        public override IEnumerable<ExtraFile> MoveFilesAfterRename(Volume volume, List<IssueFile> issueFiles)
        {
            var metadataFiles = _metadataFileService.GetFilesByVolume(volume.Id);
            var movedFiles = new List<MetadataFile>();
            var distinctTrackFilePaths = issueFiles.DistinctBy(s => Path.GetDirectoryName(s.Path)).ToList();

            // TODO: Move EpisodeImage and EpisodeMetadata metadata files, instead of relying on consumers to do it
            // (Xbmc's EpisodeImage is more than just the extension)
            foreach (var consumer in _metadataFactory.GetAvailableProviders())
            {
                foreach (var filePath in distinctTrackFilePaths)
                {
                    var metadataFilesForConsumer = GetMetadataFilesForConsumer(consumer, metadataFiles)
                        .Where(m => m.IssueId == filePath.Edition.Value.IssueId)
                        .Where(m => m.Type == MetadataType.IssueImage || m.Type == MetadataType.IssueMetadata)
                        .ToList();

                    foreach (var metadataFile in metadataFilesForConsumer)
                    {
                        var newFileName = consumer.GetFilenameAfterMove(volume, Path.GetDirectoryName(filePath.Path), metadataFile);
                        var existingFileName = Path.Combine(volume.Path, metadataFile.RelativePath);

                        if (newFileName.PathNotEquals(existingFileName))
                        {
                            try
                            {
                                _diskProvider.MoveFile(existingFileName, newFileName);
                                metadataFile.RelativePath = volume.Path.GetRelativePath(newFileName);
                                movedFiles.Add(metadataFile);
                            }
                            catch (Exception ex)
                            {
                                _logger.Warn(ex, "Unable to move metadata file after rename: {0}", existingFileName);
                            }
                        }
                    }
                }

                foreach (var issueFile in issueFiles)
                {
                    var metadataFilesForConsumer = GetMetadataFilesForConsumer(consumer, metadataFiles).Where(m => m.IssueFileId == issueFile.Id).ToList();

                    foreach (var metadataFile in metadataFilesForConsumer)
                    {
                        var newFileName = consumer.GetFilenameAfterMove(volume, issueFile, metadataFile);
                        var existingFileName = Path.Combine(volume.Path, metadataFile.RelativePath);

                        if (newFileName.PathNotEquals(existingFileName))
                        {
                            try
                            {
                                _diskProvider.MoveFile(existingFileName, newFileName);
                                metadataFile.RelativePath = volume.Path.GetRelativePath(newFileName);
                                movedFiles.Add(metadataFile);
                            }
                            catch (Exception ex)
                            {
                                _logger.Warn(ex, "Unable to move metadata file after rename: {0}", existingFileName);
                            }
                        }
                    }
                }
            }

            _metadataFileService.Upsert(movedFiles);

            return movedFiles;
        }

        public override ExtraFile Import(Volume volume, IssueFile issueFile, string path, string extension, bool readOnly)
        {
            return null;
        }

        private List<MetadataFile> GetMetadataFilesForConsumer(IMetadata consumer, List<MetadataFile> volumeMetadata)
        {
            return volumeMetadata.Where(c => c.Consumer == consumer.GetType().Name).ToList();
        }

        private MetadataFile ProcessVolumeMetadata(IMetadata consumer, Volume volume, List<MetadataFile> existingMetadataFiles)
        {
            var volumeMetadata = consumer.VolumeMetadata(volume);

            if (volumeMetadata == null)
            {
                return null;
            }

            var hash = volumeMetadata.Contents.SHA256Hash();

            var metadata = GetMetadataFile(volume, existingMetadataFiles, e => e.Type == MetadataType.VolumeMetadata) ??
                               new MetadataFile
                               {
                                   VolumeId = volume.Id,
                                   Consumer = consumer.GetType().Name,
                                   Type = MetadataType.VolumeMetadata
                               };

            if (hash == metadata.Hash)
            {
                if (volumeMetadata.RelativePath != metadata.RelativePath)
                {
                    metadata.RelativePath = volumeMetadata.RelativePath;

                    return metadata;
                }

                return null;
            }

            var fullPath = Path.Combine(volume.Path, volumeMetadata.RelativePath);

            _otherExtraFileRenamer.RenameOtherExtraFile(volume, fullPath);

            _logger.Debug("Writing Volume Metadata to: {0}", fullPath);
            SaveMetadataFile(fullPath, volumeMetadata.Contents);

            metadata.Hash = hash;
            metadata.RelativePath = volumeMetadata.RelativePath;
            metadata.Extension = Path.GetExtension(fullPath);

            return metadata;
        }

        private MetadataFile ProcessIssueMetadata(IMetadata consumer, Volume volume, IssueFile issueFile, List<MetadataFile> existingMetadataFiles)
        {
            var trackMetadata = consumer.IssueMetadata(volume, issueFile);

            if (trackMetadata == null)
            {
                return null;
            }

            var fullPath = Path.Combine(volume.Path, trackMetadata.RelativePath);

            _otherExtraFileRenamer.RenameOtherExtraFile(volume, fullPath);

            var existingMetadata = GetMetadataFile(volume, existingMetadataFiles, c => c.Type == MetadataType.IssueMetadata &&
                                                                                  c.IssueFileId == issueFile.Id);

            if (existingMetadata != null)
            {
                var existingFullPath = Path.Combine(volume.Path, existingMetadata.RelativePath);
                if (fullPath.PathNotEquals(existingFullPath))
                {
                    _diskTransferService.TransferFile(existingFullPath, fullPath, TransferMode.Move);
                    existingMetadata.RelativePath = trackMetadata.RelativePath;
                }
            }

            var hash = trackMetadata.Contents.SHA256Hash();

            var metadata = existingMetadata ??
                           new MetadataFile
                           {
                               VolumeId = volume.Id,
                               IssueId = issueFile.Edition.Value.IssueId,
                               IssueFileId = issueFile.Id,
                               Consumer = consumer.GetType().Name,
                               Type = MetadataType.IssueMetadata,
                               RelativePath = trackMetadata.RelativePath,
                               Extension = Path.GetExtension(fullPath)
                           };

            if (hash == metadata.Hash)
            {
                return null;
            }

            _logger.Debug("Writing Track Metadata to: {0}", fullPath);
            SaveMetadataFile(fullPath, trackMetadata.Contents);

            metadata.Hash = hash;

            return metadata;
        }

        private List<MetadataFile> ProcessVolumeImages(IMetadata consumer, Volume volume, List<MetadataFile> existingMetadataFiles)
        {
            var result = new List<MetadataFile>();

            foreach (var image in consumer.VolumeImages(volume))
            {
                var fullPath = Path.Combine(volume.Path, image.RelativePath);

                if (_diskProvider.FileExists(fullPath))
                {
                    _logger.Debug("Volume image already exists: {0}", fullPath);
                    continue;
                }

                _otherExtraFileRenamer.RenameOtherExtraFile(volume, fullPath);

                var metadata = GetMetadataFile(volume, existingMetadataFiles, c => c.Type == MetadataType.VolumeImage &&
                                                                              c.RelativePath == image.RelativePath) ??
                               new MetadataFile
                               {
                                   VolumeId = volume.Id,
                                   Consumer = consumer.GetType().Name,
                                   Type = MetadataType.VolumeImage,
                                   RelativePath = image.RelativePath,
                                   Extension = Path.GetExtension(fullPath)
                               };

                DownloadImage(volume, image);

                result.Add(metadata);
            }

            return result;
        }

        private void DownloadImage(Volume volume, ImageFileResult image)
        {
            var fullPath = Path.Combine(volume.Path, image.RelativePath);
            var downloaded = true;

            try
            {
                if (image.Url.StartsWith("http"))
                {
                    _httpClient.DownloadFile(image.Url, fullPath);
                }
                else if (_diskProvider.FileExists(image.Url))
                {
                    _diskProvider.CopyFile(image.Url, fullPath);
                }
                else
                {
                    downloaded = false;
                }

                if (downloaded)
                {
                    _mediaFileAttributeService.SetFilePermissions(fullPath);
                }
            }
            catch (HttpException ex)
            {
                _logger.Warn(ex, "Couldn't download image {0} for {1}. {2}", image.Url, volume, ex.Message);
            }
            catch (WebException ex)
            {
                _logger.Warn(ex, "Couldn't download image {0} for {1}. {2}", image.Url, volume, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Couldn't download image {0} for {1}", image.Url, volume);
            }
        }

        private void SaveMetadataFile(string path, string contents)
        {
            _diskProvider.WriteAllText(path, contents);
            _mediaFileAttributeService.SetFilePermissions(path);
        }

        private MetadataFile GetMetadataFile(Volume volume, List<MetadataFile> existingMetadataFiles, Func<MetadataFile, bool> predicate)
        {
            var matchingMetadataFiles = existingMetadataFiles.Where(predicate).ToList();

            if (matchingMetadataFiles.Empty())
            {
                return null;
            }

            //Remove duplicate metadata files from DB and disk
            foreach (var file in matchingMetadataFiles.Skip(1))
            {
                var path = Path.Combine(volume.Path, file.RelativePath);

                _logger.Debug("Removing duplicate Metadata file: {0}", path);

                var subfolder = _diskProvider.GetParentFolder(volume.Path).GetRelativePath(_diskProvider.GetParentFolder(path));
                _recycleBinProvider.DeleteFile(path, subfolder);
                _metadataFileService.Delete(file.Id);
            }

            return matchingMetadataFiles.First();
        }
    }
}
