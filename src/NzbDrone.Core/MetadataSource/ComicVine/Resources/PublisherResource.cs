using System.Text.Json.Serialization;

namespace NzbDrone.Core.MetadataSource.ComicVine.Resources
{
    // "publisher" is documented as a Volume field ("Primary publisher") but its own sub-fields
    // are not enumerated in the docs - id/name are the commonly-known ComicVine convention for
    // this kind of summary sub-object, not confirmed in the fetched docs text.
    public class PublisherResource
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
