namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookVolumeAddedPayload : WebhookPayload
    {
        public WebhookVolume Volume { get; set; }
    }
}
