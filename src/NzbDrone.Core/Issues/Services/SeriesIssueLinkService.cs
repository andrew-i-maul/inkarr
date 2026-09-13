using System.Collections.Generic;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Issues
{
    public interface ISeriesIssueLinkService
    {
        List<SeriesIssueLink> GetLinksBySeries(int seriesId);
        List<SeriesIssueLink> GetLinksBySeriesAndVolume(int seriesId, string foreignVolumeId);
        List<SeriesIssueLink> GetLinksByIssue(List<int> issueIds);
        void InsertMany(List<SeriesIssueLink> model);
        void UpdateMany(List<SeriesIssueLink> model);
        void DeleteMany(List<SeriesIssueLink> model);
    }

    public class SeriesIssueLinkService : ISeriesIssueLinkService,
        IHandle<IssueDeletedEvent>
    {
        private readonly ISeriesIssueLinkRepository _repo;

        public SeriesIssueLinkService(ISeriesIssueLinkRepository repo)
        {
            _repo = repo;
        }

        public List<SeriesIssueLink> GetLinksBySeries(int seriesId)
        {
            return _repo.GetLinksBySeries(seriesId);
        }

        public List<SeriesIssueLink> GetLinksBySeriesAndVolume(int seriesId, string foreignVolumeId)
        {
            return _repo.GetLinksBySeriesAndVolume(seriesId, foreignVolumeId);
        }

        public List<SeriesIssueLink> GetLinksByIssue(List<int> issueIds)
        {
            return _repo.GetLinksByIssue(issueIds);
        }

        public void InsertMany(List<SeriesIssueLink> model)
        {
            _repo.InsertMany(model);
        }

        public void UpdateMany(List<SeriesIssueLink> model)
        {
            _repo.UpdateMany(model);
        }

        public void DeleteMany(List<SeriesIssueLink> model)
        {
            _repo.DeleteMany(model);
        }

        public void Handle(IssueDeletedEvent message)
        {
            var links = GetLinksByIssue(new List<int> { message.Issue.Id });
            DeleteMany(links);
        }
    }
}
