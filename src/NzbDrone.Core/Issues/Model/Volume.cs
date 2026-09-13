using System;
using System.Collections.Generic;
using Equ;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Profiles.Metadata;
using NzbDrone.Core.Profiles.Qualities;

namespace NzbDrone.Core.Issues
{
    public class Volume : Entity<Volume>
    {
        public Volume()
        {
            Tags = new HashSet<int>();
            Metadata = new VolumeMetadata();
        }

        // These correspond to columns in the Volumes table
        public int VolumeMetadataId { get; set; }
        public string CleanName { get; set; }
        public bool Monitored { get; set; }
        public NewItemMonitorTypes MonitorNewItems { get; set; }
        public DateTime? LastInfoSync { get; set; }
        public string Path { get; set; }
        public string RootFolderPath { get; set; }
        public DateTime Added { get; set; }
        public int QualityProfileId { get; set; }
        public int MetadataProfileId { get; set; }
        public HashSet<int> Tags { get; set; }
        [MemberwiseEqualityIgnore]
        public AddVolumeOptions AddOptions { get; set; }

        // Dynamically loaded from DB
        [MemberwiseEqualityIgnore]
        public LazyLoaded<VolumeMetadata> Metadata { get; set; }
        [MemberwiseEqualityIgnore]
        public LazyLoaded<QualityProfile> QualityProfile { get; set; }
        [MemberwiseEqualityIgnore]
        public LazyLoaded<MetadataProfile> MetadataProfile { get; set; }
        [MemberwiseEqualityIgnore]
        public LazyLoaded<List<Issue>> Issues { get; set; }
        [MemberwiseEqualityIgnore]
        public LazyLoaded<List<Series>> Series { get; set; }

        //compatibility properties
        [MemberwiseEqualityIgnore]
        public string Name
        {
            get { return Metadata.Value.Name; } set { Metadata.Value.Name = value; }
        }

        [MemberwiseEqualityIgnore]
        public string ForeignVolumeId
        {
            get { return Metadata.Value.ForeignVolumeId; } set { Metadata.Value.ForeignVolumeId = value; }
        }

        public override string ToString()
        {
            return string.Format("[{0}][{1}]", Metadata.Value.ForeignVolumeId.NullSafe(), Metadata.Value.Name.NullSafe());
        }

        public override void UseMetadataFrom(Volume other)
        {
            CleanName = other.CleanName;
        }

        public override void UseDbFieldsFrom(Volume other)
        {
            Id = other.Id;
            VolumeMetadataId = other.VolumeMetadataId;
            Monitored = other.Monitored;
            MonitorNewItems = other.MonitorNewItems;
            LastInfoSync = other.LastInfoSync;
            Path = other.Path;
            RootFolderPath = other.RootFolderPath;
            Added = other.Added;
            QualityProfileId = other.QualityProfileId;
            QualityProfile = other.QualityProfile;
            MetadataProfileId = other.MetadataProfileId;
            MetadataProfile = other.MetadataProfile;
            Tags = other.Tags;
            AddOptions = other.AddOptions;
        }

        public override void ApplyChanges(Volume other)
        {
            Path = other.Path;
            QualityProfileId = other.QualityProfileId;
            QualityProfile = other.QualityProfile;
            MetadataProfileId = other.MetadataProfileId;
            MetadataProfile = other.MetadataProfile;

            Issues = other.Issues;
            Tags = other.Tags;
            AddOptions = other.AddOptions;
            RootFolderPath = other.RootFolderPath;
            Monitored = other.Monitored;
            MonitorNewItems = other.MonitorNewItems;
        }
    }
}
