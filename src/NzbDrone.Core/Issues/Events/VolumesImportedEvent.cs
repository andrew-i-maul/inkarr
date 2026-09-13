using System.Collections.Generic;
using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Issues.Events
{
    public class VolumesImportedEvent : IEvent
    {
        public List<int> VolumeIds { get; private set; }
        public bool DoRefresh { get; private set; }

        public VolumesImportedEvent(List<int> volumeIds, bool doRefresh = true)
        {
            VolumeIds = volumeIds;
            DoRefresh = doRefresh;
        }
    }
}
