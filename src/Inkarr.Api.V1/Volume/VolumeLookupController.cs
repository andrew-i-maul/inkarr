using System.Collections.Generic;
using System.Linq;
using Inkarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Organizer;

namespace Inkarr.Api.V1.Volume
{
    [V1ApiController("volume/lookup")]
    public class VolumeLookupController : Controller
    {
        private readonly ISearchForNewVolume _searchProxy;
        private readonly IBuildFileNames _fileNameBuilder;
        private readonly IMapCoversToLocal _coverMapper;

        public VolumeLookupController(ISearchForNewVolume searchProxy, IBuildFileNames fileNameBuilder, IMapCoversToLocal coverMapper)
        {
            _searchProxy = searchProxy;
            _fileNameBuilder = fileNameBuilder;
            _coverMapper = coverMapper;
        }

        [HttpGet]
        public object Search([FromQuery] string term)
        {
            var searchResults = _searchProxy.SearchForNewVolume(term);
            return MapToResource(searchResults).ToList();
        }

        private IEnumerable<VolumeResource> MapToResource(IEnumerable<NzbDrone.Core.Issues.Volume> volume)
        {
            foreach (var currentVolume in volume)
            {
                var resource = currentVolume.ToResource();

                _coverMapper.ConvertToLocalUrls(resource.Id, MediaCoverEntity.Volume, resource.Images);

                var poster = resource.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Poster);

                if (poster != null)
                {
                    resource.RemotePoster = poster.RemoteUrl;
                }

                resource.Folder = _fileNameBuilder.GetVolumeFolder(currentVolume);

                yield return resource;
            }
        }
    }
}
