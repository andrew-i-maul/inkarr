using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace NzbDrone.Core.Issues
{
    public interface IRefreshSeriesIssueLinkService
    {
        bool RefreshSeriesIssueLinkInfo(List<SeriesIssueLink> add, List<SeriesIssueLink> update, List<Tuple<SeriesIssueLink, SeriesIssueLink>> merge, List<SeriesIssueLink> delete, List<SeriesIssueLink> upToDate, List<SeriesIssueLink> remoteSeriesIssueLinks, bool forceUpdateFileTags);
    }

    public class RefreshSeriesIssueLinkService : IRefreshSeriesIssueLinkService
    {
        private readonly ISeriesIssueLinkService _seriesIssueLinkService;
        private readonly Logger _logger;

        public RefreshSeriesIssueLinkService(ISeriesIssueLinkService trackService,
                                            Logger logger)
        {
            _seriesIssueLinkService = trackService;
            _logger = logger;
        }

        public bool RefreshSeriesIssueLinkInfo(List<SeriesIssueLink> add, List<SeriesIssueLink> update, List<Tuple<SeriesIssueLink, SeriesIssueLink>> merge, List<SeriesIssueLink> delete, List<SeriesIssueLink> upToDate, List<SeriesIssueLink> remoteSeriesIssueLinks, bool forceUpdateFileTags)
        {
            var updateList = new List<SeriesIssueLink>();

            foreach (var link in update)
            {
                var remoteSeriesIssueLink = remoteSeriesIssueLinks.Single(e => e.Issue.Value.Id == link.IssueId);
                link.UseMetadataFrom(remoteSeriesIssueLink);

                // make sure title is not null
                updateList.Add(link);
            }

            _seriesIssueLinkService.DeleteMany(delete);
            _seriesIssueLinkService.UpdateMany(updateList);

            return add.Any() || delete.Any() || updateList.Any() || merge.Any();
        }
    }
}
