using System.Collections.Generic;
using System.Net;
using Inkarr.Api.V1.Volume;
using RestSharp;

namespace NzbDrone.Integration.Test.Client
{
    public class VolumeClient : ClientBase<VolumeResource>
    {
        public VolumeClient(IRestClient restClient, string apiKey)
            : base(restClient, apiKey)
        {
        }

        public List<VolumeResource> Lookup(string term)
        {
            var request = BuildRequest("lookup");
            request.AddQueryParameter("term", term);
            return Get<List<VolumeResource>>(request);
        }

        public List<VolumeResource> Editor(VolumeEditorResource volume)
        {
            var request = BuildRequest("editor");
            request.AddJsonBody(volume);
            return Put<List<VolumeResource>>(request);
        }

        public VolumeResource Get(string slug, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var request = BuildRequest(slug);
            return Get<VolumeResource>(request, statusCode);
        }
    }

    public class SystemInfoClient : ClientBase<VolumeResource>
    {
        public SystemInfoClient(IRestClient restClient, string apiKey)
            : base(restClient, apiKey)
        {
        }
    }
}
