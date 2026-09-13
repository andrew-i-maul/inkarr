using System;
using System.Linq;
using Inkarr.Http.REST;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace Inkarr.Api.V1.IssueFiles
{
    public class IssueFileResource : RestResource
    {
        public int VolumeId { get; set; }
        public int IssueId { get; set; }
        public string Path { get; set; }
        public long Size { get; set; }
        public DateTime DateAdded { get; set; }
        public QualityModel Quality { get; set; }
        public int QualityWeight { get; set; }
        public int? IndexerFlags { get; set; }
        public MediaInfoResource MediaInfo { get; set; }

        public bool QualityCutoffNotMet { get; set; }
        public ParsedTrackInfo AudioTags { get; set; }
    }

    public static class IssueFileResourceMapper
    {
        private static int QualityWeight(QualityModel quality)
        {
            if (quality == null)
            {
                return 0;
            }

            var qualityWeight = Quality.DefaultQualityDefinitions.Single(q => q.Quality == quality.Quality).Weight;
            qualityWeight += quality.Revision.Real * 10;
            qualityWeight += quality.Revision.Version;
            return qualityWeight;
        }

        public static IssueFileResource ToResource(this IssueFile model)
        {
            if (model == null)
            {
                return null;
            }

            return new IssueFileResource
            {
                Id = model.Id,
                IssueId = model.Edition.Value?.IssueId ?? 0,
                Path = model.Path,
                Size = model.Size,
                DateAdded = model.DateAdded,
                Quality = model.Quality,
                QualityWeight = QualityWeight(model.Quality),
                MediaInfo = model.MediaInfo.ToResource()
            };
        }

        public static IssueFileResource ToResource(this IssueFile model, NzbDrone.Core.Issues.Volume volume, IUpgradableSpecification upgradableSpecification)
        {
            if (model == null)
            {
                return null;
            }

            return new IssueFileResource
            {
                Id = model.Id,

                VolumeId = volume.Id,
                IssueId = model.Edition.Value?.IssueId ?? 0,
                Path = model.Path,
                Size = model.Size,
                DateAdded = model.DateAdded,
                Quality = model.Quality,
                QualityWeight = QualityWeight(model.Quality),
                MediaInfo = model.MediaInfo.ToResource(),
                QualityCutoffNotMet = upgradableSpecification.QualityCutoffNotMet(volume.QualityProfile.Value, model.Quality),
                IndexerFlags = (int)model.IndexerFlags
            };
        }
    }
}
