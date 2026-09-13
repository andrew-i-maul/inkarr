using System.Collections.Generic;
using Newtonsoft.Json;

namespace NzbDrone.Core.Issues.Calibre
{
    public class CalibreImportJob
    {
        [JsonProperty("issue_id")]
        public int Id { get; set; }
        [JsonProperty("id")]
        public int JobId { get; set; }
        public string Filename { get; set; }
        public List<string> Volumes { get; set; }
        public string Title { get; set; }
        public List<string> Languages { get; set; }
    }
}
