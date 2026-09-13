using System.Collections.Generic;
using System.Linq;
using Inkarr.Http.REST;

namespace Inkarr.Api.V1.Series
{
    public class SeriesResource : RestResource
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public List<SeriesIssueLinkResource> Links { get; set; }
    }

    public static class SeriesResourceMapper
    {
        public static SeriesResource ToResource(this NzbDrone.Core.Issues.Series model)
        {
            if (model == null)
            {
                return null;
            }

            return new SeriesResource
            {
                Id = model.Id,
                Title = model.Title,
                Description = model.Description,
                Links = model.LinkItems.Value.ToResource()
            };
        }

        public static List<SeriesResource> ToResource(this IEnumerable<NzbDrone.Core.Issues.Series> models)
        {
            return models?.Select(ToResource).ToList();
        }
    }
}
