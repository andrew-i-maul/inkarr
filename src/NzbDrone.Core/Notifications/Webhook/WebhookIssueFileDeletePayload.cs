namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookIssueFileDeletePayload : WebhookPayload
    {
        public WebhookVolume Volume { get; set; }
        public WebhookIssue Issue { get; set; }
        public WebhookIssueFile IssueFile { get; set; }
    }
}
