using System;

namespace NzbDrone.Core.Parser.Model
{
    public class ImportListItemInfo
    {
        public int ImportListId { get; set; }
        public string ImportList { get; set; }
        public string Volume { get; set; }
        public string VolumeGoodreadsId { get; set; }
        public string Issue { get; set; }
        public string IssueGoodreadsId { get; set; }
        public string EditionGoodreadsId { get; set; }
        public DateTime ReleaseDate { get; set; }

        public override string ToString()
        {
            return string.Format("[{0}] {1} [{2}]", ReleaseDate, Volume, Issue);
        }
    }
}
