using NzbDrone.Common.Http;

namespace NzbDrone.Common.Cloud
{
    public interface IInkarrCloudRequestBuilder
    {
        IHttpRequestBuilderFactory Services { get; }
        IHttpRequestBuilderFactory Metadata { get; }
    }

    public class InkarrCloudRequestBuilder : IInkarrCloudRequestBuilder
    {
        public InkarrCloudRequestBuilder()
        {
            //TODO: Create Update Endpoint
            Services = new HttpRequestBuilder("https://readarr.servarr.com/v1/")
                .CreateFactory();

            Metadata = new HttpRequestBuilder("https://api.bookinfo.club/v1/{route}")
                .CreateFactory();
        }

        public IHttpRequestBuilderFactory Services { get; }

        public IHttpRequestBuilderFactory Metadata { get; }
    }
}
