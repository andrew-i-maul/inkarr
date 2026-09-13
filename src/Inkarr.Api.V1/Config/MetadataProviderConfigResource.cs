using Inkarr.Http.REST;
using NzbDrone.Core.Configuration;

namespace Inkarr.Api.V1.Config
{
    public class MetadataProviderConfigResource : RestResource
    {
        public WriteAudioTagsType WriteAudioTags { get; set; }
        public bool ScrubAudioTags { get; set; }
        public WriteIssueTagsType WriteIssueTags { get; set; }
        public bool UpdateCovers { get; set; }
        public bool EmbedMetadata { get; set; }
    }

    public static class MetadataProviderConfigResourceMapper
    {
        public static MetadataProviderConfigResource ToResource(IConfigService model)
        {
            return new MetadataProviderConfigResource
            {
                WriteAudioTags = model.WriteAudioTags,
                ScrubAudioTags = model.ScrubAudioTags,
                WriteIssueTags = model.WriteIssueTags,
                UpdateCovers = model.UpdateCovers,
                EmbedMetadata = model.EmbedMetadata
            };
        }
    }
}
