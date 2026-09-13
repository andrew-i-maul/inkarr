using System.IO;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Organizer
{
    public class NamingConfig : ModelBase
    {
        public static NamingConfig Default => new NamingConfig
        {
            RenameIssues = false,
            ReplaceIllegalCharacters = true,
            ColonReplacementFormat = ColonReplacementFormat.Smart,
            StandardIssueFormat = "{Issue Title}" + Path.DirectorySeparatorChar + "{Volume Name} - {Issue Title}{ (PartNumber)}",
            VolumeFolderFormat = "{Volume Name}",
        };

        public bool RenameIssues { get; set; }
        public bool ReplaceIllegalCharacters { get; set; }
        public ColonReplacementFormat ColonReplacementFormat { get; set; }
        public string StandardIssueFormat { get; set; }
        public string VolumeFolderFormat { get; set; }
    }
}
