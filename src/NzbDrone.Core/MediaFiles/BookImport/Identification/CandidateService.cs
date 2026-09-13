using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.MetadataSource.Goodreads;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.IssueImport.Identification
{
    public interface ICandidateService
    {
        List<CandidateEdition> GetDbCandidatesFromTags(LocalEdition localEdition, IdentificationOverrides idOverrides, bool includeExisting);
        IEnumerable<CandidateEdition> GetRemoteCandidates(LocalEdition localEdition, IdentificationOverrides idOverrides);
    }

    public class CandidateService : ICandidateService
    {
        private readonly ISearchForNewIssue _issueSearchService;
        private readonly IVolumeService _volumeService;
        private readonly IIssueService _issueService;
        private readonly IEditionService _editionService;
        private readonly IMediaFileService _mediaFileService;
        private readonly Logger _logger;

        public CandidateService(ISearchForNewIssue issueSearchService,
                                IVolumeService volumeService,
                                IIssueService issueService,
                                IEditionService editionService,
                                IMediaFileService mediaFileService,
                                Logger logger)
        {
            _issueSearchService = issueSearchService;
            _volumeService = volumeService;
            _issueService = issueService;
            _editionService = editionService;
            _mediaFileService = mediaFileService;
            _logger = logger;
        }

        public List<CandidateEdition> GetDbCandidatesFromTags(LocalEdition localEdition, IdentificationOverrides idOverrides, bool includeExisting)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            // Generally volume, issue and release are null.  But if they're not then limit candidates appropriately.
            // We've tried to make sure that tracks are all for a single release.
            List<CandidateEdition> candidateReleases;

            // if we have a Issue ID, use that
            Issue tagMbidRelease = null;
            List<CandidateEdition> tagCandidate = null;

            // TODO: select by ISBN?
            // var releaseIds = localEdition.LocalTracks.Select(x => x.FileTrackInfo.ReleaseMBId).Distinct().ToList();
            // if (releaseIds.Count == 1 && releaseIds[0].IsNotNullOrWhiteSpace())
            // {
            //     _logger.Debug("Selecting release from consensus ForeignReleaseId [{0}]", releaseIds[0]);
            //     tagMbidRelease = _releaseService.GetReleaseByForeignReleaseId(releaseIds[0], true);

            //     if (tagMbidRelease != null)
            //     {
            //         tagCandidate = GetDbCandidatesByRelease(new List<IssueRelease> { tagMbidRelease }, includeExisting);
            //     }
            // }
            if (idOverrides?.Edition != null)
            {
                var release = idOverrides.Edition;
                _logger.Debug("Edition {0} was forced", release);
                candidateReleases = GetDbCandidatesByEdition(new List<Edition> { release }, includeExisting);
            }
            else if (idOverrides?.Issue != null)
            {
                // use the release from file tags if it exists and agrees with the specified issue
                if (tagMbidRelease?.Id == idOverrides.Issue.Id)
                {
                    candidateReleases = tagCandidate;
                }
                else
                {
                    candidateReleases = GetDbCandidatesByIssue(idOverrides.Issue, includeExisting);
                }
            }
            else if (idOverrides?.Volume != null)
            {
                // use the release from file tags if it exists and agrees with the specified issue
                if (tagMbidRelease?.VolumeMetadataId == idOverrides.Volume.VolumeMetadataId)
                {
                    candidateReleases = tagCandidate;
                }
                else
                {
                    candidateReleases = GetDbCandidatesByVolume(localEdition, idOverrides.Volume, includeExisting);
                }
            }
            else
            {
                if (tagMbidRelease != null)
                {
                    candidateReleases = tagCandidate;
                }
                else
                {
                    candidateReleases = GetDbCandidates(localEdition, includeExisting);
                }
            }

            watch.Stop();
            _logger.Debug($"Getting {candidateReleases.Count} candidates from tags for {localEdition.LocalIssues.Count} tracks took {watch.ElapsedMilliseconds}ms");

            return candidateReleases;
        }

        private List<CandidateEdition> GetDbCandidatesByEdition(List<Edition> editions, bool includeExisting)
        {
            // get the local tracks on disk for each issue
            var issueFiles = editions.Select(x => x.IssueId)
                .Distinct()
                .ToDictionary(id => id, id => includeExisting ? _mediaFileService.GetFilesByIssue(id) : new List<IssueFile>());

            return editions.Select(x => new CandidateEdition
            {
                Edition = x,
                ExistingFiles = issueFiles[x.IssueId]
            }).ToList();
        }

        private List<CandidateEdition> GetDbCandidatesByIssue(Issue issue, bool includeExisting)
        {
            // Sort by most voted so less likely to swap to a random release
            return GetDbCandidatesByEdition(_editionService.GetEditionsByIssue(issue.Id)
                                            .OrderByDescending(x => x.Ratings.Popularity)
                                            .ToList(), includeExisting);
        }

        private List<CandidateEdition> GetDbCandidatesByVolume(LocalEdition localEdition, Volume volume, bool includeExisting)
        {
            _logger.Trace("Getting candidates for {0}", volume);
            var candidateReleases = new List<CandidateEdition>();

            var issueTag = localEdition.LocalIssues.MostCommon(x => x.FileTrackInfo.IssueTitle) ?? "";
            if (issueTag.IsNotNullOrWhiteSpace())
            {
                var possibleIssues = _issueService.GetCandidates(volume.VolumeMetadataId, issueTag);
                foreach (var issue in possibleIssues)
                {
                    candidateReleases.AddRange(GetDbCandidatesByIssue(issue, includeExisting));
                }

                var possibleEditions = _editionService.GetCandidates(volume.VolumeMetadataId, issueTag);
                candidateReleases.AddRange(GetDbCandidatesByEdition(possibleEditions, includeExisting));
            }

            return candidateReleases;
        }

        private List<CandidateEdition> GetDbCandidates(LocalEdition localEdition, bool includeExisting)
        {
            // most general version, nothing has been specified.
            // get all plausible volumes, then all plausible issues, then get releases for each of these.
            var candidateReleases = new List<CandidateEdition>();

            // check if it looks like VA.
            if (TrackGroupingService.IsVariousVolumes(localEdition.LocalIssues))
            {
                var va = _volumeService.FindById(DistanceCalculator.VariousVolumeIds[0]);
                if (va != null)
                {
                    candidateReleases.AddRange(GetDbCandidatesByVolume(localEdition, va, includeExisting));
                }
            }

            var volumeTags = localEdition.LocalIssues.MostCommon(x => x.FileTrackInfo.Volumes) ?? new List<string>();
            if (volumeTags.Any())
            {
                var variants = DistanceCalculator.GetVolumeVariants(volumeTags.Where(x => x.IsNotNullOrWhiteSpace()).ToList());

                foreach (var volumeTag in variants)
                {
                    if (volumeTag.IsNotNullOrWhiteSpace())
                    {
                        var possibleVolumes = _volumeService.GetCandidates(volumeTag);
                        foreach (var volume in possibleVolumes)
                        {
                            candidateReleases.AddRange(GetDbCandidatesByVolume(localEdition, volume, includeExisting));
                        }
                    }
                }
            }

            return candidateReleases;
        }

        public IEnumerable<CandidateEdition> GetRemoteCandidates(LocalEdition localEdition, IdentificationOverrides idOverrides)
        {
            // TODO handle edition override

            // Gets candidate issue releases from the metadata server.
            // Will eventually need adding locally if we find a match
            List<Issue> remoteIssues;
            var seenCandidates = new HashSet<string>();

            var isbns = localEdition.LocalIssues.Select(x => x.FileTrackInfo.Isbn).Distinct().ToList();
            var asins = localEdition.LocalIssues.Select(x => x.FileTrackInfo.Asin).Distinct().ToList();
            var goodreads = localEdition.LocalIssues.Select(x => x.FileTrackInfo.GoodreadsId).Distinct().ToList();

            // grab possibilities for all the IDs present
            if (isbns.Count == 1 && isbns[0].IsNotNullOrWhiteSpace())
            {
                _logger.Trace($"Searching by isbn {isbns[0]}");

                try
                {
                    remoteIssues = _issueSearchService.SearchByIsbn(isbns[0]);
                }
                catch (GoodreadsException e)
                {
                    _logger.Info(e, "Skipping ISBN search due to Goodreads Error");
                    remoteIssues = new List<Issue>();
                }

                foreach (var candidate in ToCandidates(remoteIssues, seenCandidates, idOverrides))
                {
                    yield return candidate;
                }
            }

            if (asins.Count == 1 &&
                asins[0].IsNotNullOrWhiteSpace() &&
                asins[0].Length == 10)
            {
                _logger.Trace($"Searching by asin {asins[0]}");

                try
                {
                    remoteIssues = _issueSearchService.SearchByAsin(asins[0]);
                }
                catch (GoodreadsException e)
                {
                    _logger.Info(e, "Skipping ASIN search due to Goodreads Error");
                    remoteIssues = new List<Issue>();
                }

                foreach (var candidate in ToCandidates(remoteIssues, seenCandidates, idOverrides))
                {
                    yield return candidate;
                }
            }

            if (goodreads.Count == 1 &&
                goodreads[0].IsNotNullOrWhiteSpace())
            {
                if (int.TryParse(goodreads[0], out var id))
                {
                    _logger.Trace($"Searching by goodreads id {id}");

                    try
                    {
                        remoteIssues = _issueSearchService.SearchByGoodreadsIssueId(id, true);
                    }
                    catch (GoodreadsException e)
                    {
                        _logger.Info(e, "Skipping Goodreads ID search due to Goodreads Error");
                        remoteIssues = new List<Issue>();
                    }

                    foreach (var candidate in ToCandidates(remoteIssues, seenCandidates, idOverrides))
                    {
                        yield return candidate;
                    }
                }
            }

            // If we got an id result, or any overrides are set, stop
            if (seenCandidates.Any() ||
                idOverrides?.Edition != null ||
                idOverrides?.Issue != null ||
                idOverrides?.Volume != null)
            {
                yield break;
            }

            // fall back to volume / issue name search
            var volumeTags = new List<string>();

            if (TrackGroupingService.IsVariousVolumes(localEdition.LocalIssues))
            {
                volumeTags.Add("Various Volumes");
            }
            else
            {
                // the most common list of volumes reported by a file
                var volumes = localEdition.LocalIssues.Select(x => x.FileTrackInfo.Volumes.Where(a => a.IsNotNullOrWhiteSpace()).ToList())
                    .GroupBy(x => x.ConcatToString())
                    .OrderByDescending(x => x.Count())
                    .First()
                    .First();
                volumeTags.AddRange(volumes);
            }

            var issueTag = localEdition.LocalIssues.MostCommon(x => x.FileTrackInfo.IssueTitle) ?? "";

            // If no valid volume or issue tags, stop
            if (!volumeTags.Any() || issueTag.IsNullOrWhiteSpace())
            {
                yield break;
            }

            // Search by volume+issue
            foreach (var volumeTag in volumeTags)
            {
                try
                {
                    remoteIssues = _issueSearchService.SearchForNewIssue(issueTag, volumeTag);
                }
                catch (GoodreadsException e)
                {
                    _logger.Info(e, "Skipping volume/title search due to Goodreads Error");
                    remoteIssues = new List<Issue>();
                }

                foreach (var candidate in ToCandidates(remoteIssues, seenCandidates, idOverrides))
                {
                    yield return candidate;
                }
            }

            // If we got an volume/issue search result, stop
            if (seenCandidates.Any())
            {
                yield break;
            }

            // Search by just issue title
            try
            {
                remoteIssues = _issueSearchService.SearchForNewIssue(issueTag, null);
            }
            catch (GoodreadsException e)
            {
                _logger.Info(e, "Skipping issue title search due to Goodreads Error");
                remoteIssues = new List<Issue>();
            }

            foreach (var candidate in ToCandidates(remoteIssues, seenCandidates, idOverrides))
            {
                yield return candidate;
            }

            // Search by just volume
            foreach (var a in volumeTags)
            {
                try
                {
                    remoteIssues = _issueSearchService.SearchForNewIssue(a, null);
                }
                catch (GoodreadsException e)
                {
                    _logger.Info(e, "Skipping volume search due to Goodreads Error");
                    remoteIssues = new List<Issue>();
                }

                foreach (var candidate in ToCandidates(remoteIssues, seenCandidates, idOverrides))
                {
                    yield return candidate;
                }
            }
        }

        private List<CandidateEdition> ToCandidates(IEnumerable<Issue> issues, HashSet<string> seenCandidates, IdentificationOverrides idOverrides)
        {
            var candidates = new List<CandidateEdition>();

            foreach (var issue in issues)
            {
                // We have to make sure various bits and pieces are populated that are normally handled
                // by a database lazy load
                foreach (var edition in issue.Editions.Value)
                {
                    edition.Issue = issue;

                    if (!seenCandidates.Contains(edition.ForeignEditionId) && SatisfiesOverride(edition, idOverrides))
                    {
                        seenCandidates.Add(edition.ForeignEditionId);
                        candidates.Add(new CandidateEdition
                        {
                            Edition = edition,
                            ExistingFiles = new List<IssueFile>()
                        });
                    }
                }
            }

            return candidates;
        }

        private bool SatisfiesOverride(Edition edition, IdentificationOverrides idOverride)
        {
            if (idOverride?.Edition != null)
            {
                return edition.ForeignEditionId == idOverride.Edition.ForeignEditionId;
            }

            if (idOverride?.Issue != null)
            {
                return edition.Issue.Value.ForeignIssueId == idOverride.Issue.ForeignIssueId;
            }

            if (idOverride?.Volume != null)
            {
                return edition.Issue.Value.Volume.Value.ForeignVolumeId == idOverride.Volume.ForeignVolumeId;
            }

            return true;
        }
    }
}
