using System;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.Notifications.Goodreads
{
    public class GoodreadsOwnedIssues : GoodreadsNotificationBase<GoodreadsOwnedIssuesNotificationSettings>
    {
        public GoodreadsOwnedIssues(IHttpClient httpClient,
                                   Logger logger)
        : base(httpClient, logger)
        {
        }

        public override string Name => "Goodreads Owned Issues";
        public override string Link => "https://goodreads.com/";

        public override void OnReleaseImport(IssueDownloadMessage message)
        {
            var issueId = message.Issue.Editions.Value.Single(x => x.Monitored).ForeignEditionId;
            AddOwnedIssue(issueId);
        }

        private void AddOwnedIssue(string issueId)
        {
            var req = RequestBuilder()
                .Post()
                .SetSegment("route", "owned_issues.xml")
                .AddFormParameter("owned_issue[issue_id]", issueId)
                .AddFormParameter("owned_issue[condition_code]", Settings.Condition)
                .AddFormParameter("owned_issue[original_purchase_date]", DateTime.Now.ToString("O"));

            if (Settings.Description.IsNotNullOrWhiteSpace())
            {
                req.AddFormParameter("owned_issue[condition_description]", Settings.Description);
            }

            if (Settings.Location.IsNotNullOrWhiteSpace())
            {
                req.AddFormParameter("owned_issue[original_purchase_location]", Settings.Location);
            }

            OAuthExecute(req);
        }
    }
}
