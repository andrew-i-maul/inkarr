using System.Collections.Generic;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookImportPayload : WebhookPayload
    {
        public WebhookVolume Volume { get; set; }
        public WebhookIssue Issue { get; set; }
        public List<WebhookIssueFile> IssueFiles { get; set; }
        public List<WebhookIssueFile> DeletedFiles { get; set; }
        public bool IsUpgrade { get; set; }
        public string DownloadClient { get; set; }
        public string DownloadClientType { get; set; }
        public string DownloadId { get; set; }
    }
}
