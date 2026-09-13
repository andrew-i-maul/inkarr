using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MetadataSource.Goodreads;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.ImportLists.Goodreads
{
    public class GoodreadsOwnedIssuesImportListSettings : GoodreadsSettingsBase<GoodreadsOwnedIssuesImportListSettings>
    {
    }

    public class GoodreadsOwnedIssues : GoodreadsImportListBase<GoodreadsOwnedIssuesImportListSettings>
    {
        public GoodreadsOwnedIssues(IImportListStatusService importListStatusService,
                                   IConfigService configService,
                                   IParsingService parsingService,
                                   IHttpClient httpClient,
                                   Logger logger)
        : base(importListStatusService, configService, parsingService, httpClient, logger)
        {
        }

        public override string Name => "Goodreads Owned Issues";
        public override TimeSpan MinRefreshInterval => TimeSpan.FromHours(12);

        public override IList<ImportListItemInfo> Fetch()
        {
            var reviews = new List<OwnedIssueResource>();
            var page = 0;

            while (true)
            {
                var curr = GetOwned(++page);

                if (curr == null || curr.Count == 0)
                {
                    break;
                }

                reviews.AddRange(curr);
            }

            var result = reviews.Select(x => new ImportListItemInfo
            {
                Volume = x.Issue.Volumes.First().Name.CleanSpaces(),
                VolumeGoodreadsId = x.Issue.Volumes.First().Id.ToString(),
                Issue = x.Issue.TitleWithoutSeries.CleanSpaces(),
                EditionGoodreadsId = x.Issue.Id.ToString()
            }).ToList();

            return CleanupListItems(result);
        }

        private IReadOnlyList<OwnedIssueResource> GetOwned(int page)
        {
            try
            {
                var builder = RequestBuilder()
                    .SetSegment("route", $"owned_issues/user")
                    .AddQueryParam("format", "xml")
                    .AddQueryParam("id", Settings.UserId)
                    .AddQueryParam("page", page);

                var httpResponse = OAuthGet(builder);

                return httpResponse.Deserialize<PaginatedList<OwnedIssueResource>>("owned_issues").List;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Error fetching issueshelves from Goodreads");
                return new List<OwnedIssueResource>();
            }
        }
    }
}
