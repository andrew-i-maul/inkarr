using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.History;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.CustomFormats
{
    public interface ICustomFormatCalculationService
    {
        List<CustomFormat> ParseCustomFormat(RemoteIssue remoteIssue, long size);
        List<CustomFormat> ParseCustomFormat(IssueFile issueFile, Volume artist);
        List<CustomFormat> ParseCustomFormat(IssueFile issueFile);
        List<CustomFormat> ParseCustomFormat(Blocklist blocklist, Volume artist);
        List<CustomFormat> ParseCustomFormat(EntityHistory history, Volume artist);
        List<CustomFormat> ParseCustomFormat(LocalIssue localIssue);
    }

    public class CustomFormatCalculationService : ICustomFormatCalculationService
    {
        private readonly ICustomFormatService _formatService;
        private readonly Logger _logger;

        public CustomFormatCalculationService(ICustomFormatService formatService, Logger logger)
        {
            _formatService = formatService;
            _logger = logger;
        }

        public List<CustomFormat> ParseCustomFormat(RemoteIssue remoteIssue, long size)
        {
            var input = new CustomFormatInput
            {
                IssueInfo = remoteIssue.ParsedIssueInfo,
                Volume = remoteIssue.Volume,
                Size = size,
                IndexerFlags = remoteIssue.Release?.IndexerFlags ?? 0
            };

            return ParseCustomFormat(input);
        }

        public List<CustomFormat> ParseCustomFormat(IssueFile issueFile, Volume volume)
        {
            return ParseCustomFormat(issueFile, volume, _formatService.All());
        }

        public List<CustomFormat> ParseCustomFormat(IssueFile issueFile)
        {
            return ParseCustomFormat(issueFile, issueFile.Volume.Value, _formatService.All());
        }

        public List<CustomFormat> ParseCustomFormat(Blocklist blocklist, Volume volume)
        {
            var parsed = Parser.Parser.ParseIssueTitle(blocklist.SourceTitle);

            var issueInfo = new ParsedIssueInfo
            {
                VolumeName = volume.Name,
                ReleaseTitle = parsed?.ReleaseTitle ?? blocklist.SourceTitle,
                Quality = blocklist.Quality,
                ReleaseGroup = parsed?.ReleaseGroup
            };

            var input = new CustomFormatInput
            {
                IssueInfo = issueInfo,
                Volume = volume,
                Size = blocklist.Size ?? 0,
                IndexerFlags = blocklist.IndexerFlags
            };

            return ParseCustomFormat(input);
        }

        public List<CustomFormat> ParseCustomFormat(EntityHistory history, Volume volume)
        {
            var parsed = Parser.Parser.ParseIssueTitle(history.SourceTitle);

            long.TryParse(history.Data.GetValueOrDefault("size"), out var size);
            Enum.TryParse(history.Data.GetValueOrDefault("indexerFlags"), true, out IndexerFlags indexerFlags);

            var issueInfo = new ParsedIssueInfo
            {
                VolumeName = volume.Name,
                ReleaseTitle = parsed?.ReleaseTitle ?? history.SourceTitle,
                Quality = history.Quality,
                ReleaseGroup = parsed?.ReleaseGroup,
            };

            var input = new CustomFormatInput
            {
                IssueInfo = issueInfo,
                Volume = volume,
                Size = size,
                IndexerFlags = indexerFlags
            };

            return ParseCustomFormat(input);
        }

        public List<CustomFormat> ParseCustomFormat(LocalIssue localIssue)
        {
            var issueInfo = new ParsedIssueInfo
            {
                VolumeName = localIssue.Volume.Name,
                ReleaseTitle = localIssue.SceneName,
                Quality = localIssue.Quality,
                ReleaseGroup = localIssue.ReleaseGroup
            };

            var input = new CustomFormatInput
            {
                IssueInfo = issueInfo,
                Volume = localIssue.Volume,
                Size = localIssue.Size,
                IndexerFlags = localIssue.IndexerFlags,
            };

            return ParseCustomFormat(input);
        }

        private List<CustomFormat> ParseCustomFormat(CustomFormatInput input)
        {
            return ParseCustomFormat(input, _formatService.All());
        }

        private static List<CustomFormat> ParseCustomFormat(CustomFormatInput input, List<CustomFormat> allCustomFormats)
        {
            var matches = new List<CustomFormat>();

            foreach (var customFormat in allCustomFormats)
            {
                var specificationMatches = customFormat.Specifications
                    .GroupBy(t => t.GetType())
                    .Select(g => new SpecificationMatchesGroup
                    {
                        Matches = g.ToDictionary(t => t, t => t.IsSatisfiedBy(input))
                    })
                    .ToList();

                if (specificationMatches.All(x => x.DidMatch))
                {
                    matches.Add(customFormat);
                }
            }

            return matches.OrderBy(x => x.Name).ToList();
        }

        private List<CustomFormat> ParseCustomFormat(IssueFile issueFile, Volume volume, List<CustomFormat> allCustomFormats)
        {
            var releaseTitle = string.Empty;

            if (issueFile.SceneName.IsNotNullOrWhiteSpace())
            {
                _logger.Trace("Using scene name for release title: {0}", issueFile.SceneName);
                releaseTitle = issueFile.SceneName;
            }
            else if (issueFile.OriginalFilePath.IsNotNullOrWhiteSpace())
            {
                _logger.Trace("Using original file path for release title: {0}", issueFile.OriginalFilePath);
                releaseTitle = issueFile.OriginalFilePath;
            }
            else if (issueFile.Path.IsNotNullOrWhiteSpace())
            {
                _logger.Trace("Using path for release title: {0}", Path.GetFileName(issueFile.Path));
                releaseTitle = Path.GetFileName(issueFile.Path);
            }

            var issueInfo = new ParsedIssueInfo
            {
                VolumeName = volume.Name,
                ReleaseTitle = releaseTitle,
                Quality = issueFile.Quality,
                ReleaseGroup = issueFile.ReleaseGroup
            };

            var input = new CustomFormatInput
            {
                IssueInfo = issueInfo,
                Volume = volume,
                Size = issueFile.Size,
                IndexerFlags = issueFile.IndexerFlags,
                Filename = Path.GetFileName(issueFile.Path)
            };

            return ParseCustomFormat(input, allCustomFormats);
        }
    }
}
