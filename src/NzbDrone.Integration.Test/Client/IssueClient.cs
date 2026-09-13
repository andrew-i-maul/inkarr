using System.Collections.Generic;
using Inkarr.Api.V1.Issues;
using RestSharp;

namespace NzbDrone.Integration.Test.Client
{
    public class IssueClient : ClientBase<IssueResource>
    {
        public IssueClient(IRestClient restClient, string apiKey)
            : base(restClient, apiKey, "issue")
        {
        }

        public List<IssueResource> GetIssuesInVolume(int volumeId)
        {
            var request = BuildRequest("?volumeId=" + volumeId.ToString());
            return Get<List<IssueResource>>(request);
        }
    }
}
