using System;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookIssueFile
    {
        public WebhookIssueFile()
        {
        }

        public WebhookIssueFile(IssueFile issueFile)
        {
            Id = issueFile.Id;
            Path = issueFile.Path;
            Quality = issueFile.Quality.Quality.Name;
            QualityVersion = issueFile.Quality.Revision.Version;
            ReleaseGroup = issueFile.ReleaseGroup;
            SceneName = issueFile.SceneName;
            Size = issueFile.Size;
            DateAdded = issueFile.DateAdded;
        }

        public int Id { get; set; }
        public string Path { get; set; }
        public string Quality { get; set; }
        public int QualityVersion { get; set; }
        public string ReleaseGroup { get; set; }
        public string SceneName { get; set; }
        public long Size { get; set; }
        public DateTime DateAdded { get; set; }
    }
}
