using System.Text.Json.Serialization;

namespace NzbDrone.Core.MetadataSource.ComicVine.Resources
{
    // Fields confirmed against comicvine.gamespot.com/api/documentation's Issue resource field list.
    // Note: issue_number is documented as "the number assigned to the issue within the volume set"
    // and is a string, not an integer - ComicVine issue numbers include non-numeric values
    // (e.g. "Annual 1", or half-numbers). Do not assume it parses cleanly to int.
    public class IssueResource
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("issue_number")]
        public string IssueNumber { get; set; }

        [JsonPropertyName("deck")]
        public string Deck { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("cover_date")]
        public string CoverDate { get; set; }

        [JsonPropertyName("store_date")]
        public string StoreDate { get; set; }

        [JsonPropertyName("volume")]
        public VolumeSummaryResource Volume { get; set; }

        [JsonPropertyName("image")]
        public ImageResource Image { get; set; }

        [JsonPropertyName("site_detail_url")]
        public string SiteDetailUrl { get; set; }

        [JsonPropertyName("api_detail_url")]
        public string ApiDetailUrl { get; set; }
    }
}
