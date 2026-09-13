using System.Collections.Generic;
using System.Linq;
using Inkarr.Http.REST;

namespace Inkarr.Api.V1.Issues
{
    public class RenameIssueResource : RestResource
    {
        public int VolumeId { get; set; }
        public int IssueId { get; set; }
        public int IssueFileId { get; set; }
        public string ExistingPath { get; set; }
        public string NewPath { get; set; }
    }

    public static class RenameIssueResourceMapper
    {
        public static RenameIssueResource ToResource(this NzbDrone.Core.MediaFiles.RenameIssueFilePreview model)
        {
            if (model == null)
            {
                return null;
            }

            return new RenameIssueResource
            {
                VolumeId = model.VolumeId,
                IssueId = model.IssueId,
                IssueFileId = model.IssueFileId,
                ExistingPath = model.ExistingPath,
                NewPath = model.NewPath
            };
        }

        public static List<RenameIssueResource> ToResource(this IEnumerable<NzbDrone.Core.MediaFiles.RenameIssueFilePreview> models)
        {
            return models.Select(ToResource).ToList();
        }
    }
}
