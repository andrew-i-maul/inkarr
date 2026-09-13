using System.Collections.Generic;
using System.Linq;
using Inkarr.Http.REST;
using NzbDrone.Core.Issues;

namespace Inkarr.Api.V1.Series
{
    public class SeriesIssueLinkResource : RestResource
    {
        public string Position { get; set; }
        public int SeriesPosition { get; set; }
        public int SeriesId { get; set; }
        public int IssueId { get; set; }
    }

    public static class SeriesIssueLinkResourceMapper
    {
        public static SeriesIssueLinkResource ToResource(this SeriesIssueLink model)
        {
            return new SeriesIssueLinkResource
            {
                Id = model.Id,
                Position = model.Position,
                SeriesPosition = model.SeriesPosition,
                SeriesId = model.SeriesId,
                IssueId = model.IssueId
            };
        }

        public static List<SeriesIssueLinkResource> ToResource(this IEnumerable<SeriesIssueLink> models)
        {
            return models?.Select(ToResource).ToList();
        }
    }
}
