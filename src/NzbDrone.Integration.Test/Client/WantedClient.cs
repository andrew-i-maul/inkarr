using System.Collections.Generic;
using Inkarr.Api.V1.Issues;
using Inkarr.Http;
using RestSharp;

namespace NzbDrone.Integration.Test.Client
{
    public class WantedClient : ClientBase<IssueResource>
    {
        public WantedClient(IRestClient restClient, string apiKey, string resource)
            : base(restClient, apiKey, resource)
        {
        }

        public PagingResource<IssueResource> GetPagedIncludeVolume(int pageNumber, int pageSize, string sortKey, string sortDir, string filterKey = null, string filterValue = null, bool includeVolume = true)
        {
            var request = BuildRequest();
            request.AddParameter("page", pageNumber);
            request.AddParameter("pageSize", pageSize);
            request.AddParameter("sortKey", sortKey);
            request.AddParameter("sortDir", sortDir);

            if (filterKey != null && filterValue != null)
            {
                request.AddParameter("filterKey", filterKey);
                request.AddParameter("filterValue", filterValue);
            }

            request.AddParameter("includeVolume", includeVolume);

            return Get<PagingResource<IssueResource>>(request);
        }

        public List<IssueResource> GetIssuesInVolume(int volumeId)
        {
            var request = BuildRequest("?volumeId=" + volumeId.ToString());
            return Get<List<IssueResource>>(request);
        }
    }
}
