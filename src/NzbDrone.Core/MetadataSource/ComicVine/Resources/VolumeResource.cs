using System.Text.Json.Serialization;

namespace NzbDrone.Core.MetadataSource.ComicVine.Resources
{
    // Fields confirmed against comicvine.gamespot.com/api/documentation's Volume resource field list.
    public class VolumeResource
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("deck")]
        public string Deck { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("start_year")]
        public string StartYear { get; set; }

        [JsonPropertyName("count_of_issues")]
        public int CountOfIssues { get; set; }

        [JsonPropertyName("publisher")]
        public PublisherResource Publisher { get; set; }

        [JsonPropertyName("image")]
        public ImageResource Image { get; set; }

        [JsonPropertyName("site_detail_url")]
        public string SiteDetailUrl { get; set; }

        [JsonPropertyName("api_detail_url")]
        public string ApiDetailUrl { get; set; }
    }

    public class VolumeSummaryResource
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("api_detail_url")]
        public string ApiDetailUrl { get; set; }
    }
}
