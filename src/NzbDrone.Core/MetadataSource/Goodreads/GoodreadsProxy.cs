using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Http;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MetadataSource.Goodreads
{
    public interface IGoodreadsProxy
    {
        Issue GetIssueInfo(string foreignEditionId, bool useCache = true);
    }

    public class GoodreadsProxy : IGoodreadsProxy, IProvideSeriesInfo, IProvideListInfo
    {
        private readonly ICachedHttpResponseService _cachedHttpClient;
        private readonly Logger _logger;
        private readonly IHttpRequestBuilderFactory _requestBuilder;

        public GoodreadsProxy(ICachedHttpResponseService cachedHttpClient,
                              Logger logger)
        {
            _cachedHttpClient = cachedHttpClient;
            _logger = logger;

            _requestBuilder = new HttpRequestBuilder("https://www.goodreads.com/{route}")
                .AddQueryParam("key", new string("gSuM2Onzl6sjMU25HY1Xcd".Reverse().ToArray()))
                .AddQueryParam("_nc", "1")
                .SetHeader("User-Agent", "Dalvik/1.6.0 (Linux; U; Android 4.1.2; GT-I9100 Build/JZO54K)")
                .KeepAlive()
                .CreateFactory();
        }

        public SeriesResource GetSeriesInfo(int foreignSeriesId, bool useCache = true)
        {
            _logger.Debug("Getting Series with GoodreadsId of {0}", foreignSeriesId);

            var httpRequest = _requestBuilder.Create()
                .SetSegment("route", $"series/{foreignSeriesId}")
                .AddQueryParam("format", "xml")
                .Build();

            httpRequest.AllowAutoRedirect = true;
            httpRequest.SuppressHttpError = true;

            var httpResponse = _cachedHttpClient.Get(httpRequest, useCache, TimeSpan.FromDays(7));

            if (httpResponse.HasHttpError)
            {
                if (httpResponse.StatusCode == HttpStatusCode.BadRequest)
                {
                    throw new BadRequestException(foreignSeriesId.ToString());
                }
                else
                {
                    throw new HttpException(httpRequest, httpResponse);
                }
            }

            var resource = httpResponse.Deserialize<ShowSeriesResource>();

            return resource.Series;
        }

        public ListResource GetListInfo(int foreignListId, int page, bool useCache = true)
        {
            _logger.Debug("Getting List with GoodreadsId of {0}", foreignListId);

            var httpRequest = new HttpRequestBuilder("https://www.goodreads.com/issue/list/listopia.xml")
                .AddQueryParam("key", new string("whFzJP3Ud0gZsAdyXxSr7T".Reverse().ToArray()))
                .AddQueryParam("_nc", "1")
                .AddQueryParam("format", "xml")
                .AddQueryParam("id", foreignListId)
                .AddQueryParam("items_per_page", 30)
                .AddQueryParam("page", page)
                .SetHeader("User-Agent", "Goodreads/3.33.1 (iPhone; iOS 14.3; Scale/3.00)")
                .SetHeader("X_APPLE_DEVICE_MODEL", "iPhone")
                .SetHeader("x-gr-os-version", "iOS 14.3")
                .SetHeader("Accept-Language", "en-GB;q=1")
                .SetHeader("X_APPLE_APP_VERSION", "761")
                .SetHeader("x-gr-app-version", "761")
                .SetHeader("x-gr-hw-model", "iPhone11,6")
                .SetHeader("X_APPLE_SYSTEM_VERSION", "14.3")
                .KeepAlive()
                .Build();

            httpRequest.AllowAutoRedirect = true;
            httpRequest.SuppressHttpError = true;

            var httpResponse = _cachedHttpClient.Get(httpRequest, useCache, TimeSpan.FromDays(7));

            if (httpResponse.HasHttpError)
            {
                if (httpResponse.StatusCode == HttpStatusCode.BadRequest)
                {
                    throw new BadRequestException(foreignListId.ToString());
                }
                else
                {
                    throw new HttpException(httpRequest, httpResponse);
                }
            }

            return httpResponse.Deserialize<ListResource>();
        }

        public Issue GetIssueInfo(string foreignEditionId, bool useCache = true)
        {
            _logger.Debug("Getting Issue with GoodreadsId of {0}", foreignEditionId);

            var httpRequest = _requestBuilder.Create()
                .SetSegment("route", $"api/issue/basic_issue_data/{foreignEditionId}")
                .AddQueryParam("format", "xml")
                .Build();

            httpRequest.AllowAutoRedirect = true;
            httpRequest.SuppressHttpError = true;

            var httpResponse = _cachedHttpClient.Get(httpRequest, useCache, TimeSpan.FromDays(90));

            if (httpResponse.HasHttpError)
            {
                if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new IssueNotFoundException(foreignEditionId);
                }
                else if (httpResponse.StatusCode == HttpStatusCode.BadRequest)
                {
                    throw new BadRequestException(foreignEditionId);
                }
                else
                {
                    throw new HttpException(httpRequest, httpResponse);
                }
            }

            var resource = httpResponse.Deserialize<IssueResource>();

            var issue = MapIssue(resource);
            issue.CleanTitle = Parser.Parser.CleanVolumeName(issue.Title);

            var volumes = resource.Volumes.SelectList(MapVolume);
            issue.VolumeMetadata = volumes.First();

            return issue;
        }

        private static VolumeMetadata MapVolume(VolumeSummaryResource resource)
        {
            var volume = new VolumeMetadata
            {
                ForeignVolumeId = resource.Id.ToString(),
                Name = resource.Name.CleanSpaces(),
                TitleSlug = resource.Id.ToString()
            };

            volume.SortName = volume.Name.ToLower();
            volume.NameLastFirst = volume.Name.ToLastFirst();
            volume.SortNameLastFirst = volume.NameLastFirst.ToLower();

            if (resource.RatingsCount.HasValue)
            {
                volume.Ratings = new Ratings
                {
                    Votes = resource.RatingsCount ?? 0,
                    Value = resource.AverageRating ?? 0
                };
            }

            return volume;
        }

        private static Issue MapIssue(IssueResource resource)
        {
            var issue = new Issue
            {
                ForeignIssueId = resource.Work.Id.ToString(),
                Title = (resource.Work.OriginalTitle ?? resource.TitleWithoutSeries).CleanSpaces(),
                TitleSlug = resource.Work.Id.ToString(),
                ReleaseDate = resource.Work.OriginalPublicationDate ?? resource.PublicationDate,
                Ratings = new Ratings { Votes = resource.Work.RatingsCount, Value = resource.Work.AverageRating },
                AnyEditionOk = true
            };

            if (resource.EditionsUrl != null)
            {
                issue.Links.Add(new Links { Url = resource.EditionsUrl, Name = "Goodreads Editions" });
            }

            var edition = new Edition
            {
                ForeignEditionId = resource.Id.ToString(),
                TitleSlug = resource.Id.ToString(),
                Isbn13 = resource.Isbn13,
                Asin = resource.Asin ?? resource.KindleAsin,
                Title = resource.TitleWithoutSeries,
                Language = resource.LanguageCode,
                Overview = resource.Description,
                Format = resource.Format,
                IsEissue = resource.IsEissue,
                Disambiguation = resource.EditionInformation,
                Publisher = resource.Publisher,
                PageCount = resource.Pages,
                ReleaseDate = resource.PublicationDate,
                Ratings = new Ratings { Votes = resource.RatingsCount, Value = resource.AverageRating },
                Monitored = true
            };

            edition.Links.Add(new Links { Url = resource.Url, Name = "Goodreads Issue" });

            issue.Editions = new List<Edition> { edition };

            Debug.Assert(!issue.Editions.Value.Any() || issue.Editions.Value.Count(x => x.Monitored) == 1, "one edition monitored");

            return issue;
        }
    }
}
