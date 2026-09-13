using System.Collections.Generic;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.Extras
{
    public interface IImportExistingExtraFiles
    {
        int Order { get; }
        IEnumerable<ExtraFile> ProcessFiles(Volume volume, List<string> filesOnDisk, List<string> importedFiles);
    }
}
