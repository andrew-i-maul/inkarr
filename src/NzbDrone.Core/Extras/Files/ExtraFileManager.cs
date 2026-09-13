using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Extras.Files
{
    public interface IManageExtraFiles
    {
        int Order { get; }
        IEnumerable<ExtraFile> CreateAfterVolumeScan(Volume volume, List<IssueFile> issueFiles);
        IEnumerable<ExtraFile> CreateAfterIssueImport(Volume volume, IssueFile issueFile);
        IEnumerable<ExtraFile> CreateAfterIssueImport(Volume volume, Issue issue, string volumeFolder, string issueFolder);
        IEnumerable<ExtraFile> MoveFilesAfterRename(Volume volume, List<IssueFile> issueFiles);
        ExtraFile Import(Volume volume, IssueFile issueFile, string path, string extension, bool readOnly);
    }

    public abstract class ExtraFileManager<TExtraFile> : IManageExtraFiles
        where TExtraFile : ExtraFile, new()
    {
        private readonly IConfigService _configService;
        private readonly IDiskProvider _diskProvider;
        private readonly IDiskTransferService _diskTransferService;
        private readonly Logger _logger;

        public ExtraFileManager(IConfigService configService,
                                IDiskProvider diskProvider,
                                IDiskTransferService diskTransferService,
                                Logger logger)
        {
            _configService = configService;
            _diskProvider = diskProvider;
            _diskTransferService = diskTransferService;
            _logger = logger;
        }

        public abstract int Order { get; }
        public abstract IEnumerable<ExtraFile> CreateAfterVolumeScan(Volume volume, List<IssueFile> issueFiles);
        public abstract IEnumerable<ExtraFile> CreateAfterIssueImport(Volume volume, IssueFile issueFile);
        public abstract IEnumerable<ExtraFile> CreateAfterIssueImport(Volume volume, Issue issue, string volumeFolder, string issueFolder);
        public abstract IEnumerable<ExtraFile> MoveFilesAfterRename(Volume volume, List<IssueFile> issueFiles);
        public abstract ExtraFile Import(Volume volume, IssueFile issueFile, string path, string extension, bool readOnly);

        protected TExtraFile ImportFile(Volume volume, IssueFile issueFile, string path, bool readOnly, string extension, string fileNameSuffix = null)
        {
            var newFolder = Path.GetDirectoryName(issueFile.Path);
            var filenameBuilder = new StringBuilder(Path.GetFileNameWithoutExtension(issueFile.Path));

            if (fileNameSuffix.IsNotNullOrWhiteSpace())
            {
                filenameBuilder.Append(fileNameSuffix);
            }

            filenameBuilder.Append(extension);

            var newFileName = Path.Combine(newFolder, filenameBuilder.ToString());
            var transferMode = TransferMode.Move;

            if (readOnly)
            {
                transferMode = _configService.CopyUsingHardlinks ? TransferMode.HardLinkOrCopy : TransferMode.Copy;
            }

            _diskTransferService.TransferFile(path, newFileName, transferMode, true);

            return new TExtraFile
            {
                VolumeId = volume.Id,
                IssueId = issueFile.Edition.Value.IssueId,
                IssueFileId = issueFile.Id,
                RelativePath = volume.Path.GetRelativePath(newFileName),
                Extension = extension
            };
        }

        protected TExtraFile MoveFile(Volume volume, IssueFile issueFile, TExtraFile extraFile, string fileNameSuffix = null)
        {
            _logger.Trace("Renaming extra file: {0}", extraFile);

            var newFolder = Path.GetDirectoryName(issueFile.Path);
            var filenameBuilder = new StringBuilder(Path.GetFileNameWithoutExtension(issueFile.Path));

            if (fileNameSuffix.IsNotNullOrWhiteSpace())
            {
                filenameBuilder.Append(fileNameSuffix);
            }

            filenameBuilder.Append(extraFile.Extension);

            var existingFileName = Path.Combine(volume.Path, extraFile.RelativePath);
            var newFileName = Path.Combine(newFolder, filenameBuilder.ToString());

            if (newFileName.PathNotEquals(existingFileName))
            {
                try
                {
                    _logger.Trace("Renaming extra file: {0} to {1}", extraFile, newFileName);

                    _diskProvider.MoveFile(existingFileName, newFileName);
                    extraFile.RelativePath = volume.Path.GetRelativePath(newFileName);

                    _logger.Trace("Renamed extra file from: {0}", extraFile);

                    return extraFile;
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to move file after rename: {0}", existingFileName);
                }
            }

            return null;
        }
    }
}
