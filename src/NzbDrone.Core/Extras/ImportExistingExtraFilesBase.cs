using System.Collections.Generic;
using System.IO;
using System.Linq;
using NzbDrone.Common;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.Extras
{
    public abstract class ImportExistingExtraFilesBase<TExtraFile> : IImportExistingExtraFiles
        where TExtraFile : ExtraFile, new()
    {
        private readonly IExtraFileService<TExtraFile> _extraFileService;

        public ImportExistingExtraFilesBase(IExtraFileService<TExtraFile> extraFileService)
        {
            _extraFileService = extraFileService;
        }

        public abstract int Order { get; }
        public abstract IEnumerable<ExtraFile> ProcessFiles(Volume volume, List<string> filesOnDisk, List<string> importedFiles);

        public virtual ImportExistingExtraFileFilterResult<TExtraFile> FilterAndClean(Volume volume, List<string> filesOnDisk, List<string> importedFiles)
        {
            var volumeFiles = _extraFileService.GetFilesByVolume(volume.Id);

            Clean(volume, filesOnDisk, importedFiles, volumeFiles);

            return Filter(volume, filesOnDisk, importedFiles, volumeFiles);
        }

        private ImportExistingExtraFileFilterResult<TExtraFile> Filter(Volume volume, List<string> filesOnDisk, List<string> importedFiles, List<TExtraFile> volumeFiles)
        {
            var previouslyImported = volumeFiles.IntersectBy(s => Path.Combine(volume.Path, s.RelativePath), filesOnDisk, f => f, PathEqualityComparer.Instance).ToList();
            var filteredFiles = filesOnDisk.Except(previouslyImported.Select(f => Path.Combine(volume.Path, f.RelativePath)).ToList(), PathEqualityComparer.Instance)
                                           .Except(importedFiles, PathEqualityComparer.Instance)
                                           .ToList();

            // Return files that are already imported so they aren't imported again by other importers.
            // Filter out files that were previously imported and as well as ones imported by other importers.
            return new ImportExistingExtraFileFilterResult<TExtraFile>(previouslyImported, filteredFiles);
        }

        private void Clean(Volume volume, List<string> filesOnDisk, List<string> importedFiles, List<TExtraFile> volumeFiles)
        {
            var alreadyImportedFileIds = volumeFiles.IntersectBy(f => Path.Combine(volume.Path, f.RelativePath), importedFiles, i => i, PathEqualityComparer.Instance)
                .Select(f => f.Id);

            var deletedFiles = volumeFiles.ExceptBy(f => Path.Combine(volume.Path, f.RelativePath), filesOnDisk, i => i, PathEqualityComparer.Instance)
                .Select(f => f.Id);

            _extraFileService.DeleteMany(alreadyImportedFileIds);
            _extraFileService.DeleteMany(deletedFiles);
        }
    }
}
