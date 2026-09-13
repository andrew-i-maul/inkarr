using System.Collections.Generic;
using NzbDrone.Common.Messaging;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class VolumeRenamedEvent : IEvent
    {
        public Volume Volume { get; private set; }
        public List<RenamedIssueFile> RenamedFiles { get; private set; }

        public VolumeRenamedEvent(Volume volume, List<RenamedIssueFile> renamedFiles)
        {
            Volume = volume;
            RenamedFiles = renamedFiles;
        }
    }
}
