using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Parser
{
    public interface IParsingService
    {
        Volume GetVolume(string title);
        RemoteIssue Map(ParsedIssueInfo parsedIssueInfo, SearchCriteriaBase searchCriteria = null);
        RemoteIssue Map(ParsedIssueInfo parsedIssueInfo, int volumeId, IEnumerable<int> issueIds);
        List<Issue> GetIssues(ParsedIssueInfo parsedIssueInfo, Volume volume, SearchCriteriaBase searchCriteria = null);

        ParsedIssueInfo ParseIssueTitleFuzzy(string title);

        // Music stuff here
        Issue GetLocalIssue(string filename, Volume volume);
    }

    public class ParsingService : IParsingService
    {
        private readonly IVolumeService _volumeService;
        private readonly IIssueService _issueService;
        private readonly IEditionService _editionService;
        private readonly IMediaFileService _mediaFileService;
        private readonly Logger _logger;

        public ParsingService(IVolumeService volumeService,
                              IIssueService issueService,
                              IEditionService editionService,
                              IMediaFileService mediaFileService,
                              Logger logger)
        {
            _issueService = issueService;
            _editionService = editionService;
            _volumeService = volumeService;
            _mediaFileService = mediaFileService;
            _logger = logger;
        }

        public Volume GetVolume(string title)
        {
            var parsedIssueInfo = Parser.ParseIssueTitle(title);

            if (parsedIssueInfo != null && !parsedIssueInfo.VolumeName.IsNullOrWhiteSpace())
            {
                title = parsedIssueInfo.VolumeName;
            }

            var volumeInfo = _volumeService.FindByName(title);

            if (volumeInfo == null)
            {
                _logger.Debug("Trying inexact volume match for {0}", title);
                volumeInfo = _volumeService.FindByNameInexact(title);
            }

            return volumeInfo;
        }

        public RemoteIssue Map(ParsedIssueInfo parsedIssueInfo, SearchCriteriaBase searchCriteria = null)
        {
            var remoteIssue = new RemoteIssue
            {
                ParsedIssueInfo = parsedIssueInfo,
            };

            var volume = GetVolume(parsedIssueInfo, searchCriteria);

            if (volume == null)
            {
                return remoteIssue;
            }

            remoteIssue.Volume = volume;
            remoteIssue.Issues = GetIssues(parsedIssueInfo, volume, searchCriteria);

            return remoteIssue;
        }

        public List<Issue> GetIssues(ParsedIssueInfo parsedIssueInfo, Volume volume, SearchCriteriaBase searchCriteria = null)
        {
            var issueTitle = parsedIssueInfo.IssueTitle;
            var result = new List<Issue>();

            if (parsedIssueInfo.IssueTitle == null)
            {
                return new List<Issue>();
            }

            Issue issueInfo = null;

            if (parsedIssueInfo.Discography)
            {
                if (parsedIssueInfo.DiscographyStart > 0)
                {
                    return _issueService.VolumeIssuesBetweenDates(volume,
                        new DateTime(parsedIssueInfo.DiscographyStart, 1, 1),
                        new DateTime(parsedIssueInfo.DiscographyEnd, 12, 31),
                        false);
                }

                if (parsedIssueInfo.DiscographyEnd > 0)
                {
                    return _issueService.VolumeIssuesBetweenDates(volume,
                        new DateTime(1800, 1, 1),
                        new DateTime(parsedIssueInfo.DiscographyEnd, 12, 31),
                        false);
                }

                return _issueService.GetIssuesByVolume(volume.Id);
            }

            if (searchCriteria != null)
            {
                var cleanTitle = Parser.CleanVolumeName(parsedIssueInfo.IssueTitle);
                issueInfo = searchCriteria.Issues.ExclusiveOrDefault(e => e.Title == issueTitle || e.CleanTitle == cleanTitle);
            }

            if (issueInfo == null)
            {
                // TODO: Search by Title and Year instead of just Title when matching
                issueInfo = _issueService.FindByTitle(volume.VolumeMetadataId, parsedIssueInfo.IssueTitle);
            }

            if (issueInfo == null)
            {
                var edition = _editionService.FindByTitle(volume.VolumeMetadataId, parsedIssueInfo.IssueTitle);
                issueInfo = edition?.Issue.Value;
            }

            if (issueInfo == null)
            {
                _logger.Debug("Trying inexact issue match for {0}", parsedIssueInfo.IssueTitle);
                issueInfo = _issueService.FindByTitleInexact(volume.VolumeMetadataId, parsedIssueInfo.IssueTitle);
            }

            if (issueInfo == null)
            {
                _logger.Debug("Trying inexact edition match for {0}", parsedIssueInfo.IssueTitle);
                var edition = _editionService.FindByTitleInexact(volume.VolumeMetadataId, parsedIssueInfo.IssueTitle);
                issueInfo = edition?.Issue.Value;
            }

            if (issueInfo != null)
            {
                result.Add(issueInfo);
            }
            else
            {
                _logger.Debug("Unable to find {0}", parsedIssueInfo);
            }

            return result;
        }

        public RemoteIssue Map(ParsedIssueInfo parsedIssueInfo, int volumeId, IEnumerable<int> issueIds)
        {
            return new RemoteIssue
            {
                ParsedIssueInfo = parsedIssueInfo,
                Volume = _volumeService.GetVolume(volumeId),
                Issues = _issueService.GetIssues(issueIds)
            };
        }

        private Volume GetVolume(ParsedIssueInfo parsedIssueInfo, SearchCriteriaBase searchCriteria)
        {
            Volume volume = null;

            if (searchCriteria != null)
            {
                if (searchCriteria.Volume.CleanName == parsedIssueInfo.VolumeName.CleanVolumeName())
                {
                    return searchCriteria.Volume;
                }
            }

            volume = _volumeService.FindByName(parsedIssueInfo.VolumeName);

            if (volume == null)
            {
                _logger.Debug("Trying inexact volume match for {0}", parsedIssueInfo.VolumeName);
                volume = _volumeService.FindByNameInexact(parsedIssueInfo.VolumeName);
            }

            if (volume == null)
            {
                _logger.Debug("No matching volume {0}", parsedIssueInfo.VolumeName);
                return null;
            }

            return volume;
        }

        public ParsedIssueInfo ParseIssueTitleFuzzy(string title)
        {
            var bestScore = 0.0;

            Volume bestVolume = null;
            Issue bestIssue = null;

            var possibleVolumes = _volumeService.GetReportCandidates(title);

            foreach (var volume in possibleVolumes)
            {
                _logger.Trace($"Trying possible volume {volume}");

                var volumeMatch = title.FuzzyMatch(volume.Metadata.Value.Name, 0.5);
                var possibleIssues = _issueService.GetCandidates(volume.VolumeMetadataId, title);

                foreach (var issue in possibleIssues)
                {
                    var issueMatch = title.FuzzyMatch(issue.Title, 0.5);
                    var score = (volumeMatch.Item3 + issueMatch.Item3) / 2;

                    _logger.Trace($"Issue {issue} has score {score}");

                    if (score > bestScore)
                    {
                        bestVolume = volume;
                        bestIssue = issue;
                    }
                }

                var possibleEditions = _editionService.GetCandidates(volume.VolumeMetadataId, title);
                foreach (var edition in possibleEditions)
                {
                    var editionMatch = title.FuzzyMatch(edition.Title, 0.5);
                    var score = (volumeMatch.Item3 + editionMatch.Item3) / 2;

                    _logger.Trace($"Edition {edition} has score {score}");

                    if (score > bestScore)
                    {
                        bestVolume = volume;
                        bestIssue = edition.Issue.Value;
                    }
                }
            }

            _logger.Trace($"Best match: {bestVolume} {bestIssue}");

            if (bestVolume != null)
            {
                return Parser.ParseIssueTitleWithSearchCriteria(title, bestVolume, new List<Issue> { bestIssue });
            }

            return null;
        }

        public Issue GetLocalIssue(string filename, Volume volume)
        {
            if (Path.HasExtension(filename))
            {
                filename = Path.GetDirectoryName(filename);
            }

            var tracksInIssue = _mediaFileService.GetFilesByVolume(volume.Id)
                .FindAll(s => Path.GetDirectoryName(s.Path) == filename)
                .DistinctBy(s => s.EditionId)
                .ToList();

            return tracksInIssue.Count == 1 ? _issueService.GetIssue(tracksInIssue.First().EditionId) : null;
        }
    }
}
