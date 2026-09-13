using System;
using System.Linq;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookIssue
    {
        public WebhookIssue()
        {
        }

        public WebhookIssue(Issue issue)
        {
            Id = issue.Id;
            GoodreadsId = issue.ForeignIssueId;
            Title = issue.Title;
            ReleaseDate = issue.ReleaseDate;
            Edition = new WebhookIssueEdition(issue.Editions.Value.Single(e => e.Monitored));
        }

        public int Id { get; set; }
        public string GoodreadsId { get; set; }
        public string Title { get; set; }
        public WebhookIssueEdition Edition { get; set; }
        public DateTime? ReleaseDate { get; set; }
    }
}
