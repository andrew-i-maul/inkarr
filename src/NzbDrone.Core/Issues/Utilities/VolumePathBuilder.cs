using System;
using System.IO;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.Issues
{
    public interface IBuildVolumePaths
    {
        string BuildPath(Volume volume, bool useExistingRelativeFolder);
    }

    public class VolumePathBuilder : IBuildVolumePaths
    {
        private readonly IBuildFileNames _fileNameBuilder;
        private readonly IRootFolderService _rootFolderService;

        public VolumePathBuilder(IBuildFileNames fileNameBuilder, IRootFolderService rootFolderService)
        {
            _fileNameBuilder = fileNameBuilder;
            _rootFolderService = rootFolderService;
        }

        public string BuildPath(Volume volume, bool useExistingRelativeFolder)
        {
            if (volume.RootFolderPath.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Root folder was not provided", nameof(volume));
            }

            if (useExistingRelativeFolder && volume.Path.IsNotNullOrWhiteSpace())
            {
                var relativePath = GetExistingRelativePath(volume);
                return Path.Combine(volume.RootFolderPath, relativePath);
            }

            return Path.Combine(volume.RootFolderPath, _fileNameBuilder.GetVolumeFolder(volume));
        }

        private string GetExistingRelativePath(Volume volume)
        {
            var rootFolderPath = _rootFolderService.GetBestRootFolderPath(volume.Path);

            return rootFolderPath.GetRelativePath(volume.Path);
        }
    }
}
