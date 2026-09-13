using Inkarr.Http.REST;

namespace Inkarr.Api.V1.Config
{
    public class NamingConfigResource : RestResource
    {
        public bool RenameIssues { get; set; }
        public bool ReplaceIllegalCharacters { get; set; }
        public int ColonReplacementFormat { get; set; }
        public string StandardIssueFormat { get; set; }
        public string VolumeFolderFormat { get; set; }
        public bool IncludeVolumeName { get; set; }
        public bool IncludeIssueTitle { get; set; }
        public bool IncludeQuality { get; set; }
        public bool ReplaceSpaces { get; set; }
        public string Separator { get; set; }
        public string NumberStyle { get; set; }
    }
}
