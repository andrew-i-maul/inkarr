namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookIssueDeletePayload : WebhookPayload
    {
        public WebhookVolume Volume { get; set; }
        public WebhookIssue Issue { get; set; }
        public bool DeletedFiles { get; set; }
    }
}
