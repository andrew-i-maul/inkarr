namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookVolumeDeletePayload : WebhookPayload
    {
        public WebhookVolume Volume { get; set; }
        public bool DeletedFiles { get; set; }
    }
}
