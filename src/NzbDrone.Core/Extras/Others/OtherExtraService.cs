using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Extras.Others
{
    public class OtherExtraService : ExtraFileManager<OtherExtraFile>
    {
        private readonly IOtherExtraFileService _otherExtraFileService;
        private readonly IMediaFileAttributeService _mediaFileAttributeService;

        public OtherExtraService(IConfigService configService,
                                 IDiskProvider diskProvider,
                                 IDiskTransferService diskTransferService,
                                 IOtherExtraFileService otherExtraFileService,
                                 IMediaFileAttributeService mediaFileAttributeService,
                                 Logger logger)
            : base(configService, diskProvider, diskTransferService, logger)
        {
            _otherExtraFileService = otherExtraFileService;
            _mediaFileAttributeService = mediaFileAttributeService;
        }

        public override int Order => 2;

        public override IEnumerable<ExtraFile> CreateAfterVolumeScan(Volume volume, List<IssueFile> issueFiles)
        {
            return Enumerable.Empty<ExtraFile>();
        }

        public override IEnumerable<ExtraFile> CreateAfterIssueImport(Volume volume, IssueFile issueFile)
        {
            return Enumerable.Empty<ExtraFile>();
        }

        public override IEnumerable<ExtraFile> CreateAfterIssueImport(Volume volume, Issue issue, string volumeFolder, string issueFolder)
        {
            return Enumerable.Empty<ExtraFile>();
        }

        public override IEnumerable<ExtraFile> MoveFilesAfterRename(Volume volume, List<IssueFile> issueFiles)
        {
            var extraFiles = _otherExtraFileService.GetFilesByVolume(volume.Id);
            var movedFiles = new List<OtherExtraFile>();

            foreach (var issueFile in issueFiles)
            {
                var extraFilesForTrackFile = extraFiles.Where(m => m.IssueFileId == issueFile.Id).ToList();

                foreach (var extraFile in extraFilesForTrackFile)
                {
                    movedFiles.AddIfNotNull(MoveFile(volume, issueFile, extraFile));
                }
            }

            _otherExtraFileService.Upsert(movedFiles);

            return movedFiles;
        }

        public override ExtraFile Import(Volume volume, IssueFile issueFile, string path, string extension, bool readOnly)
        {
            var extraFile = ImportFile(volume, issueFile, path, readOnly, extension, null);

            _mediaFileAttributeService.SetFilePermissions(path);
            _otherExtraFileService.Upsert(extraFile);

            return extraFile;
        }
    }
}
