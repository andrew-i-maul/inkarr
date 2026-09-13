using System.Collections.Generic;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookRenamePayload : WebhookPayload
    {
        public WebhookVolume Volume { get; set; }
        public List<WebhookRenamedIssueFile> RenamedIssueFiles { get; set; }
    }
}
