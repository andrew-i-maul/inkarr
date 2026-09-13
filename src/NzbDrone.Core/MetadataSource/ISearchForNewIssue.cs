using System.Collections.Generic;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MetadataSource
{
    public interface ISearchForNewIssue
    {
        List<Issue> SearchForNewIssue(string title, string volume, bool getAllEditions = true);
        List<Issue> SearchByIsbn(string isbn);
        List<Issue> SearchByAsin(string asin);
        List<Issue> SearchByGoodreadsIssueId(int goodreadsId, bool getAllEditions);
    }
}
