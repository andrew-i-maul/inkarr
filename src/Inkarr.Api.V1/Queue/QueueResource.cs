using System;
using System.Collections.Generic;
using System.Linq;
using Inkarr.Api.V1.CustomFormats;
using Inkarr.Api.V1.Issues;
using Inkarr.Api.V1.Volume;
using Inkarr.Http.REST;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Qualities;

namespace Inkarr.Api.V1.Queue
{
    public class QueueResource : RestResource
    {
        public int? VolumeId { get; set; }
        public int? IssueId { get; set; }
        public VolumeResource Volume { get; set; }
        public IssueResource Issue { get; set; }
        public QualityModel Quality { get; set; }
        public List<CustomFormatResource> CustomFormats { get; set; }
        public int CustomFormatScore { get; set; }
        public decimal Size { get; set; }
        public string Title { get; set; }
        public decimal Sizeleft { get; set; }
        public TimeSpan? Timeleft { get; set; }
        public DateTime? EstimatedCompletionTime { get; set; }
        public string Status { get; set; }
        public TrackedDownloadStatus? TrackedDownloadStatus { get; set; }
        public TrackedDownloadState? TrackedDownloadState { get; set; }
        public List<TrackedDownloadStatusMessage> StatusMessages { get; set; }
        public string ErrorMessage { get; set; }
        public string DownloadId { get; set; }
        public DownloadProtocol Protocol { get; set; }
        public string DownloadClient { get; set; }
        public bool DownloadClientHasPostImportCategory { get; set; }
        public string Indexer { get; set; }
        public string OutputPath { get; set; }
        public bool DownloadForced { get; set; }
    }

    public static class QueueResourceMapper
    {
        public static QueueResource ToResource(this NzbDrone.Core.Queue.Queue model, bool includeVolume, bool includeIssue)
        {
            if (model == null)
            {
                return null;
            }

            var customFormats = model.RemoteIssue?.CustomFormats;
            var customFormatScore = model.RemoteIssue?.Volume?.QualityProfile?.Value?.CalculateCustomFormatScore(customFormats) ?? 0;

            return new QueueResource
            {
                Id = model.Id,
                VolumeId = model.Volume?.Id,
                IssueId = model.Issue?.Id,
                Volume = includeVolume && model.Volume != null ? model.Volume.ToResource() : null,
                Issue = includeIssue && model.Issue != null ? model.Issue.ToResource() : null,
                Quality = model.Quality,
                CustomFormats = customFormats?.ToResource(false),
                CustomFormatScore = customFormatScore,
                Size = model.Size,
                Title = model.Title,
                Sizeleft = model.Sizeleft,
                Timeleft = model.Timeleft,
                EstimatedCompletionTime = model.EstimatedCompletionTime,
                Status = model.Status.FirstCharToLower(),
                TrackedDownloadStatus = model.TrackedDownloadStatus,
                TrackedDownloadState = model.TrackedDownloadState,
                StatusMessages = model.StatusMessages,
                ErrorMessage = model.ErrorMessage,
                DownloadId = model.DownloadId,
                Protocol = model.Protocol,
                DownloadClient = model.DownloadClient,
                DownloadClientHasPostImportCategory = model.DownloadClientHasPostImportCategory,
                Indexer = model.Indexer,
                OutputPath = model.OutputPath,
                DownloadForced = model.DownloadForced
            };
        }

        public static List<QueueResource> ToResource(this IEnumerable<NzbDrone.Core.Queue.Queue> models, bool includeVolume, bool includeIssue)
        {
            return models.Select((m) => ToResource(m, includeVolume, includeIssue)).ToList();
        }
    }
}
