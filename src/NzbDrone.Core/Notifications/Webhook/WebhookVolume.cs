using NzbDrone.Core.Issues;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookVolume
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string GoodreadsId { get; set; }

        public WebhookVolume()
        {
        }

        public WebhookVolume(Volume volume)
        {
            Id = volume.Id;
            Name = volume.Name;
            Path = volume.Path;
            GoodreadsId = volume.Metadata.Value.ForeignVolumeId;
        }
    }
}
