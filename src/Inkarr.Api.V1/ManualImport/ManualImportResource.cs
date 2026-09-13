using System.Collections.Generic;
using System.Linq;
using Inkarr.Api.V1.Issues;
using Inkarr.Api.V1.Volume;
using Inkarr.Http.REST;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.MediaFiles.IssueImport.Manual;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace Inkarr.Api.V1.ManualImport
{
    public class ManualImportResource : RestResource
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public VolumeResource Volume { get; set; }
        public IssueResource Issue { get; set; }
        public string ForeignEditionId { get; set; }
        public QualityModel Quality { get; set; }
        public string ReleaseGroup { get; set; }
        public int QualityWeight { get; set; }
        public string DownloadId { get; set; }
        public int IndexerFlags { get; set; }
        public IEnumerable<Rejection> Rejections { get; set; }
        public ParsedTrackInfo AudioTags { get; set; }
        public bool AdditionalFile { get; set; }
        public bool ReplaceExistingFiles { get; set; }
        public bool DisableReleaseSwitching { get; set; }
    }

    public static class ManualImportResourceMapper
    {
        public static ManualImportResource ToResource(this ManualImportItem model)
        {
            if (model == null)
            {
                return null;
            }

            return new ManualImportResource
            {
                Id = model.Id,
                Path = model.Path,
                Name = model.Name,
                Size = model.Size,
                Volume = model.Volume.ToResource(),
                Issue = model.Issue.ToResource(),
                ForeignEditionId = model.Edition?.ForeignEditionId ?? model.Issue?.Editions.Value.Single(x => x.Monitored).ForeignEditionId,
                Quality = model.Quality,
                ReleaseGroup = model.ReleaseGroup,

                //QualityWeight
                DownloadId = model.DownloadId,
                IndexerFlags = model.IndexerFlags,
                Rejections = model.Rejections,

                AudioTags = model.Tags,
                AdditionalFile = model.AdditionalFile,
                ReplaceExistingFiles = model.ReplaceExistingFiles,
                DisableReleaseSwitching = model.DisableReleaseSwitching
            };
        }

        public static List<ManualImportResource> ToResource(this IEnumerable<ManualImportItem> models)
        {
            return models.Select(ToResource).ToList();
        }
    }
}
