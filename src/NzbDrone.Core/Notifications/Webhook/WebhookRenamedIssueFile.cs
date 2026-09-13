using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookRenamedIssueFile : WebhookIssueFile
    {
        public WebhookRenamedIssueFile(RenamedIssueFile renamedMovie)
            : base(renamedMovie.IssueFile)
        {
            PreviousPath = renamedMovie.PreviousPath;
        }

        public string PreviousPath { get; set; }
    }
}
