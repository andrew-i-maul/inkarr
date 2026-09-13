using System;
using System.Collections.Generic;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MetadataSource
{
    public interface IProvideVolumeInfo
    {
        Volume GetVolumeInfo(string inkarrId, bool useCache = true);
        HashSet<string> GetChangedVolumes(DateTime startTime);
    }
}
