namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookRetagPayload : WebhookPayload
    {
        public WebhookVolume Volume { get; set; }
        public WebhookIssueFile IssueFile { get; set; }
    }
}
