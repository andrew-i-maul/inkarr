using Newtonsoft.Json;

namespace NzbDrone.Core.MetadataSource.Goodreads
{
    public class SearchJsonResource
    {
        [JsonProperty("imageUrl")]
        public string ImageUrl { get; set; }

        [JsonProperty("issueId")]
        public int IssueId { get; set; }

        [JsonProperty("workId")]
        public int WorkId { get; set; }

        [JsonProperty("issueUrl")]
        public string IssueUrl { get; set; }

        [JsonProperty("from_search")]
        public bool FromSearch { get; set; }

        [JsonProperty("from_srp")]
        public bool FromSrp { get; set; }

        [JsonProperty("qid")]
        public string Qid { get; set; }

        [JsonProperty("rank")]
        public int Rank { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("issueTitleBare")]
        public string IssueTitleBare { get; set; }

        [JsonProperty("numPages")]
        public int PageCount { get; set; }

        [JsonProperty("avgRating")]
        public decimal AverageRating { get; set; }

        [JsonProperty("ratingsCount")]
        public int RatingsCount { get; set; }

        [JsonProperty("volume")]
        public VolumeJsonResource Volume { get; set; }

        [JsonProperty("kcrPreviewUrl")]
        public string KcrPreviewUrl { get; set; }

        [JsonProperty("description")]
        public DescriptionJsonResource Description { get; set; }
    }

    public class VolumeJsonResource
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("isGoodreadsVolume")]
        public bool IsGoodreadsVolume { get; set; }

        [JsonProperty("profileUrl")]
        public string ProfileUrl { get; set; }

        [JsonProperty("worksListUrl")]
        public string WorksListUrl { get; set; }
    }

    public class DescriptionJsonResource
    {
        [JsonProperty("html")]
        public string Html { get; set; }

        [JsonProperty("truncated")]
        public bool Truncated { get; set; }

        [JsonProperty("fullContentUrl")]
        public string FullContentUrl { get; set; }
    }
}
