using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Issues.Calibre;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.IssueImport.Aggregation.Aggregators
{
    public class AggregateCalibreData : IAggregate<LocalIssue>
    {
        private readonly Logger _logger;
        private readonly ICached<CalibreIssue> _issueCache;

        public AggregateCalibreData(Logger logger,
                                    ICacheManager cacheManager)
        {
            _logger = logger;
            _issueCache = cacheManager.GetCache<CalibreIssue>(typeof(CalibreProxy));
        }

        public LocalIssue Aggregate(LocalIssue localTrack, bool others)
        {
            var issue = _issueCache.Find(localTrack.Path);
            _logger.Trace($"Searching calibre data for {localTrack.Path}");

            if (issue != null)
            {
                _logger.Trace($"Using calibre data for {localTrack.Path}:\n{issue.ToJson()}");

                localTrack.CalibreId = issue.Id;

                var parsed = localTrack.FileTrackInfo;
                parsed.Asin = issue.Identifiers.GetValueOrDefault("mobi-asin") ?? issue.Identifiers.GetValueOrDefault("asin");
                parsed.Isbn = issue.Identifiers.GetValueOrDefault("isbn");
                parsed.GoodreadsId = issue.Identifiers.GetValueOrDefault("goodreads");
                parsed.Volumes = issue.Volumes;
                parsed.IssueTitle = issue.Title;
            }

            return localTrack;
        }
    }
}
