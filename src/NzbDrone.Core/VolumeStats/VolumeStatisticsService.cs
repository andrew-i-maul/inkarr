using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Cache;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.VolumeStats
{
    public interface IVolumeStatisticsService
    {
        List<VolumeStatistics> VolumeStatistics();
        VolumeStatistics VolumeStatistics(int volumeId);
    }

    public class VolumeStatisticsService : IVolumeStatisticsService,
        IHandle<VolumeAddedEvent>,
        IHandle<VolumeUpdatedEvent>,
        IHandle<VolumeDeletedEvent>,
        IHandle<IssueAddedEvent>,
        IHandle<IssueDeletedEvent>,
        IHandle<IssueImportedEvent>,
        IHandle<IssueEditedEvent>,
        IHandle<IssueUpdatedEvent>,
        IHandle<IssueFileDeletedEvent>
    {
        private readonly IVolumeStatisticsRepository _volumeStatisticsRepository;
        private readonly ICached<List<IssueStatistics>> _cache;

        public VolumeStatisticsService(IVolumeStatisticsRepository volumeStatisticsRepository,
                                       ICacheManager cacheManager)
        {
            _volumeStatisticsRepository = volumeStatisticsRepository;
            _cache = cacheManager.GetCache<List<IssueStatistics>>(GetType());
        }

        public List<VolumeStatistics> VolumeStatistics()
        {
            var issueStatistics = _cache.Get("AllVolumes", () => _volumeStatisticsRepository.VolumeStatistics());

            return issueStatistics.GroupBy(s => s.VolumeId).Select(s => MapVolumeStatistics(s.ToList())).ToList();
        }

        public VolumeStatistics VolumeStatistics(int volumeId)
        {
            var stats = _cache.Get(volumeId.ToString(), () => _volumeStatisticsRepository.VolumeStatistics(volumeId));

            if (stats == null || stats.Count == 0)
            {
                return new VolumeStatistics();
            }

            return MapVolumeStatistics(stats);
        }

        private VolumeStatistics MapVolumeStatistics(List<IssueStatistics> issueStatistics)
        {
            var volumeStatistics = new VolumeStatistics
            {
                VolumeId = issueStatistics.First().VolumeId,
                IssueFileCount = issueStatistics.Sum(s => s.IssueFileCount),
                IssueCount = issueStatistics.Sum(s => s.IssueCount),
                AvailableIssueCount = issueStatistics.Sum(s => s.AvailableIssueCount),
                TotalIssueCount = issueStatistics.Sum(s => s.TotalIssueCount),
                SizeOnDisk = issueStatistics.Sum(s => s.SizeOnDisk),
                IssueStatistics = issueStatistics
            };

            return volumeStatistics;
        }

        [EventHandleOrder(EventHandleOrder.First)]
        public void Handle(VolumeAddedEvent message)
        {
            _cache.Remove("AllVolumes");
            _cache.Remove(message.Volume.Id.ToString());
        }

        [EventHandleOrder(EventHandleOrder.First)]
        public void Handle(VolumeUpdatedEvent message)
        {
            _cache.Remove("AllVolumes");
            _cache.Remove(message.Volume.Id.ToString());
        }

        [EventHandleOrder(EventHandleOrder.First)]
        public void Handle(VolumeDeletedEvent message)
        {
            _cache.Remove("AllVolumes");
            _cache.Remove(message.Volume.Id.ToString());
        }

        [EventHandleOrder(EventHandleOrder.First)]
        public void Handle(IssueAddedEvent message)
        {
            _cache.Remove("AllVolumes");
            _cache.Remove(message.Issue.VolumeId.ToString());
        }

        [EventHandleOrder(EventHandleOrder.First)]
        public void Handle(IssueDeletedEvent message)
        {
            _cache.Remove("AllVolumes");
            _cache.Remove(message.Issue.VolumeId.ToString());
        }

        [EventHandleOrder(EventHandleOrder.First)]
        public void Handle(IssueImportedEvent message)
        {
            _cache.Remove("AllVolumes");
            _cache.Remove(message.Volume.Id.ToString());
        }

        [EventHandleOrder(EventHandleOrder.First)]
        public void Handle(IssueEditedEvent message)
        {
            _cache.Remove("AllVolumes");
            _cache.Remove(message.Issue.VolumeId.ToString());
        }

        [EventHandleOrder(EventHandleOrder.First)]
        public void Handle(IssueUpdatedEvent message)
        {
            _cache.Remove("AllVolumes");
            _cache.Remove(message.Issue.VolumeId.ToString());
        }

        [EventHandleOrder(EventHandleOrder.First)]
        public void Handle(IssueFileDeletedEvent message)
        {
            _cache.Remove("AllVolumes");

            var volumeId = message.IssueFile.Volume?.Value?.Id.ToString();
            if (volumeId != null)
            {
                _cache.Remove(volumeId);
            }
        }
    }
}
