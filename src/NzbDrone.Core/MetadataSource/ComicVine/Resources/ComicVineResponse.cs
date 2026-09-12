using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NzbDrone.Core.MetadataSource.ComicVine.Resources
{
    // Envelope shape confirmed from comicvine.gamespot.com/api/documentation:
    // status_code, error, number_of_total_results, number_of_page_results, limit, offset, results.
    // The docs do not explicitly state whether a detail endpoint's "results" is a single object
    // versus a one-item structure distinct from a list endpoint's array - modeled here as two
    // separate envelope types (detail vs list) on the common assumption that detail endpoints
    // return a single object. This needs confirming against a real response once an API key exists.
    public class ComicVineDetailResponse<T>
    {
        [JsonPropertyName("status_code")]
        public int StatusCode { get; set; }

        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("results")]
        public T Results { get; set; }
    }

    public class ComicVineListResponse<T>
    {
        [JsonPropertyName("status_code")]
        public int StatusCode { get; set; }

        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("number_of_total_results")]
        public int NumberOfTotalResults { get; set; }

        [JsonPropertyName("number_of_page_results")]
        public int NumberOfPageResults { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("results")]
        public List<T> Results { get; set; } = new List<T>();
    }

    // Status codes as documented: 1 OK, 100 Invalid API Key, 101 Object Not Found,
    // 102 Error in URL Format, 103 jsonp requires json_callback, 104 Filter Error,
    // 105 Subscriber only video.
    public static class ComicVineStatusCode
    {
        public const int Ok = 1;
        public const int InvalidApiKey = 100;
        public const int ObjectNotFound = 101;
    }
}
