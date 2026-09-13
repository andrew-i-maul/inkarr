using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Issues.Events
{
    public class VolumeMovedEvent : IEvent
    {
        public Volume Volume { get; set; }
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }

        public VolumeMovedEvent(Volume volume, string sourcePath, string destinationPath)
        {
            Volume = volume;
            SourcePath = sourcePath;
            DestinationPath = destinationPath;
        }
    }
}
