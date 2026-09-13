using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.MediaFiles.IssueImport.Specifications
{
    public class VolumePathInRootFolderSpecification : IImportDecisionEngineSpecification<LocalEdition>
    {
        private readonly IRootFolderService _rootFolderService;
        private readonly Logger _logger;

        public VolumePathInRootFolderSpecification(IRootFolderService rootFolderService,
                                                   Logger logger)
        {
            _rootFolderService = rootFolderService;
            _logger = logger;
        }

        public Decision IsSatisfiedBy(LocalEdition item, DownloadClientItem downloadClientItem)
        {
            // Prevent imports to volumes that are no longer inside a root folder Inkarr manages
            var volume = item.Edition.Issue.Value.Volume.Value;

            // a new volume will have empty path, and will end up having path assinged based on file location
            var pathToCheck = volume.Path.IsNotNullOrWhiteSpace() ? volume.Path : item.LocalIssues.First().Path.GetParentPath();

            if (_rootFolderService.GetBestRootFolder(pathToCheck) == null)
            {
                _logger.Warn($"Destination folder {pathToCheck} not in a Root Folder, skipping import");
                return Decision.Reject($"Destination folder {pathToCheck} is not in a Root Folder");
            }

            return Decision.Accept();
        }
    }
}
