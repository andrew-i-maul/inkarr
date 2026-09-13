using System.Collections.Generic;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MetadataSource
{
    public interface ISearchForNewVolume
    {
        List<Volume> SearchForNewVolume(string title);
    }
}
