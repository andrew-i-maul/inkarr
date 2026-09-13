using Equ;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Issues
{
    public class SeriesIssueLink : Entity<SeriesIssueLink>
    {
        public string Position { get; set; }
        public int SeriesPosition { get; set; }
        public int SeriesId { get; set; }
        public int IssueId { get; set; }
        public bool IsPrimary { get; set; }

        [MemberwiseEqualityIgnore]
        public LazyLoaded<Series> Series { get; set; }
        [MemberwiseEqualityIgnore]
        public LazyLoaded<Issue> Issue { get; set; }

        public override void UseMetadataFrom(SeriesIssueLink other)
        {
            Position = other.Position;
            SeriesPosition = other.SeriesPosition;
            IsPrimary = other.IsPrimary;
        }

        public override void UseDbFieldsFrom(SeriesIssueLink other)
        {
            Id = other.Id;
            SeriesId = other.SeriesId;
            IssueId = other.IssueId;
        }
    }
}
