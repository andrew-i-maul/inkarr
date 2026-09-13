using System.Collections.Generic;
using System.Text.RegularExpressions;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public abstract class SearchCriteriaBase
    {
        private static readonly Regex NonWord = new Regex(@"[^\w`'’]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex BeginningThe = new Regex(@"^the\s", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public virtual bool MonitoredIssuesOnly { get; set; }
        public virtual bool UserInvokedSearch { get; set; }
        public virtual bool InteractiveSearch { get; set; }

        public Volume Volume { get; set; }
        public List<Issue> Issues { get; set; }

        public string VolumeQuery => GetQueryTitle(Volume.Name);

        public static string GetQueryTitle(string title)
        {
            Ensure.That(title, () => title).IsNotNullOrWhiteSpace();

            // Most VA issues are listed as VA, not Various Volumes
            // TODO: Needed in Inkarr??
            if (title == "Various Volumes")
            {
                title = "VA";
            }

            var cleanTitle = BeginningThe.Replace(title, string.Empty);

            cleanTitle = cleanTitle.Replace(" & ", " ");
            cleanTitle = cleanTitle.Replace(".", " ");
            cleanTitle = NonWord.Replace(cleanTitle, "+");

            //remove any repeating +s
            cleanTitle = Regex.Replace(cleanTitle, @"\+{2,}", "+");
            cleanTitle = cleanTitle.RemoveAccent();
            cleanTitle = cleanTitle.Trim('+', ' ');

            return cleanTitle.Length == 0 ? title : cleanTitle;
        }
    }
}
