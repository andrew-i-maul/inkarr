using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.ImportLists.Exclusions;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Commands;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.MetadataSource.Goodreads;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.ImportLists
{
    public class ImportListSyncService : IExecute<ImportListSyncCommand>
    {
        private readonly IImportListFactory _importListFactory;
        private readonly IImportListExclusionService _importListExclusionService;
        private readonly IFetchAndParseImportList _listFetcherAndParser;
        private readonly IGoodreadsProxy _goodreadsProxy;
        private readonly IGoodreadsSearchProxy _goodreadsSearchProxy;
        private readonly IProvideIssueInfo _issueInfoProxy;
        private readonly IVolumeService _volumeService;
        private readonly IIssueService _issueService;
        private readonly IEditionService _editionService;
        private readonly IAddVolumeService _addVolumeService;
        private readonly IAddIssueService _addIssueService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly Logger _logger;

        public ImportListSyncService(IImportListFactory importListFactory,
                                     IImportListExclusionService importListExclusionService,
                                     IFetchAndParseImportList listFetcherAndParser,
                                     IGoodreadsProxy goodreadsProxy,
                                     IGoodreadsSearchProxy goodreadsSearchProxy,
                                     IProvideIssueInfo issueInfoProxy,
                                     IVolumeService volumeService,
                                     IIssueService issueService,
                                     IEditionService editionService,
                                     IAddVolumeService addVolumeService,
                                     IAddIssueService addIssueService,
                                     IEventAggregator eventAggregator,
                                     IManageCommandQueue commandQueueManager,
                                     Logger logger)
        {
            _importListFactory = importListFactory;
            _importListExclusionService = importListExclusionService;
            _listFetcherAndParser = listFetcherAndParser;
            _goodreadsProxy = goodreadsProxy;
            _goodreadsSearchProxy = goodreadsSearchProxy;
            _issueInfoProxy = issueInfoProxy;
            _volumeService = volumeService;
            _issueService = issueService;
            _editionService = editionService;
            _addVolumeService = addVolumeService;
            _addIssueService = addIssueService;
            _eventAggregator = eventAggregator;
            _commandQueueManager = commandQueueManager;
            _logger = logger;
        }

        private List<Issue> SyncAll()
        {
            if (_importListFactory.AutomaticAddEnabled().Empty())
            {
                _logger.Debug("No import lists with automatic add enabled");

                return new List<Issue>();
            }

            _logger.ProgressInfo("Starting Import List Sync");

            var listItems = _listFetcherAndParser.Fetch().ToList();

            return ProcessListItems(listItems);
        }

        private List<Issue> SyncList(ImportListDefinition definition)
        {
            _logger.ProgressInfo($"Starting Import List Refresh for List {definition.Name}");

            var listItems = _listFetcherAndParser.FetchSingleList(definition).ToList();

            return ProcessListItems(listItems);
        }

        private List<Issue> ProcessListItems(List<ImportListItemInfo> items)
        {
            var processed = new List<Issue>();
            var volumesToAdd = new List<Volume>();
            var issuesToAdd = new List<Issue>();

            if (items.Count == 0)
            {
                _logger.ProgressInfo("No list items to process");

                return new List<Issue>();
            }

            _logger.ProgressInfo("Processing {0} list items", items.Count);

            var reportNumber = 1;

            var listExclusions = _importListExclusionService.All();

            foreach (var report in items)
            {
                _logger.ProgressTrace("Processing list item {0}/{1}", reportNumber, items.Count);

                reportNumber++;

                var importList = _importListFactory.Get(report.ImportListId);

                if (report.Issue.IsNotNullOrWhiteSpace() || report.EditionGoodreadsId.IsNotNullOrWhiteSpace())
                {
                    if (report.EditionGoodreadsId.IsNullOrWhiteSpace() || report.VolumeGoodreadsId.IsNullOrWhiteSpace() || report.IssueGoodreadsId.IsNullOrWhiteSpace())
                    {
                        MapIssueReport(report);
                    }

                    ProcessIssueReport(importList, report, listExclusions, issuesToAdd, volumesToAdd);
                }
                else if (report.Volume.IsNotNullOrWhiteSpace() || report.VolumeGoodreadsId.IsNotNullOrWhiteSpace())
                {
                    if (report.VolumeGoodreadsId.IsNullOrWhiteSpace())
                    {
                        MapVolumeReport(report);
                    }

                    ProcessVolumeReport(importList, report, listExclusions, volumesToAdd);
                }
            }

            var addedVolumes = _addVolumeService.AddVolumes(volumesToAdd, false);
            var addedIssues = _addIssueService.AddIssues(issuesToAdd, false);

            var message = string.Format($"Import List Sync Completed. Items found: {items.Count}, Volumes added: {volumesToAdd.Count}, Issues added: {issuesToAdd.Count}");

            _logger.ProgressInfo(message);

            var toRefresh = addedVolumes.Select(x => x.Id).Concat(addedIssues.Select(x => x.Volume.Value.Id)).Distinct().ToList();
            if (toRefresh.Any())
            {
                _commandQueueManager.Push(new BulkRefreshVolumeCommand(toRefresh, true));
            }

            return processed;
        }

        private void MapIssueReport(ImportListItemInfo report)
        {
            if (report.VolumeGoodreadsId.IsNotNullOrWhiteSpace() && report.IssueGoodreadsId.IsNotNullOrWhiteSpace())
            {
                return;
            }

            if (report.EditionGoodreadsId.IsNotNullOrWhiteSpace() && int.TryParse(report.EditionGoodreadsId, out var goodreadsId))
            {
                // check the local DB
                var edition = _editionService.GetEditionByForeignEditionId(report.EditionGoodreadsId);

                if (edition != null)
                {
                    var issue = edition.Issue.Value;
                    report.IssueGoodreadsId = issue.ForeignIssueId;
                    report.Issue = edition.Title;
                    report.Volume ??= issue.VolumeMetadata.Value.Name;
                    report.VolumeGoodreadsId ??= issue.VolumeMetadata.Value.ForeignVolumeId;
                    return;
                }

                try
                {
                    var remoteIssue = _goodreadsProxy.GetIssueInfo(report.EditionGoodreadsId);

                    _logger.Trace($"Mapped {report.EditionGoodreadsId} to [{remoteIssue.ForeignIssueId}] {remoteIssue.Title}");

                    report.IssueGoodreadsId = remoteIssue.ForeignIssueId;
                    report.Issue = remoteIssue.Title;
                    report.Volume ??= remoteIssue.VolumeMetadata.Value.Name;
                    report.VolumeGoodreadsId ??= remoteIssue.VolumeMetadata.Value.ForeignVolumeId;
                }
                catch (IssueNotFoundException)
                {
                    _logger.Debug($"Nothing found for edition [{report.EditionGoodreadsId}]");
                    report.EditionGoodreadsId = null;
                }
            }
            else if (report.IssueGoodreadsId.IsNotNullOrWhiteSpace())
            {
                var mappedIssue = _issueInfoProxy.GetIssueInfo(report.IssueGoodreadsId);

                report.IssueGoodreadsId = mappedIssue.Item2.ForeignIssueId;
                report.Issue = mappedIssue.Item2.Title;
                report.VolumeGoodreadsId = mappedIssue.Item3.First().ForeignVolumeId;
            }
            else
            {
                var mappedIssue = _goodreadsSearchProxy.Search($"{report.Issue} {report.Volume}").FirstOrDefault();

                if (mappedIssue == null)
                {
                    _logger.Trace($"Nothing found for {report.Volume} - {report.Issue}");
                    return;
                }

                _logger.Trace($"Mapped Issue {report.Issue} by Volume {report.Volume} to [{mappedIssue.WorkId}] {mappedIssue.IssueTitleBare}");

                report.IssueGoodreadsId = mappedIssue.WorkId.ToString();
                report.Issue = mappedIssue.IssueTitleBare;
                report.Volume ??= mappedIssue.Volume.Name;
                report.VolumeGoodreadsId ??= mappedIssue.Volume.Id.ToString();
                report.EditionGoodreadsId = mappedIssue.IssueId.ToString();
            }
        }

        private void ProcessIssueReport(ImportListDefinition importList, ImportListItemInfo report, List<ImportListExclusion> listExclusions, List<Issue> issuesToAdd, List<Volume> volumesToAdd)
        {
            // Check to see if issue in DB
            var existingIssue = _issueService.FindById(report.IssueGoodreadsId);

            // Check to see if issue excluded
            var excludedIssue = listExclusions.SingleOrDefault(s => s.ForeignId == report.IssueGoodreadsId);

            // Check to see if volume excluded
            var excludedVolume = listExclusions.SingleOrDefault(s => s.ForeignId == report.VolumeGoodreadsId);

            if (excludedIssue != null)
            {
                _logger.Debug("{0} [{1}] Rejected due to list exclusion", report.EditionGoodreadsId, report.Issue);
                return;
            }

            if (excludedVolume != null)
            {
                _logger.Debug("{0} [{1}] Rejected due to list exclusion for parent volume", report.EditionGoodreadsId, report.Issue);
                return;
            }

            if (existingIssue != null)
            {
                _logger.Debug("{0} [{1}] Rejected, Issue Exists in DB.  Ensuring Issue and Volume monitored.", report.EditionGoodreadsId, report.Issue);

                if (importList.ShouldMonitorExisting && importList.ShouldMonitor != ImportListMonitorType.None)
                {
                    if (!existingIssue.Monitored)
                    {
                        _issueService.SetIssueMonitored(existingIssue.Id, true);

                        if (importList.ShouldMonitor == ImportListMonitorType.SpecificIssue)
                        {
                            _commandQueueManager.Push(new IssueSearchCommand(new List<int> { existingIssue.Id }));
                        }
                    }

                    var existingVolume = existingIssue.Volume.Value;
                    var doSearch = false;

                    if (importList.ShouldMonitor == ImportListMonitorType.EntireVolume)
                    {
                        if (existingVolume.Issues.Value.Any(x => !x.Monitored))
                        {
                            doSearch = true;
                            _issueService.SetMonitored(existingVolume.Issues.Value.Select(x => x.Id), true);
                        }
                    }

                    if (!existingVolume.Monitored)
                    {
                        doSearch = true;
                        existingVolume.Monitored = true;
                        _volumeService.UpdateVolume(existingVolume);
                    }

                    if (doSearch)
                    {
                        _commandQueueManager.Push(new MissingIssueSearchCommand(existingVolume.Id));
                    }
                }

                return;
            }

            // Append Issue if not already in DB or already on add list
            if (issuesToAdd.All(s => s.ForeignIssueId != report.IssueGoodreadsId))
            {
                var monitored = importList.ShouldMonitor != ImportListMonitorType.None;

                var toAddVolume = new Volume
                {
                    Monitored = monitored,
                    MonitorNewItems = importList.MonitorNewItems,
                    RootFolderPath = importList.RootFolderPath,
                    QualityProfileId = importList.ProfileId,
                    MetadataProfileId = importList.MetadataProfileId,
                    Tags = importList.Tags,
                    AddOptions = new AddVolumeOptions
                    {
                        SearchForMissingIssues = importList.ShouldSearch,
                        Monitored = monitored,
                        Monitor = monitored ? MonitorTypes.All : MonitorTypes.None
                    }
                };

                if (report.VolumeGoodreadsId != null && report.Volume != null)
                {
                    toAddVolume = ProcessVolumeReport(importList, report, listExclusions, volumesToAdd);
                }

                var toAdd = new Issue
                {
                    ForeignIssueId = report.IssueGoodreadsId,
                    Monitored = monitored,
                    AnyEditionOk = true,
                    Editions = new List<Edition>(),
                    Volume = toAddVolume,
                    AddOptions = new AddIssueOptions
                    {
                        // Only search for new issue for existing volumes
                        // New volume searches are triggered by SearchForMissingIssues
                        SearchForNewIssue = importList.ShouldSearch && toAddVolume.Id > 0
                    }
                };

                if (report.EditionGoodreadsId.IsNotNullOrWhiteSpace() && int.TryParse(report.EditionGoodreadsId, out var goodreadsId))
                {
                    toAdd.Editions.Value.Add(new Edition
                    {
                        ForeignEditionId = report.EditionGoodreadsId,
                        Monitored = true
                    });
                }

                if (importList.ShouldMonitor == ImportListMonitorType.SpecificIssue && toAddVolume.AddOptions != null)
                {
                    Debug.Assert(toAddVolume.Id == 0, "new volume added but ID is not 0");
                    toAddVolume.AddOptions.IssuesToMonitor.Add(toAdd.ForeignIssueId);
                }

                issuesToAdd.Add(toAdd);
            }
        }

        private void MapVolumeReport(ImportListItemInfo report)
        {
            var mappedIssue = _goodreadsSearchProxy.Search(report.Volume).FirstOrDefault();

            if (mappedIssue == null)
            {
                _logger.Trace($"Nothing found for {report.Volume}");
                return;
            }

            _logger.Trace($"Mapped {report.Volume} to [{mappedIssue.Volume.Name}]");

            report.Volume = mappedIssue.Volume.Name;
            report.VolumeGoodreadsId = mappedIssue.Volume.Id.ToString();
        }

        private Volume ProcessVolumeReport(ImportListDefinition importList, ImportListItemInfo report, List<ImportListExclusion> listExclusions, List<Volume> volumesToAdd)
        {
            if (report.VolumeGoodreadsId == null)
            {
                return null;
            }

            // Check to see if volume in DB
            var existingVolume = _volumeService.FindById(report.VolumeGoodreadsId);

            // Check to see if volume excluded
            var excludedVolume = listExclusions.SingleOrDefault(s => s.ForeignId == report.VolumeGoodreadsId);

            // Check to see if volume in import
            var existingImportVolume = volumesToAdd.Find(i => i.ForeignVolumeId == report.VolumeGoodreadsId);

            if (excludedVolume != null)
            {
                _logger.Debug("{0} [{1}] Rejected due to list exclusion", report.VolumeGoodreadsId, report.Volume);
                return null;
            }

            if (existingVolume != null)
            {
                _logger.Debug("{0} [{1}] Rejected, Volume Exists in DB.  Ensuring Volume monitored", report.VolumeGoodreadsId, report.Volume);

                if (importList.ShouldMonitorExisting && !existingVolume.Monitored)
                {
                    existingVolume.Monitored = true;
                    _volumeService.UpdateVolume(existingVolume);
                }

                return existingVolume;
            }

            if (existingImportVolume != null)
            {
                _logger.Debug("{0} [{1}] Rejected, Volume Exists in Import.", report.VolumeGoodreadsId, report.Volume);

                return existingImportVolume;
            }

            var monitored = importList.ShouldMonitor != ImportListMonitorType.None;

            var toAdd = new Volume
            {
                Metadata = new VolumeMetadata
                {
                    ForeignVolumeId = report.VolumeGoodreadsId,
                    Name = report.Volume
                },
                Monitored = monitored,
                MonitorNewItems = importList.MonitorNewItems,
                RootFolderPath = importList.RootFolderPath,
                QualityProfileId = importList.ProfileId,
                MetadataProfileId = importList.MetadataProfileId,
                Tags = importList.Tags,
                AddOptions = new AddVolumeOptions
                {
                    SearchForMissingIssues = importList.ShouldSearch,
                    Monitored = monitored,
                    Monitor = monitored ? MonitorTypes.All : MonitorTypes.None
                }
            };

            volumesToAdd.Add(toAdd);

            return toAdd;
        }

        public void Execute(ImportListSyncCommand message)
        {
            var processed = message.DefinitionId.HasValue ? SyncList(_importListFactory.Get(message.DefinitionId.Value)) : SyncAll();

            _eventAggregator.PublishEvent(new ImportListSyncCompleteEvent(processed));
        }
    }
}
