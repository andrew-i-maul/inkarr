using System.Collections.Generic;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Extras.Metadata
{
    public interface IMetadata : IProvider
    {
        string GetFilenameAfterMove(Volume volume, IssueFile issueFile, MetadataFile metadataFile);
        string GetFilenameAfterMove(Volume volume, string issuePath, MetadataFile metadataFile);
        MetadataFile FindMetadataFile(Volume volume, string path);
        MetadataFileResult VolumeMetadata(Volume volume);
        MetadataFileResult IssueMetadata(Volume volume, IssueFile issueFile);
        List<ImageFileResult> VolumeImages(Volume volume);
        List<ImageFileResult> IssueImages(Volume volume, IssueFile issueFile);
    }
}
