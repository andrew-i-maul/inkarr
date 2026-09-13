using System.IO;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Extras.Others
{
    public interface IOtherExtraFileRenamer
    {
        void RenameOtherExtraFile(Volume volume, string path);
    }

    public class OtherExtraFileRenamer : IOtherExtraFileRenamer
    {
        private readonly Logger _logger;
        private readonly IDiskProvider _diskProvider;
        private readonly IRecycleBinProvider _recycleBinProvider;
        private readonly IVolumeService _volumeService;
        private readonly IOtherExtraFileService _otherExtraFileService;

        public OtherExtraFileRenamer(IOtherExtraFileService otherExtraFileService,
                                     IVolumeService volumeService,
                                     IRecycleBinProvider recycleBinProvider,
                                     IDiskProvider diskProvider,
                                     Logger logger)
        {
            _logger = logger;
            _diskProvider = diskProvider;
            _recycleBinProvider = recycleBinProvider;
            _volumeService = volumeService;
            _otherExtraFileService = otherExtraFileService;
        }

        public void RenameOtherExtraFile(Volume volume, string path)
        {
            if (!_diskProvider.FileExists(path))
            {
                return;
            }

            var relativePath = volume.Path.GetRelativePath(path);
            var otherExtraFile = _otherExtraFileService.FindByPath(volume.Id, relativePath);

            if (otherExtraFile != null)
            {
                var newPath = path + "-orig";

                // Recycle an existing -orig file.
                RemoveOtherExtraFile(volume, newPath);

                // Rename the file to .*-orig
                _diskProvider.MoveFile(path, newPath);
                otherExtraFile.RelativePath = relativePath + "-orig";
                otherExtraFile.Extension += "-orig";
                _otherExtraFileService.Upsert(otherExtraFile);
            }
        }

        private void RemoveOtherExtraFile(Volume volume, string path)
        {
            if (!_diskProvider.FileExists(path))
            {
                return;
            }

            var relativePath = volume.Path.GetRelativePath(path);
            var otherExtraFile = _otherExtraFileService.FindByPath(volume.Id, relativePath);

            if (otherExtraFile != null)
            {
                var subfolder = Path.GetDirectoryName(relativePath);
                _recycleBinProvider.DeleteFile(path, subfolder);
            }
        }
    }
}
