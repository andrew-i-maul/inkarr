using System.Collections.Generic;

namespace NzbDrone.Core.ImportLists.Readarr
{
    public class ReadarrVolume
    {
        public string VolumeName { get; set; }
        public int Id { get; set; }
        public string ForeignVolumeId { get; set; }
        public string Overview { get; set; }
        public List<MediaCover.MediaCover> Images { get; set; }
        public bool Monitored { get; set; }
        public int QualityProfileId { get; set; }
        public string RootFolderPath { get; set; }
        public HashSet<int> Tags { get; set; }
    }

    public class ReadarrEdition
    {
        public string Title { get; set; }
        public string ForeignEditionId { get; set; }
        public string Overview { get; set; }
        public List<MediaCover.MediaCover> Images { get; set; }
        public bool Monitored { get; set; }
    }

    public class ReadarrIssue
    {
        public string Title { get; set; }
        public string ForeignIssueId { get; set; }
        public string ForeignEditionId { get; set; }
        public string Overview { get; set; }
        public List<MediaCover.MediaCover> Images { get; set; }
        public bool Monitored { get; set; }
        public ReadarrVolume Volume { get; set; }
        public int VolumeId { get; set; }
        public List<ReadarrEdition> Editions { get; set; }
    }

    public class ReadarrProfile
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }

    public class ReadarrTag
    {
        public string Label { get; set; }
        public int Id { get; set; }
    }

    public class ReadarrRootFolder
    {
        public string Path { get; set; }
        public int Id { get; set; }
    }
}
