using System.Collections.Generic;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MediaFiles.IssueImport.Identification
{
    public class CandidateEdition
    {
        public CandidateEdition()
        {
        }

        public CandidateEdition(Edition edition)
        {
            Edition = edition;
            ExistingFiles = new List<IssueFile>();
        }

        public Edition Edition { get; set; }
        public List<IssueFile> ExistingFiles { get; set; }
    }
}
