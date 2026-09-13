using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.MetadataSource.Goodreads;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.Goodreads
{
    public class GoodreadsIssueshelf : GoodreadsNotificationBase<GoodreadsIssueshelfNotificationSettings>
    {
        public GoodreadsIssueshelf(IHttpClient httpClient,
                              Logger logger)
        : base(httpClient, logger)
        {
        }

        public override string Name => "Goodreads Issueshelves";
        public override string Link => "https://goodreads.com/";

        public override void OnReleaseImport(IssueDownloadMessage message)
        {
            var importedIssue = message.Issue;

            foreach (var shelf in Settings.RemoveIds)
            {
                // try to find the edition that we need to remove
                var listIssues = SearchShelf(shelf, importedIssue.VolumeMetadata.Value.Name);
                var toRemove = listIssues.Where(x => x.Issue.WorkId.ToString() == importedIssue.ForeignIssueId);

                foreach (var listIssue in toRemove)
                {
                    RemoveIssueFromShelves(listIssue.Issue.Id, shelf);
                }
            }

            var issueId = importedIssue.Editions.Value.Single(x => x.Monitored).ForeignEditionId;
            AddToShelves(issueId, Settings.AddIds);
        }

        public override void OnVolumeDelete(VolumeDeleteMessage deleteMessage)
        {
            if (deleteMessage.DeletedFiles)
            {
                foreach (var shelf in Settings.RemoveIds)
                {
                    var listIssues = SearchShelf(shelf, deleteMessage.Volume.Name);
                    var toRemove = listIssues.Where(x => deleteMessage.Volume.Issues.Value.Select(b => b.ForeignIssueId).Contains(x.Issue.WorkId.ToString()));

                    foreach (var listIssue in toRemove)
                    {
                        RemoveIssueFromShelves(listIssue.Issue.Id, shelf);
                    }
                }
            }
        }

        public override void OnIssueDelete(IssueDeleteMessage deleteMessage)
        {
            if (deleteMessage.DeletedFiles)
            {
                foreach (var shelf in Settings.RemoveIds)
                {
                    var listIssues = SearchShelf(shelf, deleteMessage.Issue.Volume.Value.Name);
                    var toRemove = listIssues.Where(x => x.Issue.WorkId.ToString() == deleteMessage.Issue.ForeignIssueId);

                    foreach (var listIssue in toRemove)
                    {
                        RemoveIssueFromShelves(listIssue.Issue.Id, shelf);
                    }
                }
            }
        }

        public override void OnIssueFileDelete(IssueFileDeleteMessage deleteMessage)
        {
            foreach (var shelf in Settings.RemoveIds)
            {
                var listIssues = SearchShelf(shelf, deleteMessage.Issue.Volume.Value.Name);
                var toRemove = listIssues.Where(x => x.Issue.WorkId.ToString() == deleteMessage.Issue.ForeignIssueId);

                foreach (var listIssue in toRemove)
                {
                    RemoveIssueFromShelves(listIssue.Issue.Id, shelf);
                }
            }
        }

        public override object RequestAction(string action, IDictionary<string, string> query)
        {
            if (action == "getIssueshelves")
            {
                if (Settings.AccessToken.IsNullOrWhiteSpace())
                {
                    return new
                    {
                        shelves = new List<object>()
                    };
                }

                Settings.Validate().Filter("AccessToken").ThrowOnError();

                var shelves = new List<UserShelfResource>();
                var page = 0;

                while (true)
                {
                    var curr = GetShelfList(++page);
                    if (curr == null || curr.Count == 0)
                    {
                        break;
                    }

                    shelves.AddRange(curr);
                }

                _logger.Trace($"Name: {query["name"]} {query["name"] == "removeIds"}");

                var helptext = new
                {
                    addIds = $"Add imported issue to {Settings.UserName}'s shelves:",
                    removeIds = $"Remove imported issue from {Settings.UserName}'s shelves:"
                };

                return new
                {
                    options = new
                    {
                        helptext,
                        user = Settings.UserName,
                        shelves = shelves.OrderBy(p => p.Name)
                        .Select(p => new
                        {
                            id = p.Name,
                            name = p.Name
                        })
                    }
                };
            }
            else
            {
                return base.RequestAction(action, query);
            }
        }

        private IReadOnlyList<UserShelfResource> GetShelfList(int page)
        {
            try
            {
                var builder = RequestBuilder()
                    .SetSegment("route", $"shelf/list.xml")
                    .AddQueryParam("user_id", Settings.UserId)
                    .AddQueryParam("page", page);

                var httpResponse = OAuthExecute(builder);

                return httpResponse.Deserialize<PaginatedList<UserShelfResource>>("shelves").List;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Error fetching issueshelves from Goodreads");
                return new List<UserShelfResource>();
            }
        }

        private IReadOnlyList<ReviewResource> SearchShelf(string shelf, string query)
        {
            List<ReviewResource> results = new ();

            while (true)
            {
                var page = 1;

                try
                {
                    var builder = RequestBuilder()
                        .SetSegment("route", $"review/list.xml")
                        .AddQueryParam("v", 2)
                        .AddQueryParam("id", Settings.UserId)
                        .AddQueryParam("shelf", shelf)
                        .AddQueryParam("per_page", 200)
                        .AddQueryParam("page", page++)
                        .AddQueryParam("search[query]", query);

                    var httpResponse = OAuthExecute(builder);

                    var resource = httpResponse.Deserialize<PaginatedList<ReviewResource>>("reviews");

                    results.AddRange(resource.List);

                    if (resource.Pagination.End >= resource.Pagination.TotalItems)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Error fetching issueshelves from Goodreads");
                    return results;
                }
            }

            return results;
        }

        private void RemoveIssueFromShelves(long issueId, string shelf)
        {
            var req = RequestBuilder()
                .Post()
                .SetSegment("route", "shelf/add_to_shelf.xml")
                .AddFormParameter("name", shelf)
                .AddFormParameter("issue_id", issueId)
                .AddFormParameter("a", "remove");

            // in case not found in shelf
            req.SuppressHttpError = true;

            OAuthExecute(req);
        }

        private void AddToShelves(string issueId, IEnumerable<string> shelves)
        {
            var req = RequestBuilder()
                .Post()
                .SetSegment("route", "shelf/add_issues_to_shelves.xml")
                .AddFormParameter("issueids", issueId)
                .AddFormParameter("shelves", shelves.ConcatToString());

            OAuthExecute(req);
        }
    }
}
