using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentValidation;
using Inkarr.Http;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;
using HttpStatusCode = System.Net.HttpStatusCode;

namespace Inkarr.Api.V1.Indexers
{
    [V1ApiController]
    public class ReleaseController : ReleaseControllerBase
    {
        private readonly IFetchAndParseRss _rssFetcherAndParser;
        private readonly ISearchForReleases _releaseSearchService;
        private readonly IMakeDownloadDecision _downloadDecisionMaker;
        private readonly IPrioritizeDownloadDecision _prioritizeDownloadDecision;
        private readonly IDownloadService _downloadService;
        private readonly IVolumeService _volumeService;
        private readonly IIssueService _issueService;
        private readonly IParsingService _parsingService;
        private readonly Logger _logger;

        private readonly ICached<RemoteIssue> _remoteIssueCache;

        public ReleaseController(IFetchAndParseRss rssFetcherAndParser,
                             ISearchForReleases releaseSearchService,
                             IMakeDownloadDecision downloadDecisionMaker,
                             IPrioritizeDownloadDecision prioritizeDownloadDecision,
                             IDownloadService downloadService,
                             IVolumeService volumeService,
                             IIssueService issueService,
                             IParsingService parsingService,
                             ICacheManager cacheManager,
                             Logger logger)
        {
            _rssFetcherAndParser = rssFetcherAndParser;
            _releaseSearchService = releaseSearchService;
            _downloadDecisionMaker = downloadDecisionMaker;
            _prioritizeDownloadDecision = prioritizeDownloadDecision;
            _downloadService = downloadService;
            _volumeService = volumeService;
            _issueService = issueService;
            _parsingService = parsingService;
            _logger = logger;

            PostValidator.RuleFor(s => s.IndexerId).ValidId();
            PostValidator.RuleFor(s => s.Guid).NotEmpty();

            _remoteIssueCache = cacheManager.GetCache<RemoteIssue>(GetType(), "remoteIssues");
        }

        [HttpPost]
        public async Task<ActionResult<ReleaseResource>> DownloadRelease(ReleaseResource release)
        {
            ValidateResource(release);

            var remoteIssue = _remoteIssueCache.Find(GetCacheKey(release));

            if (remoteIssue == null)
            {
                _logger.Debug("Couldn't find requested release in cache, cache timeout probably expired.");

                throw new NzbDroneClientException(HttpStatusCode.NotFound, "Couldn't find requested release in cache, try searching again");
            }

            try
            {
                if (remoteIssue.Volume == null)
                {
                    if (release.IssueId.HasValue)
                    {
                        var issue = _issueService.GetIssue(release.IssueId.Value);

                        remoteIssue.Volume = _volumeService.GetVolume(issue.VolumeId);
                        remoteIssue.Issues = new List<Issue> { issue };
                    }
                    else if (release.VolumeId.HasValue)
                    {
                        var volume = _volumeService.GetVolume(release.VolumeId.Value);
                        var issues = _parsingService.GetIssues(remoteIssue.ParsedIssueInfo, volume);

                        if (issues.Empty())
                        {
                            throw new NzbDroneClientException(HttpStatusCode.NotFound, "Unable to parse issues in the release");
                        }

                        remoteIssue.Volume = volume;
                        remoteIssue.Issues = issues;
                    }
                    else
                    {
                        throw new NzbDroneClientException(HttpStatusCode.NotFound, "Unable to find matching volume and issues");
                    }
                }
                else if (remoteIssue.Issues.Empty())
                {
                    var issues = _parsingService.GetIssues(remoteIssue.ParsedIssueInfo, remoteIssue.Volume);

                    if (issues.Empty() && release.IssueId.HasValue)
                    {
                        var issue = _issueService.GetIssue(release.IssueId.Value);

                        issues = new List<Issue> { issue };
                    }

                    remoteIssue.Issues = issues;
                }

                if (remoteIssue.Issues.Empty())
                {
                    throw new NzbDroneClientException(HttpStatusCode.NotFound, "Unable to parse issues in the release");
                }

                await _downloadService.DownloadReport(remoteIssue, release.DownloadClientId);
            }
            catch (ReleaseDownloadException ex)
            {
                _logger.Error(ex, "Getting release from indexer failed");
                throw new NzbDroneClientException(HttpStatusCode.Conflict, "Getting release from indexer failed");
            }

            return Ok(release);
        }

        [HttpGet]
        public async Task<List<ReleaseResource>> GetReleases(int? issueId, int? volumeId)
        {
            if (issueId.HasValue)
            {
                return await GetIssueReleases(int.Parse(Request.Query["issueId"]));
            }

            if (volumeId.HasValue)
            {
                return await GetVolumeReleases(int.Parse(Request.Query["volumeId"]));
            }

            return await GetRss();
        }

        private async Task<List<ReleaseResource>> GetIssueReleases(int issueId)
        {
            try
            {
                var decisions = await _releaseSearchService.IssueSearch(issueId, true, true, true);
                var prioritizedDecisions = _prioritizeDownloadDecision.PrioritizeDecisions(decisions);

                return MapDecisions(prioritizedDecisions);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Issue search failed");
                throw new NzbDroneClientException(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        private async Task<List<ReleaseResource>> GetVolumeReleases(int volumeId)
        {
            try
            {
                var decisions = await _releaseSearchService.VolumeSearch(volumeId, false, true, true);
                var prioritizedDecisions = _prioritizeDownloadDecision.PrioritizeDecisions(decisions);

                return MapDecisions(prioritizedDecisions);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Volume search failed");
                throw new NzbDroneClientException(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        private async Task<List<ReleaseResource>> GetRss()
        {
            var reports = await _rssFetcherAndParser.Fetch();
            var decisions = _downloadDecisionMaker.GetRssDecision(reports);
            var prioritizedDecisions = _prioritizeDownloadDecision.PrioritizeDecisions(decisions);

            return MapDecisions(prioritizedDecisions);
        }

        protected override ReleaseResource MapDecision(DownloadDecision decision, int initialWeight)
        {
            var resource = base.MapDecision(decision, initialWeight);
            _remoteIssueCache.Set(GetCacheKey(resource), decision.RemoteIssue, TimeSpan.FromMinutes(30));

            return resource;
        }

        private string GetCacheKey(ReleaseResource resource)
        {
            return string.Concat(resource.IndexerId, "_", resource.Guid);
        }
    }
}
