using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Extras;
using NzbDrone.Core.History;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Calibre;
using NzbDrone.Core.Issues.Commands;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.MediaFiles.IssueImport
{
    public interface IImportApprovedIssues
    {
        List<ImportResult> Import(List<ImportDecision<LocalIssue>> decisions, bool replaceExisting, DownloadClientItem downloadClientItem = null, ImportMode importMode = ImportMode.Auto);
    }

    public class ImportApprovedIssues : IImportApprovedIssues
    {
        private static readonly RegexReplace PadNumbers = new RegexReplace(@"\d+", n => n.Value.PadLeft(9, '0'), RegexOptions.Compiled);

        private readonly IUpgradeMediaFiles _issueFileUpgrader;
        private readonly IMediaFileService _mediaFileService;
        private readonly IMetadataTagService _metadataTagService;
        private readonly IVolumeService _volumeService;
        private readonly IAddVolumeService _addVolumeService;
        private readonly IIssueService _issueService;
        private readonly IEditionService _editionService;
        private readonly IRootFolderService _rootFolderService;
        private readonly IRecycleBinProvider _recycleBinProvider;
        private readonly IExtraService _extraService;
        private readonly IDiskProvider _diskProvider;
        private readonly IHistoryService _historyService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly Logger _logger;

        public ImportApprovedIssues(IUpgradeMediaFiles issueFileUpgrader,
                                   IMediaFileService mediaFileService,
                                   IMetadataTagService metadataTagService,
                                   IVolumeService volumeService,
                                   IAddVolumeService addVolumeService,
                                   IIssueService issueService,
                                   IEditionService editionService,
                                   IRootFolderService rootFolderService,
                                   IRecycleBinProvider recycleBinProvider,
                                   IExtraService extraService,
                                   IDiskProvider diskProvider,
                                   IHistoryService historyService,
                                   IEventAggregator eventAggregator,
                                   IManageCommandQueue commandQueueManager,
                                   Logger logger)
        {
            _issueFileUpgrader = issueFileUpgrader;
            _mediaFileService = mediaFileService;
            _metadataTagService = metadataTagService;
            _volumeService = volumeService;
            _addVolumeService = addVolumeService;
            _issueService = issueService;
            _editionService = editionService;
            _rootFolderService = rootFolderService;
            _recycleBinProvider = recycleBinProvider;
            _extraService = extraService;
            _diskProvider = diskProvider;
            _historyService = historyService;
            _eventAggregator = eventAggregator;
            _commandQueueManager = commandQueueManager;
            _logger = logger;
        }

        public List<ImportResult> Import(List<ImportDecision<LocalIssue>> decisions, bool replaceExisting, DownloadClientItem downloadClientItem = null, ImportMode importMode = ImportMode.Auto)
        {
            var importResults = new List<ImportResult>();
            var allImportedTrackFiles = new List<IssueFile>();
            var allOldTrackFiles = new List<IssueFile>();
            var addedVolumes = new List<Volume>();
            var addedIssues = new List<Issue>();

            var issueDecisions = decisions.Where(e => e.Item.Issue != null && e.Approved)
                .GroupBy(e => e.Item.Issue.ForeignIssueId).ToList();

            var iDecision = 1;
            foreach (var issueDecision in issueDecisions)
            {
                _logger.ProgressInfo("Importing issue {0}/{1} {2}", iDecision++, issueDecisions.Count, issueDecision.First().Item.Issue);

                var decisionList = issueDecision.ToList();

                var volume = EnsureVolumeAdded(decisionList, addedVolumes);

                if (volume == null)
                {
                    // failed to add the volume, carry on with next issue
                    continue;
                }

                var issue = EnsureIssueAdded(decisionList, addedIssues);

                if (issue == null)
                {
                    // failed to add the issue, carry on with next one
                    continue;
                }

                var edition = EnsureEditionAdded(decisionList);

                if (edition == null)
                {
                    // failed to add the edition, carry on with next one
                    continue;
                }

                // if (replaceExisting)
                // {
                //     RemoveExistingTrackFiles(volume, issue);
                // }

                // Make sure part numbers are populated for audioissues
                // If all audio files and all part numbers are zero, set them by filename order
                if (decisionList.All(b => MediaFileExtensions.AudioExtensions.Contains(Path.GetExtension(b.Item.Path)) && b.Item.Part == 0))
                {
                    var part = 1;
                    foreach (var d in decisionList.OrderBy(x => PadNumbers.Replace(x.Item.Path)))
                    {
                        d.Item.Part = part++;
                    }
                }

                // set the correct release to be monitored before importing the new files
                var newRelease = issueDecision.First().Item.Edition;
                _logger.Debug("Updating release to {0}", newRelease);
                issue.Editions = _editionService.SetMonitored(newRelease);

                // Publish issue edited event.
                // Deliberately don't put in the old issue since we don't want to trigger an VolumeScan.
                _eventAggregator.PublishEvent(new IssueEditedEvent(issue, issue));
            }

            var qualifiedImports = decisions.Where(c => c.Approved)
                .GroupBy(c => c.Item.Volume.Id, (i, s) => s
                         .OrderByDescending(c => c.Item.Quality, new QualityModelComparer(s.First().Item.Volume.QualityProfile))
                         .ThenByDescending(c => c.Item.Size))
                .SelectMany(c => c)
                .ToList();

            _logger.ProgressInfo("Importing {0} files", qualifiedImports.Count);
            _logger.Debug("Importing {0} files. Replace existing: {1}", qualifiedImports.Count, replaceExisting);

            var filesToAdd = new List<IssueFile>(qualifiedImports.Count);
            var trackImportedEvents = new List<TrackImportedEvent>(qualifiedImports.Count);

            foreach (var importDecision in qualifiedImports)
            {
                var localTrack = importDecision.Item;
                var oldFiles = new List<IssueFile>();

                try
                {
                    //check if already imported
                    if (importResults.Where(r => r.ImportDecision.Item.Issue.Id == localTrack.Issue.Id).Any(r => r.ImportDecision.Item.Part == localTrack.Part))
                    {
                        importResults.Add(new ImportResult(importDecision, "Issue has already been imported"));
                        continue;
                    }

                    localTrack.Issue.Volume = localTrack.Volume;

                    var issueFile = new IssueFile
                    {
                        Path = localTrack.Path.CleanFilePath(),
                        CalibreId = localTrack.CalibreId,
                        Part = localTrack.Part,
                        PartCount = localTrack.PartCount,
                        Size = localTrack.Size,
                        Modified = localTrack.Modified,
                        DateAdded = DateTime.UtcNow,
                        ReleaseGroup = localTrack.ReleaseGroup,
                        Quality = localTrack.Quality,
                        MediaInfo = localTrack.FileTrackInfo.MediaInfo,
                        EditionId = localTrack.Edition.Id,
                        Volume = localTrack.Volume,
                        Edition = localTrack.Edition
                    };

                    if (downloadClientItem?.DownloadId.IsNotNullOrWhiteSpace() == true)
                    {
                        var grabHistory = _historyService.FindByDownloadId(downloadClientItem.DownloadId)
                            .OrderByDescending(h => h.Date)
                            .FirstOrDefault(h => h.EventType == EntityHistoryEventType.Grabbed);

                        if (Enum.TryParse(grabHistory?.Data.GetValueOrDefault("indexerFlags"), true, out IndexerFlags flags))
                        {
                            issueFile.IndexerFlags = flags;
                        }
                    }
                    else
                    {
                        issueFile.IndexerFlags = localTrack.IndexerFlags;
                    }

                    bool copyOnly;
                    switch (importMode)
                    {
                        default:
                        case ImportMode.Auto:
                            copyOnly = downloadClientItem != null && !downloadClientItem.CanMoveFiles;
                            break;
                        case ImportMode.Move:
                            copyOnly = false;
                            break;
                        case ImportMode.Copy:
                            copyOnly = true;
                            break;
                    }

                    if (!localTrack.ExistingFile)
                    {
                        issueFile.SceneName = GetSceneReleaseName(downloadClientItem);

                        var moveResult = _issueFileUpgrader.UpgradeIssueFile(issueFile, localTrack, copyOnly);
                        oldFiles = moveResult.OldFiles;
                    }
                    else
                    {
                        // Delete existing files from the DB mapped to this path
                        var previousFile = _mediaFileService.GetFileWithPath(issueFile.Path);

                        if (previousFile != null)
                        {
                            _mediaFileService.Delete(previousFile, DeleteMediaFileReason.ManualOverride);

                            if (issueFile.CalibreId == 0 && previousFile.CalibreId != 0)
                            {
                                issueFile.CalibreId = previousFile.CalibreId;
                            }
                        }

                        _metadataTagService.WriteTags(issueFile, false);
                    }

                    filesToAdd.Add(issueFile);
                    importResults.Add(new ImportResult(importDecision));

                    if (!localTrack.ExistingFile)
                    {
                        _extraService.ImportTrack(localTrack, issueFile, copyOnly);
                    }

                    allImportedTrackFiles.Add(issueFile);
                    allOldTrackFiles.AddRange(oldFiles);

                    // create all the import events here, but we can't publish until the trackfiles have been
                    // inserted and ids created
                    trackImportedEvents.Add(new TrackImportedEvent(localTrack, issueFile, oldFiles, !localTrack.ExistingFile, downloadClientItem));
                }
                catch (RootFolderNotFoundException e)
                {
                    _logger.Warn(e, "Couldn't import issue " + localTrack);
                    _eventAggregator.PublishEvent(new TrackImportFailedEvent(e, localTrack, !localTrack.ExistingFile, downloadClientItem));

                    importResults.Add(new ImportResult(importDecision, "Failed to import issue, root folder missing."));
                }
                catch (DestinationAlreadyExistsException e)
                {
                    _logger.Warn(e, "Couldn't import issue " + localTrack);
                    importResults.Add(new ImportResult(importDecision, "Failed to import issue, destination already exists."));
                }
                catch (UnauthorizedAccessException e)
                {
                    _logger.Warn(e, "Couldn't import issue " + localTrack);
                    _eventAggregator.PublishEvent(new TrackImportFailedEvent(e, localTrack, !localTrack.ExistingFile, downloadClientItem));

                    importResults.Add(new ImportResult(importDecision, "Failed to import issue, permissions error"));
                }
                catch (RecycleBinException e)
                {
                    _logger.Warn(e, "Couldn't import issue " + localTrack);
                    _eventAggregator.PublishEvent(new TrackImportFailedEvent(e, localTrack, !localTrack.ExistingFile, downloadClientItem));

                    importResults.Add(new ImportResult(importDecision, "Failed to import issue, unable to move existing file to the Recycle Bin."));
                }
                catch (CalibreException e)
                {
                    _logger.Warn(e, "Couldn't import issue " + localTrack);

                    importResults.Add(new ImportResult(importDecision, "Failed to import issue, error communicating with Calibre.  Check log for details."));
                }
                catch (Exception e)
                {
                    _logger.Warn(e, "Couldn't import issue " + localTrack);
                    importResults.Add(new ImportResult(importDecision, "Failed to import issue."));
                }
            }

            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            _mediaFileService.AddMany(filesToAdd);
            _logger.Debug("Inserted new trackfiles in {0}ms", watch.ElapsedMilliseconds);

            // now that trackfiles have been inserted and ids generated, publish the import events
            foreach (var trackImportedEvent in trackImportedEvents)
            {
                _eventAggregator.PublishEvent(trackImportedEvent);
            }

            var issueImports = importResults.Where(e => e.ImportDecision.Item.Issue != null)
                .GroupBy(e => e.ImportDecision.Item.Issue.Id).ToList();

            foreach (var issueImport in issueImports)
            {
                var issue = issueImport.First().ImportDecision.Item.Issue;
                var edition = issue.Editions.Value.Single(x => x.Monitored);
                var volume = issueImport.First().ImportDecision.Item.Volume;

                if (issueImport.Where(e => e.Errors.Count == 0).ToList().Count > 0 && volume != null && issue != null)
                {
                    _eventAggregator.PublishEvent(new IssueImportedEvent(
                        volume,
                        issue,
                        allImportedTrackFiles.Where(s => s.EditionId == edition.Id).ToList(),
                        allOldTrackFiles.Where(s => s.EditionId == edition.Id).ToList(),
                        replaceExisting,
                        downloadClientItem));
                }
            }

            //Adding all the rejected decisions
            importResults.AddRange(decisions.Where(c => !c.Approved)
                                            .Select(d => new ImportResult(d, d.Rejections.Select(r => r.Reason).ToArray())));

            // Refresh any volumes we added
            if (addedVolumes.Any())
            {
                _commandQueueManager.Push(new BulkRefreshVolumeCommand(addedVolumes.Select(x => x.Id).ToList(), true));
            }

            var addedVolumeMetadataIds = addedVolumes.Select(x => x.VolumeMetadataId).ToHashSet();
            var issuesToRefresh = addedIssues.Where(x => !addedVolumeMetadataIds.Contains(x.VolumeMetadataId)).ToList();

            if (issuesToRefresh.Any())
            {
                _logger.Debug("Refreshing info for {0} new issues", issuesToRefresh.Count);
                _commandQueueManager.Push(new BulkRefreshIssueCommand(issuesToRefresh.Select(x => x.Id).ToList()));
            }

            return importResults;
        }

        private Volume EnsureVolumeAdded(List<ImportDecision<LocalIssue>> decisions, List<Volume> addedVolumes)
        {
            var volume = decisions.First().Item.Volume;

            if (volume.Id == 0)
            {
                var dbVolume = _volumeService.FindById(volume.ForeignVolumeId);

                if (dbVolume == null)
                {
                    _logger.Debug("Adding remote volume {0}", volume);

                    var path = decisions.First().Item.Path;
                    var rootFolder = _rootFolderService.GetBestRootFolder(path);

                    volume.RootFolderPath = rootFolder.Path;
                    volume.MetadataProfileId = rootFolder.DefaultMetadataProfileId;
                    volume.QualityProfileId = rootFolder.DefaultQualityProfileId;
                    volume.Monitored = rootFolder.DefaultMonitorOption != MonitorTypes.None;
                    volume.MonitorNewItems = rootFolder.DefaultNewItemMonitorOption;
                    volume.Tags = rootFolder.DefaultTags;
                    volume.AddOptions = new AddVolumeOptions
                    {
                        SearchForMissingIssues = false,
                        Monitored = volume.Monitored,
                        Monitor = rootFolder.DefaultMonitorOption
                    };

                    if (rootFolder.IsCalibreLibrary)
                    {
                        // calibre has volume / issue / files
                        volume.Path = path.GetParentPath().GetParentPath();
                    }

                    try
                    {
                        dbVolume = _addVolumeService.AddVolume(volume, false);

                        // this looks redundant but is necessary to get the LazyLoads populated
                        dbVolume = _volumeService.GetVolume(dbVolume.Id);
                        addedVolumes.Add(dbVolume);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Failed to add volume {0}", volume);
                        foreach (var decision in decisions)
                        {
                            decision.Reject(new Rejection("Failed to add missing volume", RejectionType.Temporary));
                        }

                        return null;
                    }
                }

                // Put in the newly loaded volume
                foreach (var decision in decisions)
                {
                    decision.Item.Volume = dbVolume;
                    decision.Item.Issue.Volume = dbVolume;
                    decision.Item.Issue.VolumeMetadataId = dbVolume.VolumeMetadataId;
                }

                volume = dbVolume;
            }

            return volume;
        }

        private Issue EnsureIssueAdded(List<ImportDecision<LocalIssue>> decisions, List<Issue> addedIssues)
        {
            var issue = decisions.First().Item.Issue;

            if (issue.Id == 0)
            {
                var dbIssue = _issueService.FindById(issue.ForeignIssueId);

                if (dbIssue == null)
                {
                    _logger.Debug("Adding remote issue {0}", issue);

                    if (issue.VolumeMetadataId == 0)
                    {
                        throw new InvalidOperationException("Cannot insert issue with VolumeMetadataId = 0");
                    }

                    try
                    {
                        issue.Monitored = issue.Volume.Value.Monitored;
                        issue.Added = DateTime.UtcNow;
                        _issueService.InsertMany(new List<Issue> { issue });
                        addedIssues.Add(issue);

                        issue.Editions.Value.ForEach(x => x.IssueId = issue.Id);
                        _editionService.InsertMany(issue.Editions.Value);

                        dbIssue = _issueService.FindById(issue.ForeignIssueId);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Failed to add issue {0}", issue);
                        RejectIssue(decisions);

                        return null;
                    }
                }

                var edition = dbIssue.Editions.Value.ExclusiveOrDefault(x => x.ForeignEditionId == decisions.First().Item.Edition.ForeignEditionId);
                if (edition == null)
                {
                    RejectIssue(decisions);
                    return null;
                }

                // Populate the new DB issue
                foreach (var decision in decisions)
                {
                    decision.Item.Issue = dbIssue;
                    decision.Item.Edition = edition;
                }

                issue = dbIssue;
            }

            return issue;
        }

        private Edition EnsureEditionAdded(List<ImportDecision<LocalIssue>> decisions)
        {
            var issue = decisions.First().Item.Issue;
            var edition = decisions.First().Item.Edition;

            if (edition.Id == 0)
            {
                var dbEdition = _editionService.GetEditionByForeignEditionId(edition.ForeignEditionId);

                if (dbEdition == null)
                {
                    _logger.Debug("Adding remote edition {0}", edition);

                    try
                    {
                        edition.IssueId = issue.Id;
                        edition.Monitored = false;
                        _editionService.InsertMany(new List<Edition> { edition });

                        dbEdition = _editionService.GetEditionByForeignEditionId(edition.ForeignEditionId);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Failed to add edition {0}", edition);
                        RejectIssue(decisions);

                        return null;
                    }

                    // Populate the new DB issue
                    foreach (var decision in decisions)
                    {
                        decision.Item.Edition = dbEdition;
                    }

                    edition = dbEdition;
                }
            }

            return edition;
        }

        private void RejectIssue(List<ImportDecision<LocalIssue>> decisions)
        {
            foreach (var decision in decisions)
            {
                decision.Reject(new Rejection("Failed to add missing issue", RejectionType.Temporary));
            }
        }

        private void RemoveExistingTrackFiles(Volume volume, Issue issue)
        {
            var rootFolder = _diskProvider.GetParentFolder(volume.Path);
            var previousFiles = _mediaFileService.GetFilesByIssue(issue.Id);

            _logger.Debug("Deleting {0} existing files for {1}", previousFiles.Count, issue);

            foreach (var previousFile in previousFiles)
            {
                var subfolder = rootFolder.GetRelativePath(_diskProvider.GetParentFolder(previousFile.Path));
                if (_diskProvider.FileExists(previousFile.Path))
                {
                    _logger.Debug("Removing existing issue file: {0}", previousFile);
                    _recycleBinProvider.DeleteFile(previousFile.Path, subfolder);
                }

                _mediaFileService.Delete(previousFile, DeleteMediaFileReason.Upgrade);
            }
        }

        private string GetSceneReleaseName(DownloadClientItem downloadClientItem)
        {
            if (downloadClientItem != null)
            {
                var title = Parser.Parser.RemoveFileExtension(downloadClientItem.Title);

                var parsedTitle = Parser.Parser.ParseIssueTitle(title);

                if (parsedTitle != null)
                {
                    return title;
                }
            }

            return null;
        }
    }
}
