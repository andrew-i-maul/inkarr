using System.Diagnostics;
using System.Xml.Linq;

namespace NzbDrone.Core.MetadataSource.Goodreads
{
    /// <summary>
    /// This class models the best issue in a work, as defined by the Goodreads API.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public sealed class BestIssueResource : GoodreadsResource
    {
        public override string ElementName => "best_issue";

        /// <summary>
        /// The Id of this issue.
        /// </summary>
        public long Id { get; private set; }

        /// <summary>
        /// The title of this issue.
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// The Goodreads id of the volume.
        /// </summary>
        public long VolumeId { get; private set; }

        /// <summary>
        /// The name of the volume.
        /// </summary>
        public string VolumeName { get; private set; }

        /// <summary>
        /// The cover image of this issue.
        /// </summary>
        public string ImageUrl { get; private set; }

        public string LargeImageUrl { get; private set; }

        public override void Parse(XElement element)
        {
            Id = element.ElementAsLong("id");
            Title = element.ElementAsString("title");

            var volumeElement = element.Element("volume");
            if (volumeElement != null)
            {
                VolumeId = volumeElement.ElementAsLong("id");
                VolumeName = volumeElement.ElementAsString("name");
            }

            ImageUrl = element.ElementAsString("image_url");
            LargeImageUrl = element.ElementAsString("large_image_url");
        }
    }
}
