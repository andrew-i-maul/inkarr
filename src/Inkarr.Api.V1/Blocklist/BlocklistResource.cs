using System;
using System.Collections.Generic;
using Inkarr.Api.V1.CustomFormats;
using Inkarr.Api.V1.Volume;
using Inkarr.Http.REST;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Qualities;

namespace Inkarr.Api.V1.Blocklist
{
    public class BlocklistResource : RestResource
    {
        public int VolumeId { get; set; }
        public List<int> IssueIds { get; set; }
        public string SourceTitle { get; set; }
        public QualityModel Quality { get; set; }
        public List<CustomFormatResource> CustomFormats { get; set; }
        public DateTime Date { get; set; }
        public DownloadProtocol Protocol { get; set; }
        public string Indexer { get; set; }
        public string Message { get; set; }

        public VolumeResource Volume { get; set; }
    }

    public static class BlocklistResourceMapper
    {
        public static BlocklistResource MapToResource(this NzbDrone.Core.Blocklisting.Blocklist model, ICustomFormatCalculationService formatCalculator)
        {
            if (model == null)
            {
                return null;
            }

            return new BlocklistResource
            {
                Id = model.Id,

                VolumeId = model.VolumeId,
                IssueIds = model.IssueIds,
                SourceTitle = model.SourceTitle,
                Quality = model.Quality,
                CustomFormats = formatCalculator.ParseCustomFormat(model, model.Volume).ToResource(false),
                Date = model.Date,
                Protocol = model.Protocol,
                Indexer = model.Indexer,
                Message = model.Message,

                Volume = model.Volume.ToResource()
            };
        }
    }
}
