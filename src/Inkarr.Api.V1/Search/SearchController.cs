using System;
using System.Collections.Generic;
using System.Linq;
using Inkarr.Api.V1.Issues;
using Inkarr.Api.V1.Volume;
using Inkarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Organizer;

namespace Inkarr.Api.V1.Search
{
    [V1ApiController]
    public class SearchController : Controller
    {
        private readonly ISearchForNewEntity _searchProxy;
        private readonly IBuildFileNames _fileNameBuilder;
        private readonly IMapCoversToLocal _coverMapper;

        public SearchController(ISearchForNewEntity searchProxy, IBuildFileNames fileNameBuilder, IMapCoversToLocal coverMapper)
        {
            _searchProxy = searchProxy;
            _fileNameBuilder = fileNameBuilder;
            _coverMapper = coverMapper;
        }

        [HttpGet]
        public object Search([FromQuery] string term)
        {
            var searchResults = _searchProxy.SearchForNewEntity(term);
            return MapToResource(searchResults).ToList();
        }

        private IEnumerable<SearchResource> MapToResource(IEnumerable<object> results)
        {
            var id = 1;
            foreach (var result in results)
            {
                var resource = new SearchResource();
                resource.Id = id++;

                if (result is NzbDrone.Core.Issues.Volume volume)
                {
                    resource.Volume = volume.ToResource();
                    resource.ForeignId = volume.ForeignVolumeId;

                    _coverMapper.ConvertToLocalUrls(resource.Volume.Id, MediaCoverEntity.Volume, resource.Volume.Images);

                    var poster = resource.Volume.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Poster);

                    if (poster != null)
                    {
                        resource.Volume.RemotePoster = poster.RemoteUrl;
                    }

                    resource.Volume.Folder = _fileNameBuilder.GetVolumeFolder(volume);
                }
                else if (result is NzbDrone.Core.Issues.Issue issue)
                {
                    resource.Issue = issue.ToResource();
                    resource.Issue.Overview = issue.Editions.Value.Single(x => x.Monitored).Overview;
                    resource.Issue.Volume = issue.Volume.Value.ToResource();
                    resource.Issue.Editions = issue.Editions.Value.ToResource();
                    resource.ForeignId = issue.ForeignIssueId;

                    _coverMapper.ConvertToLocalUrls(resource.Issue.Id, MediaCoverEntity.Issue, resource.Issue.Images);

                    var cover = resource.Issue.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Cover);

                    if (cover != null)
                    {
                        resource.Issue.RemoteCover = cover.RemoteUrl;
                    }

                    resource.Issue.Volume.Folder = _fileNameBuilder.GetVolumeFolder(issue.Volume);
                }
                else
                {
                    throw new NotImplementedException("Bad response from search all proxy");
                }

                yield return resource;
            }
        }
    }
}
