using System.Collections.Generic;
using System.IO;
using System.Linq;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.IssueImport.Identification;

namespace NzbDrone.Core.Parser.Model
{
    public class LocalEdition
    {
        public LocalEdition()
        {
            LocalIssues = new List<LocalIssue>();

            // A dummy distance, will be replaced
            Distance = new Distance();
            Distance.Add("issue_id", 1.0);
        }

        public LocalEdition(List<LocalIssue> tracks)
        {
            LocalIssues = tracks;

            // A dummy distance, will be replaced
            Distance = new Distance();
            Distance.Add("issue_id", 1.0);
        }

        public List<LocalIssue> LocalIssues { get; set; }
        public int TrackCount => LocalIssues.Count;

        public Distance Distance { get; set; }
        public Edition Edition { get; set; }
        public List<LocalIssue> ExistingTracks { get; set; }
        public bool NewDownload { get; set; }

        public void PopulateMatch(bool keepAllEditions)
        {
            if (Edition != null)
            {
                LocalIssues = LocalIssues.Concat(ExistingTracks).DistinctBy(x => x.Path).ToList();

                if (!keepAllEditions)
                {
                    // Manually clone the edition / issue to avoid holding references to *every* edition we have
                    // seen during the matching process
                    var edition = new Edition();
                    edition.UseMetadataFrom(Edition);
                    edition.UseDbFieldsFrom(Edition);
                    edition.IssueFiles = Edition.IssueFiles;

                    var fullIssue = Edition.Issue.Value;

                    var issue = new Issue();
                    issue.UseMetadataFrom(fullIssue);
                    issue.UseDbFieldsFrom(fullIssue);
                    issue.Volume.Value.UseMetadataFrom(fullIssue.Volume.Value);
                    issue.Volume.Value.UseDbFieldsFrom(fullIssue.Volume.Value);
                    issue.Volume.Value.Metadata = fullIssue.VolumeMetadata.Value;
                    issue.VolumeMetadata = fullIssue.VolumeMetadata.Value;
                    issue.IssueFiles = fullIssue.IssueFiles;
                    issue.Editions = new List<Edition> { edition };

                    if (fullIssue.SeriesLinks.IsLoaded)
                    {
                        issue.SeriesLinks = fullIssue.SeriesLinks.Value.Select(l => new SeriesIssueLink
                        {
                            Issue = issue,
                            Series = new Series
                            {
                                ForeignSeriesId = l.Series.Value.ForeignSeriesId,
                                Title = l.Series.Value.Title,
                                Description = l.Series.Value.Description,
                                Numbered = l.Series.Value.Numbered,
                                WorkCount = l.Series.Value.WorkCount,
                                PrimaryWorkCount = l.Series.Value.PrimaryWorkCount
                            },
                            IsPrimary = l.IsPrimary,
                            Position = l.Position,
                            SeriesPosition = l.SeriesPosition
                        }).ToList();
                    }
                    else
                    {
                        issue.SeriesLinks = fullIssue.SeriesLinks;
                    }

                    edition.Issue = issue;

                    Edition = edition;

                    foreach (var localTrack in LocalIssues)
                    {
                        localTrack.Edition = edition;
                        localTrack.Issue = issue;
                        localTrack.Volume = issue.Volume.Value;
                        localTrack.PartCount = LocalIssues.Count;
                    }
                }
                else
                {
                    foreach (var localTrack in LocalIssues)
                    {
                        localTrack.Edition = Edition;
                        localTrack.Issue = Edition.Issue.Value;
                        localTrack.Volume = Edition.Issue.Value.Volume.Value;
                        localTrack.PartCount = LocalIssues.Count;
                    }
                }
            }
        }

        public override string ToString()
        {
            return "[" + string.Join(", ", LocalIssues.Select(x => Path.GetDirectoryName(x.Path)).Distinct()) + "]";
        }
    }
}
