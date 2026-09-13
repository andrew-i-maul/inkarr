using System;
using System.Collections.Generic;
using System.Linq;
using Inkarr.Http.REST;
using Newtonsoft.Json;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaCover;

namespace Inkarr.Api.V1.Volume
{
    public class VolumeResource : RestResource
    {
        //Todo: Sorters should be done completely on the client
        //Todo: Is there an easy way to keep IgnoreArticlesWhenSorting in sync between, Series, History, Missing?
        //Todo: We should get the entire Profile instead of ID and Name separately
        [JsonIgnore]
        public int VolumeMetadataId { get; set; }
        public VolumeStatusType Status { get; set; }

        public bool Ended => Status == VolumeStatusType.Ended;

        public string VolumeName { get; set; }
        public string VolumeNameLastFirst { get; set; }
        public string ForeignVolumeId { get; set; }
        public string TitleSlug { get; set; }
        public string Overview { get; set; }
        public string Disambiguation { get; set; }
        public List<Links> Links { get; set; }

        public Issue NextIssue { get; set; }
        public Issue LastIssue { get; set; }

        public List<MediaCover> Images { get; set; }

        public string RemotePoster { get; set; }

        //View & Edit
        public string Path { get; set; }
        public int QualityProfileId { get; set; }
        public int MetadataProfileId { get; set; }

        //Editing Only
        public bool Monitored { get; set; }
        public NewItemMonitorTypes MonitorNewItems { get; set; }

        public string RootFolderPath { get; set; }
        public string Folder { get; set; }
        public List<string> Genres { get; set; }
        public string CleanName { get; set; }
        public string SortName { get; set; }
        public string SortNameLastFirst { get; set; }

        public HashSet<int> Tags { get; set; }
        public DateTime Added { get; set; }
        public AddVolumeOptions AddOptions { get; set; }
        public Ratings Ratings { get; set; }

        public VolumeStatisticsResource Statistics { get; set; }
    }

    public static class VolumeResourceMapper
    {
        public static VolumeResource ToResource(this NzbDrone.Core.Issues.Volume model)
        {
            if (model == null)
            {
                return null;
            }

            return new VolumeResource
            {
                Id = model.Id,
                VolumeMetadataId = model.VolumeMetadataId,

                VolumeName = model.Name,
                VolumeNameLastFirst = model.Metadata.Value.NameLastFirst,

                //AlternateTitles
                SortName = model.Metadata.Value.SortName,
                SortNameLastFirst = model.Metadata.Value.SortNameLastFirst,

                Status = model.Metadata.Value.Status,
                Overview = model.Metadata.Value.Overview,
                Disambiguation = model.Metadata.Value.Disambiguation,

                Images = model.Metadata.Value.Images.JsonClone(),

                Path = model.Path,
                QualityProfileId = model.QualityProfileId,
                MetadataProfileId = model.MetadataProfileId,
                Links = model.Metadata.Value.Links,

                Monitored = model.Monitored,
                MonitorNewItems = model.MonitorNewItems,

                CleanName = model.CleanName,
                ForeignVolumeId = model.Metadata.Value.ForeignVolumeId,
                TitleSlug = model.Metadata.Value.TitleSlug,

                // Root folder path is now calculated from the volume path
                // RootFolderPath = model.RootFolderPath,
                Genres = model.Metadata.Value.Genres,
                Tags = model.Tags,
                Added = model.Added,
                AddOptions = model.AddOptions,
                Ratings = model.Metadata.Value.Ratings,

                Statistics = new VolumeStatisticsResource()
            };
        }

        public static NzbDrone.Core.Issues.Volume ToModel(this VolumeResource resource)
        {
            if (resource == null)
            {
                return null;
            }

            return new NzbDrone.Core.Issues.Volume
            {
                Id = resource.Id,

                Metadata = new NzbDrone.Core.Issues.VolumeMetadata
                {
                    ForeignVolumeId = resource.ForeignVolumeId,
                    TitleSlug = resource.TitleSlug,
                    Name = resource.VolumeName,
                    NameLastFirst = resource.VolumeNameLastFirst,
                    SortName = resource.SortName,
                    SortNameLastFirst = resource.SortNameLastFirst,
                    Status = resource.Status,
                    Overview = resource.Overview,
                    Links = resource.Links,
                    Images = resource.Images,
                    Genres = resource.Genres,
                    Ratings = resource.Ratings,
                },

                //AlternateTitles
                Path = resource.Path,
                QualityProfileId = resource.QualityProfileId,
                MetadataProfileId = resource.MetadataProfileId,

                Monitored = resource.Monitored,
                MonitorNewItems = resource.MonitorNewItems,

                CleanName = resource.CleanName,
                RootFolderPath = resource.RootFolderPath,

                Tags = resource.Tags,
                Added = resource.Added,
                AddOptions = resource.AddOptions
            };
        }

        public static NzbDrone.Core.Issues.Volume ToModel(this VolumeResource resource, NzbDrone.Core.Issues.Volume volume)
        {
            var updatedVolume = resource.ToModel();

            volume.ApplyChanges(updatedVolume);

            return volume;
        }

        public static List<VolumeResource> ToResource(this IEnumerable<NzbDrone.Core.Issues.Volume> volume)
        {
            return volume.Select(ToResource).ToList();
        }

        public static List<NzbDrone.Core.Issues.Volume> ToModel(this IEnumerable<VolumeResource> resources)
        {
            return resources.Select(ToModel).ToList();
        }
    }
}
