using System.Collections.Generic;

namespace Inkarr.Api.V1.Volume
{
    public class VolumeEditorDeleteResource
    {
        public List<int> VolumeIds { get; set; }
        public bool DeleteFiles { get; set; }
    }
}
