using System.Linq;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MetadataSource.ComicVine;
using NzbDrone.Core.MetadataSource.ComicVine.Resources;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MetadataSource.ComicVine
{
    [TestFixture]
    public class ComicVineProxyFixture : CoreTest<ComicVineProxy>
    {
        // Captured verbatim from a real, live call made during Phase 1 development:
        // GET /api/search/?resources=volume&query=Batman&field_list=id,name,start_year,publisher
        // (with a real api_key and format=json). This is genuine ComicVine response data, not
        // a fabricated fixture - it is what proved the publisher{id,name} shape and the overall
        // envelope (status_code/error/results) are correct.
        private const string RealBatmanSearchResponse =
            "{\"error\":\"OK\",\"limit\":3,\"offset\":0,\"number_of_page_results\":3," +
            "\"number_of_total_results\":2221,\"status_code\":1,\"results\":[" +
            "{\"id\":126840,\"name\":\"Batman\",\"publisher\":{\"api_detail_url\":\"https://comicvine.gamespot.com/api/publisher/4010-2365/\",\"id\":2365,\"name\":\"Editorial Novaro\"},\"start_year\":\"1954\",\"resource_type\":\"volume\"}," +
            "{\"id\":796,\"name\":\"Batman\",\"publisher\":{\"api_detail_url\":\"https://comicvine.gamespot.com/api/publisher/4010-10/\",\"id\":10,\"name\":\"DC Comics\"},\"start_year\":\"1940\",\"resource_type\":\"volume\"}," +
            "{\"id\":77146,\"name\":\"Batman\",\"publisher\":{\"api_detail_url\":\"https://comicvine.gamespot.com/api/publisher/4010-2538/\",\"id\":2538,\"name\":\"Grupo Editorial Vid\"},\"start_year\":\"1987\",\"resource_type\":\"volume\"}]," +
            "\"version\":\"1.0\"}";

        // Synthesized (not from a live call) for the fields the real search response above didn't
        // request via field_list - description/image/site_detail_url. Shape follows the docs and
        // the same publisher id/name shape the real call above confirmed.
        private const string VolumeDetailResponse =
            "{\"status_code\":1,\"error\":\"OK\",\"results\":{" +
            "\"id\":796,\"name\":\"Batman\",\"deck\":\"The Dark Knight of Gotham City.\"," +
            "\"description\":\"Batman is a costumed vigilante who protects Gotham City.\"," +
            "\"start_year\":\"1940\",\"count_of_issues\":716," +
            "\"publisher\":{\"id\":10,\"name\":\"DC Comics\"}," +
            "\"image\":{\"original_url\":\"https://comicvine.gamespot.com/a/uploads/original/batman.jpg\"}," +
            "\"site_detail_url\":\"https://comicvine.gamespot.com/batman/4050-796/\"," +
            "\"api_detail_url\":\"https://comicvine.gamespot.com/api/volume/4050-796/\"}}";

        private const string IssuesListResponse =
            "{\"status_code\":1,\"error\":\"OK\",\"number_of_total_results\":2,\"number_of_page_results\":2,\"limit\":100,\"offset\":0,\"results\":[" +
            "{\"id\":111,\"name\":null,\"issue_number\":\"1\",\"deck\":null,\"description\":\"The origin issue.\"," +
            "\"cover_date\":\"1940-04-25\",\"store_date\":null," +
            "\"volume\":{\"id\":796,\"name\":\"Batman\",\"api_detail_url\":\"https://comicvine.gamespot.com/api/volume/4050-796/\"}," +
            "\"image\":{\"original_url\":\"https://comicvine.gamespot.com/a/uploads/original/issue1.jpg\"}," +
            "\"site_detail_url\":\"https://comicvine.gamespot.com/batman-1/4000-111/\"," +
            "\"api_detail_url\":\"https://comicvine.gamespot.com/api/issue/4000-111/\"}," +
            "{\"id\":112,\"name\":\"The Case of the Chemical Syndicate\",\"issue_number\":\"Annual 1\",\"deck\":null,\"description\":null," +
            "\"cover_date\":null,\"store_date\":\"1940-05-01\"," +
            "\"volume\":{\"id\":796,\"name\":\"Batman\",\"api_detail_url\":\"https://comicvine.gamespot.com/api/volume/4050-796/\"}," +
            "\"image\":null," +
            "\"site_detail_url\":\"https://comicvine.gamespot.com/batman-annual-1/4000-112/\"," +
            "\"api_detail_url\":\"https://comicvine.gamespot.com/api/issue/4000-112/\"}]}";

        private const string IssueDetailResponse =
            "{\"status_code\":1,\"error\":\"OK\",\"results\":" +
            "{\"id\":111,\"name\":null,\"issue_number\":\"1\",\"deck\":null,\"description\":\"The origin issue.\"," +
            "\"cover_date\":\"1940-04-25\",\"store_date\":null," +
            "\"volume\":{\"id\":796,\"name\":\"Batman\",\"api_detail_url\":\"https://comicvine.gamespot.com/api/volume/4050-796/\"}," +
            "\"image\":{\"original_url\":\"https://comicvine.gamespot.com/a/uploads/original/issue1.jpg\"}," +
            "\"site_detail_url\":\"https://comicvine.gamespot.com/batman-1/4000-111/\"," +
            "\"api_detail_url\":\"https://comicvine.gamespot.com/api/issue/4000-111/\"}}";

        private const string ObjectNotFoundResponse =
            "{\"status_code\":101,\"error\":\"Object Not Found\",\"results\":[]}";

        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.ComicVineApiKey)
                  .Returns("test-api-key");
        }

        private void GivenHttpResponse(string urlContains, string jsonContent)
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(x => x.Get(It.Is<HttpRequest>(r => r.Url.ToString().Contains(urlContains))))
                  .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader(), jsonContent, HttpStatusCode.OK));
        }

        [Test]
        public void should_deserialize_snake_case_volume_envelope()
        {
            var envelope = JsonSerializer.Deserialize<ComicVineDetailResponse<VolumeResource>>(VolumeDetailResponse);

            envelope.StatusCode.Should().Be(ComicVineStatusCode.Ok);
            envelope.Results.Id.Should().Be(796);
            envelope.Results.Name.Should().Be("Batman");
            envelope.Results.StartYear.Should().Be("1940");
            envelope.Results.Publisher.Name.Should().Be("DC Comics");
            envelope.Results.Image.OriginalUrl.Should().EndWith("batman.jpg");
        }

        [Test]
        public void should_deserialize_snake_case_issue_with_non_numeric_issue_number()
        {
            var envelope = JsonSerializer.Deserialize<ComicVineListResponse<IssueResource>>(IssuesListResponse);

            envelope.Results.Should().HaveCount(2);
            envelope.Results[0].IssueNumber.Should().Be("1");
            envelope.Results[1].IssueNumber.Should().Be("Annual 1");
            envelope.Results[1].Volume.Id.Should().Be(796);
        }

        [Test]
        public void should_map_search_results_using_real_captured_response()
        {
            GivenHttpResponse("search/", RealBatmanSearchResponse);

            var volumes = Subject.SearchForNewVolume("Batman");

            volumes.Should().HaveCount(3);

            var dcBatman = volumes.Single(a => a.Metadata.Value.ForeignVolumeId == "796");
            dcBatman.Name.Should().Be("Batman");
            dcBatman.CleanName.Should().Be("Batman");
        }

        [Test]
        public void should_get_volume_info_with_issues_and_publisher_on_edition()
        {
            GivenHttpResponse("volume/4050-796", VolumeDetailResponse);
            GivenHttpResponse("issues/", IssuesListResponse);

            var volume = Subject.GetVolumeInfo("796");

            volume.Name.Should().Be("Batman");
            volume.Metadata.Value.Overview.Should().Be("Batman is a costumed vigilante who protects Gotham City.");
            volume.Issues.Value.Should().HaveCount(2);

            var issue1 = volume.Issues.Value.Single(b => b.ForeignIssueId == "111");
            issue1.Title.Should().Be("#1");
            issue1.Editions.Value.Should().HaveCount(1);
            issue1.Editions.Value.Single().Publisher.Should().Be("DC Comics");

            var annual = volume.Issues.Value.Single(b => b.ForeignIssueId == "112");
            annual.Title.Should().Be("#Annual 1 - The Case of the Chemical Syndicate");
        }

        [Test]
        public void should_get_issue_info_and_synthesize_exactly_one_edition()
        {
            GivenHttpResponse("issue/4000-111", IssueDetailResponse);
            GivenHttpResponse("volume/4050-796", VolumeDetailResponse);

            var result = Subject.GetIssueInfo("111");

            result.Item1.Should().Be("796");
            result.Item2.Editions.Value.Should().HaveCount(1);
            result.Item2.Editions.Value.Single().Publisher.Should().Be("DC Comics");
            result.Item3.Should().ContainSingle(m => m.ForeignVolumeId == "796");
        }

        [Test]
        public void should_throw_when_volume_not_found()
        {
            GivenHttpResponse("volume/", ObjectNotFoundResponse);

            Assert.Throws<ComicVineException>(() => Subject.GetVolumeInfo("999999"));
        }

        [Test]
        public void should_throw_and_make_no_http_call_when_api_key_missing()
        {
            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.ComicVineApiKey)
                  .Returns(string.Empty);

            Assert.Throws<ComicVineException>(() => Subject.GetVolumeInfo("796"));

            Mocker.GetMock<IHttpClient>().Verify(x => x.Get(It.IsAny<HttpRequest>()), Times.Never());
        }

        [Test]
        public void should_return_empty_for_isbn_and_asin_search_without_any_http_call()
        {
            Subject.SearchByIsbn("9780000000000").Should().BeEmpty();
            Subject.SearchByAsin("B000000000").Should().BeEmpty();

            Mocker.GetMock<IHttpClient>().Verify(x => x.Get(It.IsAny<HttpRequest>()), Times.Never());
        }
    }
}
