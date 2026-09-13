using System;
using System.Collections.Generic;
using System.Linq;
using Inkarr.Api.V1.Volume;
using Inkarr.Http.REST;
using Newtonsoft.Json;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaCover;
using Swashbuckle.AspNetCore.Annotations;

namespace Inkarr.Api.V1.Issues
{
    public class IssueResource : RestResource
    {
        public string Title { get; set; }
        public string VolumeTitle { get; set; }
        public string SeriesTitle { get; set; }
        public string Disambiguation { get; set; }
        public string Overview { get; set; }
        public int VolumeId { get; set; }
        public string ForeignIssueId { get; set; }
        public string ForeignEditionId { get; set; }
        public string TitleSlug { get; set; }
        public bool Monitored { get; set; }
        public bool AnyEditionOk { get; set; }
        public Ratings Ratings { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public int PageCount { get; set; }
        public List<string> Genres { get; set; }
        public VolumeResource Volume { get; set; }
        public List<MediaCover> Images { get; set; }
        public List<Links> Links { get; set; }
        public IssueStatisticsResource Statistics { get; set; }
        public DateTime? Added { get; set; }
        public AddIssueOptions AddOptions { get; set; }
        public string RemoteCover { get; set; }
        public DateTime? LastSearchTime { get; set; }
        public List<EditionResource> Editions { get; set; }

        //Hiding this so people don't think its usable (only used to set the initial state)
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        [SwaggerIgnore]
        public bool Grabbed { get; set; }
    }

    public static class IssueResourceMapper
    {
        public static IssueResource ToResource(this Issue model)
        {
            if (model == null)
            {
                return null;
            }

            var selectedEdition = model.Editions?.Value.Where(x => x.Monitored).SingleOrDefault();

            var title = selectedEdition?.Title ?? model.Title;
            var volumeTitle = $"{model.Volume?.Value?.Metadata?.Value?.SortNameLastFirst} {title}";

            var seriesLinks = model.SeriesLinks?.Value?.OrderBy(x => x.SeriesPosition);
            var seriesTitle = seriesLinks?.Select(x => x?.Series?.Value?.Title + (x?.Position.IsNotNullOrWhiteSpace() ?? false ? $" #{x.Position}" : string.Empty)).ConcatToString("; ");

            return new IssueResource
            {
                Id = model.Id,
                VolumeId = model.VolumeId,
                ForeignIssueId = model.ForeignIssueId,
                ForeignEditionId = model.Editions?.Value?.SingleOrDefault(x => x.Monitored)?.ForeignEditionId,
                TitleSlug = model.TitleSlug,
                Monitored = model.Monitored,
                AnyEditionOk = model.AnyEditionOk,
                ReleaseDate = model.ReleaseDate,
                PageCount = selectedEdition?.PageCount ?? 0,
                Genres = model.Genres,
                Title = title,
                VolumeTitle = volumeTitle,
                SeriesTitle = seriesTitle,
                Disambiguation = selectedEdition?.Disambiguation,
                Images = selectedEdition?.Images ?? new List<MediaCover>(),
                Links = model.Links.Concat(selectedEdition?.Links ?? new List<Links>()).ToList(),
                Ratings = selectedEdition?.Ratings ?? new Ratings(),
                Added = model.Added,
                LastSearchTime = model.LastSearchTime
            };
        }

        public static Issue ToModel(this IssueResource resource)
        {
            if (resource == null)
            {
                return null;
            }

            var volume = resource.Volume?.ToModel() ?? new NzbDrone.Core.Issues.Volume();

            return new Issue
            {
                Id = resource.Id,
                ForeignIssueId = resource.ForeignIssueId,
                ForeignEditionId = resource.ForeignEditionId,
                TitleSlug = resource.TitleSlug,
                Title = resource.Title,
                Monitored = resource.Monitored,
                AnyEditionOk = resource.AnyEditionOk,
                Editions = resource.Editions.ToModel(),
                AddOptions = resource.AddOptions,
                Volume = volume,
                VolumeMetadata = volume.Metadata.Value
            };
        }

        public static Issue ToModel(this IssueResource resource, Issue issue)
        {
            var updatedIssue = resource.ToModel();

            issue.ApplyChanges(updatedIssue);
            issue.Editions = updatedIssue.Editions;

            return issue;
        }

        public static List<IssueResource> ToResource(this IEnumerable<Issue> models)
        {
            return models?.Select(ToResource).ToList();
        }

        public static List<Issue> ToModel(this IEnumerable<IssueResource> resources)
        {
            return resources.Select(ToModel).ToList();
        }
    }
}
