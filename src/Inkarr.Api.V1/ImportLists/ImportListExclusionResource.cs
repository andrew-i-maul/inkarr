using System.Collections.Generic;
using System.Linq;
using Inkarr.Http.REST;
using NzbDrone.Core.ImportLists.Exclusions;

namespace Inkarr.Api.V1.ImportLists
{
    public class ImportListExclusionResource : RestResource
    {
        public string ForeignId { get; set; }
        public string VolumeName { get; set; }
    }

    public static class ImportListExclusionResourceMapper
    {
        public static ImportListExclusionResource ToResource(this ImportListExclusion model)
        {
            if (model == null)
            {
                return null;
            }

            return new ImportListExclusionResource
            {
                Id = model.Id,
                ForeignId = model.ForeignId,
                VolumeName = model.Name,
            };
        }

        public static ImportListExclusion ToModel(this ImportListExclusionResource resource)
        {
            if (resource == null)
            {
                return null;
            }

            return new ImportListExclusion
            {
                Id = resource.Id,
                ForeignId = resource.ForeignId,
                Name = resource.VolumeName
            };
        }

        public static List<ImportListExclusionResource> ToResource(this IEnumerable<ImportListExclusion> filters)
        {
            return filters.Select(ToResource).ToList();
        }
    }
}
