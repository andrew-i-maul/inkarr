using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.MediaCover
{
    public interface IMapCoversToLocal
    {
        void ConvertToLocalUrls(int entityId, MediaCoverEntity coverEntity, IEnumerable<MediaCover> covers);
        string GetCoverPath(int entityId, MediaCoverEntity coverEntity, MediaCoverTypes coverType, string extension, int? height = null);
        void EnsureIssueCovers(Issue issue);
    }

    public class MediaCoverService :
        IHandleAsync<VolumeRefreshCompleteEvent>,
        IHandleAsync<VolumeDeletedEvent>,
        IHandleAsync<IssueDeletedEvent>,
        IMapCoversToLocal
    {
        private const string USER_AGENT = "Dalvik/2.1.0 (Linux; U; Android 10; SM-G975U Build/QP1A.190711.020)";

        private readonly IMediaCoverProxy _mediaCoverProxy;
        private readonly IImageResizer _resizer;
        private readonly IIssueService _issueService;
        private readonly IHttpClient _httpClient;
        private readonly IDiskProvider _diskProvider;
        private readonly ICoverExistsSpecification _coverExistsSpecification;
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        private readonly string _coverRootFolder;

        // ImageSharp is slow on ARM (no hardware acceleration on mono yet)
        // So limit the number of concurrent resizing tasks
        private static SemaphoreSlim _semaphore = new SemaphoreSlim((int)Math.Ceiling(Environment.ProcessorCount / 2.0));

        public MediaCoverService(IMediaCoverProxy mediaCoverProxy,
                                 IImageResizer resizer,
                                 IIssueService issueService,
                                 IHttpClient httpClient,
                                 IDiskProvider diskProvider,
                                 IAppFolderInfo appFolderInfo,
                                 ICoverExistsSpecification coverExistsSpecification,
                                 IConfigFileProvider configFileProvider,
                                 IEventAggregator eventAggregator,
                                 Logger logger)
        {
            _mediaCoverProxy = mediaCoverProxy;
            _resizer = resizer;
            _issueService = issueService;
            _httpClient = httpClient;
            _diskProvider = diskProvider;
            _coverExistsSpecification = coverExistsSpecification;
            _configFileProvider = configFileProvider;
            _eventAggregator = eventAggregator;
            _logger = logger;

            _coverRootFolder = appFolderInfo.GetMediaCoverPath();
        }

        public string GetCoverPath(int entityId, MediaCoverEntity coverEntity, MediaCoverTypes coverType, string extension, int? height = null)
        {
            var heightSuffix = height.HasValue ? "-" + height.ToString() : "";

            if (coverEntity == MediaCoverEntity.Issue)
            {
                return Path.Combine(GetIssueCoverPath(entityId), coverType.ToString().ToLower() + heightSuffix + GetExtension(coverType, extension));
            }

            return Path.Combine(GetVolumeCoverPath(entityId), coverType.ToString().ToLower() + heightSuffix + GetExtension(coverType, extension));
        }

        public void ConvertToLocalUrls(int entityId, MediaCoverEntity coverEntity, IEnumerable<MediaCover> covers)
        {
            if (entityId == 0)
            {
                // Volume isn't in Inkarr yet, map via a proxy to circument referrer issues
                foreach (var mediaCover in covers)
                {
                    mediaCover.RemoteUrl = mediaCover.Url;
                    mediaCover.Url = _mediaCoverProxy.RegisterUrl(mediaCover.RemoteUrl);
                }
            }
            else
            {
                foreach (var mediaCover in covers)
                {
                    if (mediaCover.CoverType == MediaCoverTypes.Unknown)
                    {
                        continue;
                    }

                    var filePath = GetCoverPath(entityId, coverEntity, mediaCover.CoverType, mediaCover.Extension, null);

                    mediaCover.RemoteUrl = mediaCover.Url;

                    if (coverEntity == MediaCoverEntity.Issue)
                    {
                        mediaCover.Url = _configFileProvider.UrlBase + @"/MediaCover/Issues/" + entityId + "/" + mediaCover.CoverType.ToString().ToLower() + GetExtension(mediaCover.CoverType, mediaCover.Extension);
                    }
                    else
                    {
                        mediaCover.Url = _configFileProvider.UrlBase + @"/MediaCover/" + entityId + "/" + mediaCover.CoverType.ToString().ToLower() + GetExtension(mediaCover.CoverType, mediaCover.Extension);
                    }

                    if (_diskProvider.FileExists(filePath))
                    {
                        var lastWrite = _diskProvider.FileGetLastWrite(filePath);
                        mediaCover.Url += "?lastWrite=" + lastWrite.Ticks;
                    }
                }
            }
        }

        private string GetVolumeCoverPath(int volumeId)
        {
            return Path.Combine(_coverRootFolder, volumeId.ToString());
        }

        private string GetIssueCoverPath(int issueId)
        {
            return Path.Combine(_coverRootFolder, "Issues", issueId.ToString());
        }

        private void EnsureVolumeCovers(Volume volume)
        {
            var toResize = new List<Tuple<MediaCover, bool>>();

            foreach (var cover in volume.Metadata.Value.Images)
            {
                if (cover.CoverType == MediaCoverTypes.Unknown)
                {
                    continue;
                }

                var fileName = GetCoverPath(volume.Id, MediaCoverEntity.Volume, cover.CoverType, cover.Extension);
                var alreadyExists = false;

                try
                {
                    var serverFileHeaders = GetServerHeaders(cover.Url);

                    alreadyExists = _coverExistsSpecification.AlreadyExists(serverFileHeaders.LastModified, GetContentLength(serverFileHeaders), fileName);

                    if (!alreadyExists)
                    {
                        DownloadCover(volume, cover, serverFileHeaders.LastModified ?? DateTime.Now);
                    }
                }
                catch (HttpException e)
                {
                    _logger.Warn("Couldn't download media cover for {0}. {1}", volume, e.Message);
                }
                catch (WebException e)
                {
                    _logger.Warn("Couldn't download media cover for {0}. {1}", volume, e.Message);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Couldn't download media cover for {0}", volume);
                }

                toResize.Add(Tuple.Create(cover, alreadyExists));
            }

            try
            {
                _semaphore.Wait();

                foreach (var tuple in toResize)
                {
                    EnsureResizedCovers(volume, tuple.Item1, !tuple.Item2);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void EnsureIssueCovers(Issue issue)
        {
            foreach (var cover in issue.Editions.Value.Single(x => x.Monitored).Images.Where(e => e.CoverType == MediaCoverTypes.Cover))
            {
                if (cover.CoverType == MediaCoverTypes.Unknown)
                {
                    continue;
                }

                var fileName = GetCoverPath(issue.Id, MediaCoverEntity.Issue, cover.CoverType, cover.Extension, null);
                var alreadyExists = false;

                try
                {
                    var serverFileHeaders = GetServerHeaders(cover.Url);

                    alreadyExists = _coverExistsSpecification.AlreadyExists(serverFileHeaders.LastModified, GetContentLength(serverFileHeaders), fileName);

                    if (!alreadyExists)
                    {
                        DownloadIssueCover(issue, cover, serverFileHeaders.LastModified ?? DateTime.Now);
                    }
                }
                catch (HttpException e)
                {
                    _logger.Warn("Couldn't download media cover for {0}. {1}", issue, e.Message);
                }
                catch (WebException e)
                {
                    _logger.Warn("Couldn't download media cover for {0}. {1}", issue, e.Message);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Couldn't download media cover for {0}", issue);
                }
            }
        }

        private void DownloadCover(Volume volume, MediaCover cover, DateTime lastModified)
        {
            var fileName = GetCoverPath(volume.Id, MediaCoverEntity.Volume, cover.CoverType, cover.Extension);

            _logger.Info("Downloading {0} for {1} {2}", cover.CoverType, volume, cover.Url);
            _httpClient.DownloadFile(cover.Url, fileName, USER_AGENT);

            try
            {
                _diskProvider.FileSetLastWriteTime(fileName, lastModified);
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Unable to set modified date for {0} image for volume {1}", cover.CoverType, volume);
            }
        }

        private void DownloadIssueCover(Issue issue, MediaCover cover, DateTime lastModified)
        {
            var fileName = GetCoverPath(issue.Id, MediaCoverEntity.Issue, cover.CoverType, cover.Extension, null);

            _logger.Info("Downloading {0} for {1} {2}", cover.CoverType, issue, cover.Url);
            _httpClient.DownloadFile(cover.Url, fileName, USER_AGENT);

            try
            {
                _diskProvider.FileSetLastWriteTime(fileName, lastModified);
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Unable to set modified date for {0} image for issue {1}", cover.CoverType, issue);
            }
        }

        private void EnsureResizedCovers(Volume volume, MediaCover cover, bool forceResize, Issue issue = null)
        {
            var heights = GetDefaultHeights(cover.CoverType);

            foreach (var height in heights)
            {
                var mainFileName = GetCoverPath(volume.Id, MediaCoverEntity.Volume, cover.CoverType, cover.Extension);
                var resizeFileName = GetCoverPath(volume.Id, MediaCoverEntity.Volume, cover.CoverType, cover.Extension, height);

                if (forceResize || !_diskProvider.FileExists(resizeFileName) || _diskProvider.GetFileSize(resizeFileName) == 0)
                {
                    _logger.Debug("Resizing {0}-{1} for {2}", cover.CoverType, height, volume);

                    try
                    {
                        _resizer.Resize(mainFileName, resizeFileName, height);
                    }
                    catch
                    {
                        _logger.Debug("Couldn't resize media cover {0}-{1} for volume {2}, using full size image instead.", cover.CoverType, height, volume);
                    }
                }
            }
        }

        private int[] GetDefaultHeights(MediaCoverTypes coverType)
        {
            switch (coverType)
            {
                default:
                    return new int[] { };

                case MediaCoverTypes.Poster:
                case MediaCoverTypes.Disc:
                case MediaCoverTypes.Cover:
                case MediaCoverTypes.Logo:
                case MediaCoverTypes.Headshot:
                    return new[] { 500, 250 };

                case MediaCoverTypes.Banner:
                    return new[] { 70, 35 };

                case MediaCoverTypes.Fanart:
                case MediaCoverTypes.Screenshot:
                    return new[] { 360, 180 };
            }
        }

        private string GetExtension(MediaCoverTypes coverType, string defaultExtension)
        {
            return coverType switch
            {
                MediaCoverTypes.Clearlogo => ".png",
                _ => defaultExtension
            };
        }

        private HttpHeader GetServerHeaders(string url)
        {
            // Goodreads doesn't allow a HEAD, so request a zero byte range instead
            var request = new HttpRequest(url)
            {
                AllowAutoRedirect = true,
            };

            request.Headers.Add("Range", "bytes=0-0");
            request.Headers.Add("User-Agent", USER_AGENT);

            return _httpClient.Get(request).Headers;
        }

        private long? GetContentLength(HttpHeader headers)
        {
            var range = headers.Get("content-range");

            if (range == null)
            {
                return null;
            }

            var split = range.Split('/');
            if (split.Length == 2 && long.TryParse(split[1], out var length))
            {
                return length;
            }

            return null;
        }

        public void HandleAsync(VolumeRefreshCompleteEvent message)
        {
            EnsureVolumeCovers(message.Volume);

            var issues = _issueService.GetIssuesByVolume(message.Volume.Id);
            foreach (var issue in issues)
            {
                EnsureIssueCovers(issue);
            }

            _eventAggregator.PublishEvent(new MediaCoversUpdatedEvent(message.Volume));
        }

        public void HandleAsync(VolumeDeletedEvent message)
        {
            var path = GetVolumeCoverPath(message.Volume.Id);
            if (_diskProvider.FolderExists(path))
            {
                _diskProvider.DeleteFolder(path, true);
            }
        }

        public void HandleAsync(IssueDeletedEvent message)
        {
            var path = GetIssueCoverPath(message.Issue.Id);
            if (_diskProvider.FolderExists(path))
            {
                _diskProvider.DeleteFolder(path, true);
            }
        }
    }
}
