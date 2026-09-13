using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Calibre;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Profiles.Releases;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.Profiles.Metadata
{
    public interface IMetadataProfileService
    {
        MetadataProfile Add(MetadataProfile profile);
        void Update(MetadataProfile profile);
        void Delete(int id);
        List<MetadataProfile> All();
        MetadataProfile Get(int id);
        bool Exists(int id);
        List<Issue> FilterIssues(Volume input, int profileId);
    }

    public class MetadataProfileService : IMetadataProfileService, IHandle<ApplicationStartedEvent>
    {
        public const string NONE_PROFILE_NAME = "None";
        public const double NONE_PROFILE_MIN_POPULARITY = 1e10;

        private static readonly Regex PartOrSetRegex = new Regex(@"(?<from>\d+) of (?<to>\d+)|(?<from>\d+)\s?/\s?(?<to>\d+)|(?<from>\d+)\s?-\s?(?<to>\d+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly IMetadataProfileRepository _profileRepository;
        private readonly IVolumeService _volumeService;
        private readonly IIssueService _issueService;
        private readonly IEditionService _editionService;
        private readonly IMediaFileService _mediaFileService;
        private readonly IImportListFactory _importListFactory;
        private readonly IRootFolderService _rootFolderService;
        private readonly ITermMatcherService _termMatcherService;
        private readonly Logger _logger;

        public MetadataProfileService(IMetadataProfileRepository profileRepository,
                                      IVolumeService volumeService,
                                      IIssueService issueService,
                                      IEditionService editionService,
                                      IMediaFileService mediaFileService,
                                      IImportListFactory importListFactory,
                                      IRootFolderService rootFolderService,
                                      ITermMatcherService termMatcherService,
                                      Logger logger)
        {
            _profileRepository = profileRepository;
            _volumeService = volumeService;
            _issueService = issueService;
            _editionService = editionService;
            _mediaFileService = mediaFileService;
            _importListFactory = importListFactory;
            _rootFolderService = rootFolderService;
            _termMatcherService = termMatcherService;
            _logger = logger;
        }

        public MetadataProfile Add(MetadataProfile profile)
        {
            return _profileRepository.Insert(profile);
        }

        public void Update(MetadataProfile profile)
        {
            if (profile.Name == NONE_PROFILE_NAME)
            {
                throw new InvalidOperationException("Not permitted to alter None metadata profile");
            }

            _profileRepository.Update(profile);
        }

        public void Delete(int id)
        {
            var profile = _profileRepository.Get(id);

            if (profile.Name == NONE_PROFILE_NAME ||
                _volumeService.GetAllVolumes().Any(c => c.MetadataProfileId == id) ||
                _importListFactory.All().Any(c => c.MetadataProfileId == id) ||
                _rootFolderService.All().Any(c => c.DefaultMetadataProfileId == id))
            {
                throw new MetadataProfileInUseException(profile.Name);
            }

            _profileRepository.Delete(id);
        }

        public List<MetadataProfile> All()
        {
            return _profileRepository.All().ToList();
        }

        public MetadataProfile Get(int id)
        {
            return _profileRepository.Get(id);
        }

        public bool Exists(int id)
        {
            return _profileRepository.Exists(id);
        }

        public List<Issue> FilterIssues(Volume input, int profileId)
        {
            var seriesLinks = input.Series.Value.SelectMany(x => x.LinkItems.Value)
                .GroupBy(x => x.Issue.Value)
                .ToDictionary(x => x.Key, y => y.ToList());

            var dbVolume = _volumeService.FindById(input.ForeignVolumeId);

            var localIssues = new List<Issue>();
            if (dbVolume != null)
            {
                localIssues = _issueService.GetIssuesByVolumeMetadataId(dbVolume.VolumeMetadataId);
                var editions = _editionService.GetEditionsByVolume(dbVolume.Id).GroupBy(x => x.IssueId).ToDictionary(x => x.Key, y => y.ToList());

                foreach (var issue in localIssues)
                {
                    if (editions.TryGetValue(issue.Id, out var issueEditions))
                    {
                        issue.Editions = issueEditions;
                    }
                    else
                    {
                        issue.Editions = new List<Edition>();
                    }
                }
            }

            var localFiles = _mediaFileService.GetFilesByVolume(dbVolume?.Id ?? 0);

            return FilterIssues(input.Issues.Value, localIssues, localFiles, seriesLinks, profileId);
        }

        private List<Issue> FilterIssues(IEnumerable<Issue> remoteIssues, List<Issue> localIssues, List<IssueFile> localFiles, Dictionary<Issue, List<SeriesIssueLink>> seriesLinks, int metadataProfileId)
        {
            var profile = Get(metadataProfileId);

            _logger.Trace($"Filtering:\n{remoteIssues.Select(x => x.ToString()).Join("\n")}");

            var hash = new HashSet<Issue>(remoteIssues);
            var titles = new HashSet<string>(remoteIssues.Select(x => x.Title));

            var localHash = new HashSet<string>(localIssues.Where(x => x.AddOptions.AddType == IssueAddType.Manual).Select(x => x.ForeignIssueId));
            localHash.UnionWith(localFiles.Select(x => x.Edition.Value.Issue.Value.ForeignIssueId));

            FilterByPredicate(hash, x => x.ForeignIssueId, localHash, profile, IssueAllowedByRating, "rating criteria not met");
            FilterByPredicate(hash, x => x.ForeignIssueId, localHash, profile, (x, p) => !p.SkipMissingDate || x.ReleaseDate.HasValue, "release date is missing");
            FilterByPredicate(hash, x => x.ForeignIssueId, localHash, profile, (x, p) => !p.SkipPartsAndSets || !IsPartOrSet(x, seriesLinks.GetValueOrDefault(x), titles), "issue is part of set");
            FilterByPredicate(hash, x => x.ForeignIssueId, localHash, profile, (x, p) => !p.SkipSeriesSecondary || !seriesLinks.ContainsKey(x) || seriesLinks[x].Any(y => y.IsPrimary), "issue is a secondary series item");
            FilterByPredicate(hash, x => x.ForeignIssueId, localHash, profile, (x, p) => !p.Ignored.Any(i => MatchesTerms(x.Title, i)), "contains ignored terms");

            foreach (var issue in hash)
            {
                var localEditions = localIssues.SingleOrDefault(x => x.ForeignIssueId == issue.ForeignIssueId)?.Editions.Value ?? new List<Edition>();

                issue.Editions = FilterEditions(issue.Editions.Value, localEditions, localFiles, profile);
            }

            FilterByPredicate(hash, x => x.ForeignIssueId, localHash, profile, (x, p) => x.Editions.Value.Any(e => e.PageCount > p.MinPages) || x.Editions.Value.All(e => e.PageCount == 0), "minimum page count not met");
            FilterByPredicate(hash, x => x.ForeignIssueId, localHash, profile, (x, p) => x.Editions.Value.Any(), "all editions filtered out");

            return hash.ToList();
        }

        private List<Edition> FilterEditions(IEnumerable<Edition> editions, List<Edition> localEditions, List<IssueFile> localFiles, MetadataProfile profile)
        {
            var allowedLanguages = profile.AllowedLanguages.IsNotNullOrWhiteSpace() ? new HashSet<string>(profile.AllowedLanguages.Trim(',').Split(',').Select(x => x.CanonicalizeLanguage())) : new HashSet<string>();

            var hash = new HashSet<Edition>(editions);

            var localHash = new HashSet<string>(localEditions.Where(x => x.ManualAdd).Select(x => x.ForeignEditionId));
            localHash.UnionWith(localFiles.Select(x => x.Edition.Value.ForeignEditionId));

            FilterByPredicate(hash, x => x.ForeignEditionId, localHash, profile, (x, p) => !allowedLanguages.Any() || allowedLanguages.Contains(x.Language?.CanonicalizeLanguage()), "edition language not allowed");
            FilterByPredicate(hash, x => x.ForeignEditionId, localHash, profile, (x, p) => !p.SkipMissingIsbn || x.Isbn13.IsNotNullOrWhiteSpace() || x.Asin.IsNotNullOrWhiteSpace(), "isbn and asin is missing");
            FilterByPredicate(hash, x => x.ForeignEditionId, localHash, profile, (x, p) => !p.Ignored.Any(i => MatchesTerms(x.Title, i)), "contains ignored terms");

            return hash.ToList();
        }

        private void FilterByPredicate<T>(HashSet<T> remoteItems, Func<T, string> getId, HashSet<string> localItems, MetadataProfile profile, Func<T, MetadataProfile, bool> issueAllowed, string message)
        {
            var filtered = new HashSet<T>(remoteItems.Where(x => !issueAllowed(x, profile) && !localItems.Contains(getId(x))));
            if (filtered.Any())
            {
                _logger.Trace($"Skipping {filtered.Count} {typeof(T).Name} because {message}:\n{filtered.ConcatToString(x => x.ToString(), "\n")}");
                remoteItems.RemoveWhere(x => filtered.Contains(x));
            }
        }

        private bool IssueAllowedByRating(Issue b, MetadataProfile p)
        {
            // hack for the 'none' metadata profile
            if (p.MinPopularity == NONE_PROFILE_MIN_POPULARITY)
            {
                return false;
            }

            return (b.Ratings.Popularity >= p.MinPopularity) || b.ReleaseDate > DateTime.UtcNow;
        }

        private bool IsPartOrSet(Issue issue, List<SeriesIssueLink> seriesLinks, HashSet<string> titles)
        {
            if (seriesLinks != null &&
                seriesLinks.Any(x => x.Position.IsNotNullOrWhiteSpace()) &&
                !seriesLinks.Any(s => double.TryParse(s.Position, out _)))
            {
                // No non-empty series entries parse to a number, so all like 1-3 etc.
                return true;
            }

            // Skip things of form Title1 / Title2 when Title1 and Title2 are already in the list
            var issueTitles = new[] { issue.Title }.Concat(issue.Editions.Value.Select(x => x.Title)).ToList();
            foreach (var title in issueTitles)
            {
                var split = title.Split('/').Select(x => x.Trim()).ToList();
                if (split.Count > 1 && split.All(x => titles.Contains(x)))
                {
                    return true;
                }
            }

            var match = PartOrSetRegex.Match(issue.Title);

            if (match.Groups["from"].Success)
            {
                var from = int.Parse(match.Groups["from"].Value);
                return from <= 1800 || from > DateTime.UtcNow.Year;
            }

            return false;
        }

        private bool MatchesTerms(string value, string terms)
        {
            if (terms.IsNullOrWhiteSpace() || value.IsNullOrWhiteSpace())
            {
                return false;
            }

            var split = terms.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var foundTerms = ContainsAny(split, value);

            return foundTerms.Any();
        }

        private List<string> ContainsAny(List<string> terms, string title)
        {
            return terms.Where(t => _termMatcherService.IsMatch(t, title)).ToList();
        }

        public void Handle(ApplicationStartedEvent message)
        {
            var profiles = All();

            // Name is a unique property
            var emptyProfile = profiles.FirstOrDefault(x => x.Name == NONE_PROFILE_NAME);

            // make sure empty profile exists and is actually empty
            // TODO: reinstate
            if (emptyProfile != null &&
                emptyProfile.MinPopularity == NONE_PROFILE_MIN_POPULARITY)
            {
                return;
            }

            if (!profiles.Any())
            {
                _logger.Info("Setting up standard metadata profile");

                Add(new MetadataProfile
                {
                    Name = "Standard",
                    MinPopularity = 350,
                    SkipMissingDate = true,
                    SkipPartsAndSets = true,
                    AllowedLanguages = "eng, null"
                });
            }

            if (emptyProfile != null)
            {
                // emptyProfile is not the correct empty profile - move it out of the way
                _logger.Info($"Renaming non-empty metadata profile {emptyProfile.Name}");

                var names = profiles.Select(x => x.Name).ToList();

                var i = 1;
                emptyProfile.Name = $"{NONE_PROFILE_NAME}.{i}";

                while (names.Contains(emptyProfile.Name))
                {
                    i++;
                }

                _profileRepository.Update(emptyProfile);
            }

            _logger.Info("Setting up empty metadata profile");

            Add(new MetadataProfile
            {
                Name = NONE_PROFILE_NAME,
                MinPopularity = NONE_PROFILE_MIN_POPULARITY
            });
        }
    }
}
