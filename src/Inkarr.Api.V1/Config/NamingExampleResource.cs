using NzbDrone.Core.Organizer;

namespace Inkarr.Api.V1.Config
{
    public class NamingExampleResource
    {
        public string SingleIssueExample { get; set; }
        public string MultiPartIssueExample { get; set; }
        public string VolumeFolderExample { get; set; }
    }

    public static class NamingConfigResourceMapper
    {
        public static NamingConfigResource ToResource(this NamingConfig model)
        {
            return new NamingConfigResource
            {
                Id = model.Id,

                RenameIssues = model.RenameIssues,
                ReplaceIllegalCharacters = model.ReplaceIllegalCharacters,
                ColonReplacementFormat = (int)model.ColonReplacementFormat,
                StandardIssueFormat = model.StandardIssueFormat,
                VolumeFolderFormat = model.VolumeFolderFormat
            };
        }

        public static void AddToResource(this BasicNamingConfig basicNamingConfig, NamingConfigResource resource)
        {
            resource.IncludeVolumeName = basicNamingConfig.IncludeVolumeName;
            resource.IncludeIssueTitle = basicNamingConfig.IncludeIssueTitle;
            resource.IncludeQuality = basicNamingConfig.IncludeQuality;
            resource.ReplaceSpaces = basicNamingConfig.ReplaceSpaces;
            resource.Separator = basicNamingConfig.Separator;
            resource.NumberStyle = basicNamingConfig.NumberStyle;
        }

        public static NamingConfig ToModel(this NamingConfigResource resource)
        {
            return new NamingConfig
            {
                Id = resource.Id,

                RenameIssues = resource.RenameIssues,
                ReplaceIllegalCharacters = resource.ReplaceIllegalCharacters,
                ColonReplacementFormat = (ColonReplacementFormat)resource.ColonReplacementFormat,
                StandardIssueFormat = resource.StandardIssueFormat,
                VolumeFolderFormat = resource.VolumeFolderFormat,
            };
        }
    }
}
