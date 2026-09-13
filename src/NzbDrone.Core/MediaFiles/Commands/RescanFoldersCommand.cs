using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.MediaFiles.Commands
{
    public class RescanFoldersCommand : Command
    {
        public RescanFoldersCommand()
        {
            // These are the settings used in the scheduled task
            Filter = FilterFilesType.Known;
            AddNewVolumes = true;
        }

        public RescanFoldersCommand(List<string> folders, FilterFilesType filter, bool addNewVolumes, List<int> volumeIds)
        {
            Folders = folders;
            Filter = filter;
            AddNewVolumes = addNewVolumes;
            VolumeIds = volumeIds;
        }

        public List<string> Folders { get; set; }
        public FilterFilesType Filter { get; set; }
        public bool AddNewVolumes { get; set; }
        public List<int> VolumeIds { get; set; }

        public override bool SendUpdatesToClient => true;
        public override bool RequiresDiskAccess => true;
    }
}
