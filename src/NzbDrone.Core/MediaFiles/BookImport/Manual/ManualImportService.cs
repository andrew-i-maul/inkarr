using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using NLog;
using NzbDrone.Common;
using NzbDrone.Common.Crypto;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.MediaFiles.IssueImport.Manual
{
    public interface IManualImportService
    {
        List<ManualImportItem> GetMediaFiles(string path, string downloadId, Volume volume, FilterFilesType filter, bool replaceExistingFiles);
        List<ManualImportItem> UpdateItems(List<ManualImportItem> item);
    }

    public class ManualImportService : IExecute<ManualImportCommand>, IManualImportService
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IParsingService _parsingService;
        private readonly IRootFolderService _rootFolderService;
        private readonly IDiskScanService _diskScanService;
        private readonly IMakeImportDecision _importDecisionMaker;
        private readonly IVolumeService _volumeService;
        private readonly IIssueService _issueService;
        private readonly IEditionService _editionService;
        private readonly IProvideIssueInfo _issueInfo;
        private readonly IMetadataTagService _metadataTagService;
        private readonly IImportApprovedIssues _importApprovedIssues;
        private readonly ICustomFormatCalculationService _formatCalculator;
        private readonly ITrackedDownloadService _trackedDownloadService;
        private readonly IDownloadedIssuesImportService _downloadedTracksImportService;
        private readonly IProvideImportItemService _provideImportItemService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public ManualImportService(IDiskProvider diskProvider,
                                   IParsingService parsingService,
                                   IRootFolderService rootFolderService,
                                   IDiskScanService diskScanService,
                                   IMakeImportDecision importDecisionMaker,
                                   IVolumeService volumeService,
                                   IIssueService issueService,
                                   IEditionService editionService,
                                   IProvideIssueInfo issueInfo,
                                   IMetadataTagService metadataTagService,
                                   IImportApprovedIssues importApprovedIssues,
                                   ICustomFormatCalculationService formatCalculator,
                                   ITrackedDownloadService trackedDownloadService,
                                   IDownloadedIssuesImportService downloadedTracksImportService,
                                   IProvideImportItemService provideImportItemService,
                                   IEventAggregator eventAggregator,
                                   Logger logger)
        {
            _diskProvider = diskProvider;
            _parsingService = parsingService;
            _rootFolderService = rootFolderService;
            _diskScanService = diskScanService;
            _importDecisionMaker = importDecisionMaker;
            _volumeService = volumeService;
            _issueService = issueService;
            _editionService = editionService;
            _issueInfo = issueInfo;
            _metadataTagService = metadataTagService;
            _importApprovedIssues = importApprovedIssues;
            _formatCalculator = formatCalculator;
            _trackedDownloadService = trackedDownloadService;
            _downloadedTracksImportService = downloadedTracksImportService;
            _provideImportItemService = provideImportItemService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public List<ManualImportItem> GetMediaFiles(string path, string downloadId, Volume volume, FilterFilesType filter, bool replaceExistingFiles)
        {
            if (downloadId.IsNotNullOrWhiteSpace())
            {
                var trackedDownload = _trackedDownloadService.Find(downloadId);

                if (trackedDownload == null)
                {
                    return new List<ManualImportItem>();
                }

                if (trackedDownload.ImportItem == null)
                {
                    trackedDownload.ImportItem = _provideImportItemService.ProvideImportItem(trackedDownload.DownloadItem, trackedDownload.ImportItem);
                }

                path = trackedDownload.ImportItem.OutputPath.FullPath;
            }

            if (!_diskProvider.FolderExists(path))
            {
                if (!_diskProvider.FileExists(path))
                {
                    return new List<ManualImportItem>();
                }

                var files = new List<IFileInfo> { _diskProvider.GetFileInfo(path) };

                var config = new ImportDecisionMakerConfig
                {
                    Filter = FilterFilesType.None,
                    NewDownload = true,
                    SingleRelease = false,
                    IncludeExisting = !replaceExistingFiles,
                    AddNewVolumes = false,
                    KeepAllEditions = true
                };

                var decision = _importDecisionMaker.GetImportDecisions(files, null, null, config);
                var result = MapItem(decision.First(), downloadId, replaceExistingFiles, false);

                return new List<ManualImportItem> { result };
            }

            return ProcessFolder(path, downloadId, volume, filter, replaceExistingFiles);
        }

        private List<ManualImportItem> ProcessFolder(string folder, string downloadId, Volume volume, FilterFilesType filter, bool replaceExistingFiles)
        {
            DownloadClientItem downloadClientItem = null;
            var directoryInfo = new DirectoryInfo(folder);
            volume = volume ?? _parsingService.GetVolume(directoryInfo.Name);

            if (downloadId.IsNotNullOrWhiteSpace())
            {
                var trackedDownload = _trackedDownloadService.Find(downloadId);
                downloadClientItem = trackedDownload?.DownloadItem;

                if (volume == null)
                {
                    volume = trackedDownload?.RemoteIssue?.Volume;
                }
            }

            var volumeFiles = _diskScanService.GetIssueFiles(folder).ToList();
            var idOverrides = new IdentificationOverrides
            {
                Volume = volume
            };
            var itemInfo = new ImportDecisionMakerInfo
            {
                DownloadClientItem = downloadClientItem,
                ParsedIssueInfo = Parser.Parser.ParseIssueTitle(directoryInfo.Name)
            };
            var config = new ImportDecisionMakerConfig
            {
                Filter = filter,
                NewDownload = true,
                SingleRelease = false,
                IncludeExisting = !replaceExistingFiles,
                AddNewVolumes = false,
                KeepAllEditions = true
            };

            var decisions = _importDecisionMaker.GetImportDecisions(volumeFiles, idOverrides, itemInfo, config);

            // paths will be different for new and old files which is why we need to map separately
            var newFiles = volumeFiles.Join(decisions,
                                            f => f.FullName,
                                            d => d.Item.Path,
                                            (f, d) => new { File = f, Decision = d },
                                            PathEqualityComparer.Instance);

            var newItems = newFiles.Select(x => MapItem(x.Decision, downloadId, replaceExistingFiles, false));
            var existingDecisions = decisions.Except(newFiles.Select(x => x.Decision));
            var existingItems = existingDecisions.Select(x => MapItem(x, null, replaceExistingFiles, false));

            return newItems.Concat(existingItems).ToList();
        }

        public List<ManualImportItem> UpdateItems(List<ManualImportItem> items)
        {
            var replaceExistingFiles = items.All(x => x.ReplaceExistingFiles);
            var groupedItems = items.Where(x => !x.AdditionalFile).GroupBy(x => x.Issue?.Id);
            _logger.Debug($"UpdateItems, {groupedItems.Count()} groups, replaceExisting {replaceExistingFiles}");

            var result = new List<ManualImportItem>();

            foreach (var group in groupedItems)
            {
                _logger.Debug("UpdateItems, group key: {0}", group.Key);

                var disableReleaseSwitching = group.First().DisableReleaseSwitching;

                var files = group.Select(x => _diskProvider.GetFileInfo(x.Path)).ToList();
                var idOverride = new IdentificationOverrides
                {
                    Volume = group.First().Volume,
                    Issue = group.First().Issue,
                    Edition = group.First().Edition
                };
                var config = new ImportDecisionMakerConfig
                {
                    Filter = FilterFilesType.None,
                    NewDownload = true,
                    SingleRelease = true,
                    IncludeExisting = !replaceExistingFiles,
                    AddNewVolumes = false
                };
                var decisions = _importDecisionMaker.GetImportDecisions(files, idOverride, null, config);

                var existingItems = group.Join(decisions,
                                               i => i.Path,
                                               d => d.Item.Path,
                                               (i, d) => new { Item = i, Decision = d },
                                               PathEqualityComparer.Instance);

                foreach (var pair in existingItems)
                {
                    var item = pair.Item;
                    var decision = pair.Decision;

                    if (decision.Item.Volume != null)
                    {
                        item.Volume = decision.Item.Volume;
                    }

                    if (decision.Item.Issue != null)
                    {
                        item.Issue = decision.Item.Issue;
                        item.Edition = decision.Item.Edition;
                    }

                    if (item.Quality?.Quality == Quality.Unknown)
                    {
                        item.Quality = decision.Item.Quality;
                    }

                    if (item.ReleaseGroup.IsNullOrWhiteSpace())
                    {
                        item.ReleaseGroup = decision.Item.ReleaseGroup;
                    }

                    item.Rejections = decision.Rejections;
                    item.Size = decision.Item.Size;

                    result.Add(item);
                }

                var newDecisions = decisions.Except(existingItems.Select(x => x.Decision));
                result.AddRange(newDecisions.Select(x => MapItem(x, null, replaceExistingFiles, disableReleaseSwitching)));
            }

            return result;
        }

        private ManualImportItem MapItem(ImportDecision<LocalIssue> decision, string downloadId, bool replaceExistingFiles, bool disableReleaseSwitching)
        {
            var item = new ManualImportItem();

            item.Id = HashConverter.GetHashInt31(decision.Item.Path);
            item.Path = decision.Item.Path;
            item.Name = Path.GetFileNameWithoutExtension(decision.Item.Path);
            item.DownloadId = downloadId;

            if (decision.Item.Volume != null)
            {
                item.Volume = decision.Item.Volume;

                item.CustomFormats = _formatCalculator.ParseCustomFormat(decision.Item);
            }

            if (decision.Item.Issue != null)
            {
                item.Issue = decision.Item.Issue;
                item.Edition = decision.Item.Edition;
            }

            item.Quality = decision.Item.Quality;
            item.IndexerFlags = (int)decision.Item.IndexerFlags;
            item.Size = _diskProvider.GetFileSize(decision.Item.Path);
            item.Rejections = decision.Rejections;
            item.Tags = decision.Item.FileTrackInfo;
            item.AdditionalFile = decision.Item.AdditionalFile;
            item.ReplaceExistingFiles = replaceExistingFiles;
            item.DisableReleaseSwitching = disableReleaseSwitching;

            return item;
        }

        public void Execute(ManualImportCommand message)
        {
            _logger.ProgressTrace("Manually importing {0} files using mode {1}", message.Files.Count, message.ImportMode);

            var imported = new List<ImportResult>();
            var importedTrackedDownload = new List<ManuallyImportedFile>();
            var issueIds = message.Files.GroupBy(e => e.IssueId).ToList();
            var fileCount = 0;

            foreach (var importIssueId in issueIds)
            {
                var issueImportDecisions = new List<ImportDecision<LocalIssue>>();

                // turn off anyReleaseOk if specified
                if (importIssueId.First().DisableReleaseSwitching)
                {
                    var issue = _issueService.GetIssue(importIssueId.First().IssueId);
                    issue.AnyEditionOk = false;
                    _issueService.UpdateIssue(issue);
                }

                foreach (var file in importIssueId)
                {
                    _logger.ProgressTrace("Processing file {0} of {1}", fileCount + 1, message.Files.Count);

                    var volume = _volumeService.GetVolume(file.VolumeId);
                    var issue = _issueService.GetIssue(file.IssueId);

                    var edition = _editionService.GetEditionByForeignEditionId(file.ForeignEditionId);
                    if (edition == null)
                    {
                        var tuple = _issueInfo.GetIssueInfo(issue.ForeignIssueId);
                        edition = tuple.Item2.Editions.Value.SingleOrDefault(x => x.ForeignEditionId == file.ForeignEditionId);
                    }

                    var fileRootFolder = _rootFolderService.GetBestRootFolder(file.Path);
                    var fileInfo = _diskProvider.GetFileInfo(file.Path);
                    var fileTrackInfo = _metadataTagService.ReadTags(fileInfo) ?? new ParsedTrackInfo();

                    var localTrack = new LocalIssue
                    {
                        ExistingFile = fileRootFolder != null,
                        FileTrackInfo = fileTrackInfo,
                        Path = file.Path,
                        Part = fileTrackInfo.TrackNumbers.Any() ? fileTrackInfo.TrackNumbers.First() : 1,
                        PartCount = importIssueId.Count(),
                        Size = fileInfo.Length,
                        Modified = fileInfo.LastWriteTimeUtc,
                        Quality = file.Quality,
                        IndexerFlags = (IndexerFlags)file.IndexerFlags,
                        Volume = volume,
                        Issue = issue,
                        Edition = edition
                    };

                    var importDecision = new ImportDecision<LocalIssue>(localTrack);
                    if (_rootFolderService.GetBestRootFolder(volume.Path) == null)
                    {
                        _logger.Warn($"Destination volume folder {volume.Path} not in a Root Folder, skipping import");
                        importDecision.Reject(new Rejection($"Destination volume folder {volume.Path} is not in a Root Folder"));
                    }

                    issueImportDecisions.Add(importDecision);
                    fileCount += 1;
                }

                var downloadId = importIssueId.Select(x => x.DownloadId).FirstOrDefault(x => x.IsNotNullOrWhiteSpace());
                if (downloadId.IsNullOrWhiteSpace())
                {
                    imported.AddRange(_importApprovedIssues.Import(issueImportDecisions, message.ReplaceExistingFiles, null, message.ImportMode));
                }
                else
                {
                    var trackedDownload = _trackedDownloadService.Find(downloadId);
                    var importResults = _importApprovedIssues.Import(issueImportDecisions, message.ReplaceExistingFiles, trackedDownload.DownloadItem, message.ImportMode);

                    imported.AddRange(importResults);

                    foreach (var importResult in importResults)
                    {
                        importedTrackedDownload.Add(new ManuallyImportedFile
                        {
                            TrackedDownload = trackedDownload,
                            ImportResult = importResult
                        });
                    }
                }
            }

            _logger.ProgressTrace("Manually imported {0} files", imported.Count);

            foreach (var groupedTrackedDownload in importedTrackedDownload.GroupBy(i => i.TrackedDownload.DownloadItem.DownloadId).ToList())
            {
                var trackedDownload = groupedTrackedDownload.First().TrackedDownload;
                var outputPath = trackedDownload.ImportItem.OutputPath.FullPath;

                if (_diskProvider.FolderExists(outputPath))
                {
                    if (_downloadedTracksImportService.ShouldDeleteFolder(_diskProvider.GetDirectoryInfo(outputPath)) &&
                        trackedDownload.DownloadItem.CanMoveFiles)
                    {
                        _diskProvider.DeleteFolder(outputPath, true);
                    }
                }

                var importedCount = groupedTrackedDownload.Select(c => c.ImportResult)
                    .Count(c => c.Result == ImportResultType.Imported);
                var downloadItemCount = Math.Max(1, trackedDownload.RemoteIssue?.Issues.Count ?? 1);
                var allItemsImported = importedCount >= downloadItemCount;

                if (allItemsImported)
                {
                    trackedDownload.State = TrackedDownloadState.Imported;
                    _eventAggregator.PublishEvent(new DownloadCompletedEvent(trackedDownload, imported.First().ImportDecision.Item.Volume.Id));
                }
            }
        }
    }
}
