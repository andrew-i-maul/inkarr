using NzbDrone.Common.Messaging;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class VolumeScanSkippedEvent : IEvent
    {
        public Volume Volume { get; private set; }
        public VolumeScanSkippedReason Reason { get; private set; }

        public VolumeScanSkippedEvent(Volume volume, VolumeScanSkippedReason reason)
        {
            Volume = volume;
            Reason = reason;
        }
    }

    public enum VolumeScanSkippedReason
    {
        RootFolderDoesNotExist,
        RootFolderIsEmpty
    }
}
