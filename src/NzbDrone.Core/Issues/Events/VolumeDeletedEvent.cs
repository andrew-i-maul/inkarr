using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Issues.Events
{
    public class VolumeDeletedEvent : IEvent
    {
        public Volume Volume { get; private set; }
        public bool DeleteFiles { get; private set; }
        public bool AddImportListExclusion { get; private set; }

        public VolumeDeletedEvent(Volume volume, bool deleteFiles, bool addImportListExclusion)
        {
            Volume = volume;
            DeleteFiles = deleteFiles;
            AddImportListExclusion = addImportListExclusion;
        }
    }
}
