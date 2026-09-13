using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.TPL;
using NzbDrone.Core.Books;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource.ComicVine.Resources;

namespace NzbDrone.Core.MetadataSource.ComicVine
{
    // Replaces the Goodreads/BookInfo metadata sources with ComicVine as the sole source of
    // comic metadata. Domain mapping (see Phase 1 scoping): a ComicVine Volume (e.g. "Batman
    // (2016)") is the atomic, monitorable unit users subscribe to - it becomes an Author, since
    // Author is Readarr's root aggregate carrying QualityProfileId/RootFolderPath/Monitored/Tags,
    // and monitoring needs to happen per-volume, not per-publisher. AuthorMetadata's
    // Name/Overview/Images are populated directly from the Volume's own name/description/image
    // fields - not from person_credits, which is per-issue and has no stable "volume author".
    // A ComicVine Issue becomes a Book. ComicVine has no variant-cover/printing entity, so each
    // Book gets exactly one synthesized Edition (matching the existing "exactly one edition
    // monitored" invariant used by Goodreads/BookInfoProxy); the Volume's publisher name is
    // stored on that Edition's existing Publisher string field.
    public class ComicVineProxy : IProvideAuthorInfo, IProvideBookInfo, ISearchForNewBook, ISearchForNewAuthor, ISearchForNewEntity
    {
        private const string BaseUrl = "https://comicvine.gamespot.com/api/";

        // ComicVine's public docs (comicvine.gamespot.com/api/documentation) do not publish a
        // rate limit figure. This interval is a conservative, unverified default - tune once
        // real usage against a live key shows what's actually safe.
        private static readonly TimeSpan RateLimitInterval = TimeSpan.FromSeconds(1);

        private static readonly JsonSerializerOptions SerializerSettings = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = false
        };

        private readonly IHttpClient _httpClient;
        private readonly IConfigService _configService;
        private readonly IRateLimitService _rateLimitService;
        private readonly Logger _logger;

        public ComicVineProxy(IHttpClient httpClient,
                              IConfigService configService,
                              IRateLimitService rateLimitService,
                              Logger logger)
        {
            _httpClient = httpClient;
            _configService = configService;
            _rateLimitService = rateLimitService;
            _logger = logger;
        }

        private HttpRequestBuilder RequestBuilder()
        {
            var apiKey = _configService.ComicVineApiKey;

            if (apiKey.IsNullOrWhiteSpace())
            {
                throw new ComicVineException("No ComicVine API key configured. Register for a free key at comicvine.gamespot.com/api and set it before using this metadata source.");
            }

            return new HttpRequestBuilder(BaseUrl + "{route}/")
                .AddQueryParam("api_key", apiKey)
                .AddQueryParam("format", "json")
                .SetHeader("User-Agent", "Inkarr")
                .KeepAlive();
        }

        private HttpResponse Execute(HttpRequestBuilder request)
        {
            _rateLimitService.WaitAndPulse("comicvine", RateLimitInterval);

            var httpRequest = request.Build();
            httpRequest.SuppressHttpError = true;

            var response = _httpClient.Get(httpRequest);

            if (response.HasHttpError)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new ComicVineException("ComicVine resource not found: {0}", httpRequest.Url);
                }

                throw new HttpException(httpRequest, response);
            }

            return response;
        }

        private ComicVineDetailResponse<T> GetDetail<T>(string route)
        {
            var response = Execute(RequestBuilder().SetSegment("route", route));
            var envelope = JsonSerializer.Deserialize<ComicVineDetailResponse<T>>(response.Content, SerializerSettings);

            if (envelope.StatusCode == ComicVineStatusCode.ObjectNotFound)
            {
                throw new ComicVineException("ComicVine object not found: {0}", route);
            }

            if (envelope.StatusCode == ComicVineStatusCode.InvalidApiKey)
            {
                throw new ComicVineException("ComicVine rejected the configured API key.");
            }

            if (envelope.StatusCode != ComicVineStatusCode.Ok)
            {
                throw new ComicVineException("Unexpected ComicVine status_code {0} ({1}) for {2}", envelope.StatusCode, envelope.Error, route);
            }

            return envelope;
        }

        private List<T> GetAllPages<T>(string route, Dictionary<string, string> extraParams, int maxItems = 1000)
        {
            var results = new List<T>();
            var offset = 0;

            while (true)
            {
                var builder = RequestBuilder().SetSegment("route", route)
                    .AddQueryParam("offset", offset)
                    .AddQueryParam("limit", 100);

                foreach (var kvp in extraParams)
                {
                    builder = builder.AddQueryParam(kvp.Key, kvp.Value);
                }

                var response = Execute(builder);
                var envelope = JsonSerializer.Deserialize<ComicVineListResponse<T>>(response.Content, SerializerSettings);

                if (envelope.StatusCode != ComicVineStatusCode.Ok)
                {
                    throw new ComicVineException("Unexpected ComicVine status_code {0} ({1}) for {2}", envelope.StatusCode, envelope.Error, route);
                }

                results.AddRange(envelope.Results);

                if (results.Count >= envelope.NumberOfTotalResults || envelope.Results.Count == 0 || results.Count >= maxItems)
                {
                    break;
                }

                offset += envelope.Results.Count;
            }

            return results;
        }

        public Author GetAuthorInfo(string inkarrId, bool useCache = true)
        {
            _logger.Debug("Getting Volume with ComicVine id of {0}", inkarrId);

            // "4050-" is ComicVine's documented-elsewhere (not in the fetched official docs
            // text) resource-type prefix for volume ids in detail-endpoint URLs. NOT verified
            // against a real API response in this pass - confirm with a real key before trusting.
            var volume = GetDetail<VolumeResource>($"volume/4050-{inkarrId}").Results;

            var issues = GetAllPages<IssueResource>("issues", new Dictionary<string, string>
            {
                { "filter", $"volume:{inkarrId}" },
                { "field_list", "id,name,issue_number,deck,description,cover_date,store_date,volume,image,site_detail_url,api_detail_url" }
            });

            return MapAuthor(volume, issues);
        }

        public HashSet<string> GetChangedAuthors(DateTime startTime)
        {
            // ComicVine's public API has no documented "recently changed volumes" feed
            // analogous to Goodreads/BookInfo's author/changed route. Returning null tells
            // RefreshAuthorService to fall back to its own per-author staleness check
            // (see RefreshAuthorService.cs, ShouldRefresh), which is already null-tolerant.
            return null;
        }

        public Tuple<string, Book, List<AuthorMetadata>> GetBookInfo(string id)
        {
            // "4000-" prefix for issue ids: same caveat as the "4050-" volume prefix above.
            var issue = GetDetail<IssueResource>($"issue/4000-{id}").Results;
            var volume = GetDetail<VolumeResource>($"volume/4050-{issue.Volume.Id}").Results;

            var book = MapBook(issue, volume.Publisher?.Name);
            var authorMetadata = MapAuthorMetadata(volume);

            return Tuple.Create(volume.Id.ToString(CultureInfo.InvariantCulture), book, new List<AuthorMetadata> { authorMetadata });
        }

        public List<object> SearchForNewEntity(string title)
        {
            var trimmed = title?.Trim();

            if (trimmed.IsNotNullOrWhiteSpace() && trimmed.StartsWith("comicvine:", StringComparison.OrdinalIgnoreCase))
            {
                var idPart = trimmed.Substring("comicvine:".Length);

                if (int.TryParse(idPart, out var issueId))
                {
                    return SearchByGoodreadsBookId(issueId, true).Cast<object>().ToList();
                }

                return new List<object>();
            }

            var authors = SearchForNewAuthor(title);

            return authors.Cast<object>().ToList();
        }

        public List<Author> SearchForNewAuthor(string title)
        {
            var volumes = SearchVolumes(title);

            return volumes.Select(v => MapAuthor(v, new List<IssueResource>())).ToList();
        }

        public List<Book> SearchForNewBook(string title, string author, bool getAllEditions = true)
        {
            var q = author.IsNotNullOrWhiteSpace() ? $"{title} {author}" : title;

            var issues = SearchIssues(q);

            return issues.Select(i => MapBook(i, null)).ToList();
        }

        public List<Book> SearchByIsbn(string isbn)
        {
            // Individual comic issues are not indexed by ISBN in ComicVine's basic search -
            // ISBNs, where they exist at all for comics, are usually only assigned to
            // collected editions/trades, which are out of scope for this Volume/Issue mapping.
            _logger.Debug("SearchByIsbn is not meaningful for ComicVine single issues; returning no results for {0}", isbn);
            return new List<Book>();
        }

        public List<Book> SearchByAsin(string asin)
        {
            _logger.Debug("SearchByAsin is not meaningful for ComicVine single issues; returning no results for {0}", asin);
            return new List<Book>();
        }

        public List<Book> SearchByGoodreadsBookId(int goodreadsId, bool getAllEditions)
        {
            // Interface method name is a leftover from its Goodreads origin (ISearchForNewBook
            // is shared across metadata sources) - here it means "search by ComicVine issue id".
            try
            {
                var tuple = GetBookInfo(goodreadsId.ToString(CultureInfo.InvariantCulture));
                return new List<Book> { tuple.Item2 };
            }
            catch (ComicVineException e)
            {
                _logger.Warn(e, "Error looking up ComicVine issue id {0}", goodreadsId);
                return new List<Book>();
            }
        }

        private List<VolumeResource> SearchVolumes(string query)
        {
            var searchParams = new Dictionary<string, string>
            {
                { "query", query },
                { "resources", "volume" },
                { "field_list", "id,name,deck,description,start_year,count_of_issues,publisher,image,site_detail_url,api_detail_url" }
            };

            return GetAllPages<VolumeResource>("search", searchParams, 50);
        }

        private List<IssueResource> SearchIssues(string query)
        {
            var searchParams = new Dictionary<string, string>
            {
                { "query", query },
                { "resources", "issue" },
                { "field_list", "id,name,issue_number,deck,description,cover_date,store_date,volume,image,site_detail_url,api_detail_url" }
            };

            return GetAllPages<IssueResource>("search", searchParams, 50);
        }

        private static AuthorMetadata MapAuthorMetadata(VolumeResource volume)
        {
            var metadata = new AuthorMetadata
            {
                ForeignAuthorId = volume.Id.ToString(CultureInfo.InvariantCulture),
                TitleSlug = volume.Id.ToString(CultureInfo.InvariantCulture),
                Name = volume.Name,
                Overview = volume.Description.IsNotNullOrWhiteSpace() ? volume.Description : volume.Deck,
                Status = AuthorStatusType.Continuing
            };

            metadata.SortName = metadata.Name?.ToLowerInvariant();
            metadata.NameLastFirst = metadata.Name;
            metadata.SortNameLastFirst = metadata.SortName;

            if (volume.Image?.OriginalUrl.IsNotNullOrWhiteSpace() == true)
            {
                metadata.Images.Add(new MediaCover.MediaCover
                {
                    Url = volume.Image.OriginalUrl,
                    CoverType = MediaCoverTypes.Poster
                });
            }

            if (volume.SiteDetailUrl.IsNotNullOrWhiteSpace())
            {
                metadata.Links.Add(new Links { Url = volume.SiteDetailUrl, Name = "ComicVine" });
            }

            return metadata;
        }

        private static Author MapAuthor(VolumeResource volume, List<IssueResource> issues)
        {
            var metadata = MapAuthorMetadata(volume);
            var publisherName = volume.Publisher?.Name;

            var books = issues.Select(i => MapBook(i, publisherName)).ToList();
            books.ForEach(b => b.AuthorMetadata = metadata);

            return new Author
            {
                Metadata = metadata,
                CleanName = metadata.Name,
                Books = books
            };
        }

        private static Book MapBook(IssueResource issue, string publisherName)
        {
            var releaseDate = ParseComicVineDate(issue.CoverDate) ?? ParseComicVineDate(issue.StoreDate);

            var title = issue.Name.IsNotNullOrWhiteSpace()
                ? $"#{issue.IssueNumber} - {issue.Name}"
                : $"#{issue.IssueNumber}";

            var book = new Book
            {
                ForeignBookId = issue.Id.ToString(CultureInfo.InvariantCulture),
                Title = title,
                TitleSlug = issue.Id.ToString(CultureInfo.InvariantCulture),
                CleanTitle = title,
                ReleaseDate = releaseDate,
                AnyEditionOk = true
            };

            if (issue.SiteDetailUrl.IsNotNullOrWhiteSpace())
            {
                book.Links.Add(new Links { Url = issue.SiteDetailUrl, Name = "ComicVine" });
            }

            var edition = MapEdition(issue, publisherName);
            edition.Monitored = true;
            book.Editions = new List<Edition> { edition };

            return book;
        }

        private static Edition MapEdition(IssueResource issue, string publisherName)
        {
            var releaseDate = ParseComicVineDate(issue.CoverDate) ?? ParseComicVineDate(issue.StoreDate);

            var edition = new Edition
            {
                ForeignEditionId = issue.Id.ToString(CultureInfo.InvariantCulture),
                TitleSlug = issue.Id.ToString(CultureInfo.InvariantCulture),
                Title = issue.Name.IsNotNullOrWhiteSpace() ? issue.Name : $"#{issue.IssueNumber}",
                Overview = issue.Description.IsNotNullOrWhiteSpace() ? issue.Description : issue.Deck,
                Publisher = publisherName,
                ReleaseDate = releaseDate,
                Monitored = false
            };

            if (issue.Image?.OriginalUrl.IsNotNullOrWhiteSpace() == true)
            {
                edition.Images.Add(new MediaCover.MediaCover
                {
                    Url = issue.Image.OriginalUrl,
                    CoverType = MediaCoverTypes.Cover
                });
            }

            if (issue.SiteDetailUrl.IsNotNullOrWhiteSpace())
            {
                edition.Links.Add(new Links { Url = issue.SiteDetailUrl, Name = "ComicVine" });
            }

            return edition;
        }

        // cover_date/store_date are documented as date strings but the exact format
        // ("yyyy-MM-dd" is ComicVine's established community-known convention) is not spelled
        // out in the fetched docs text - parsed defensively, returns null rather than throwing
        // on anything unexpected so a single malformed date can't take down a whole import.
        private static DateTime? ParseComicVineDate(string value)
        {
            if (value.IsNullOrWhiteSpace())
            {
                return null;
            }

            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
                ? result
                : null;
        }
    }
}
