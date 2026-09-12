using System.Text.Json.Serialization;

namespace NzbDrone.Core.MetadataSource.ComicVine.Resources
{
    // ComicVine's docs list "image" as a Volume/Issue field but do not enumerate its sub-fields.
    // original_url is the commonly-known highest-resolution field from ComicVine's public API
    // (community knowledge, not confirmed in the fetched docs text) - verify against a real
    // response once an API key is available, and add the other known size variants
    // (icon_url/medium_url/small_url/thumb_url/tiny_url/screen_url) if useful.
    public class ImageResource
    {
        [JsonPropertyName("original_url")]
        public string OriginalUrl { get; set; }
    }
}
