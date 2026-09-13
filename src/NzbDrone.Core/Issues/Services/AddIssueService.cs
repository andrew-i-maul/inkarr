using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using NLog;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.ImportLists.Exclusions;
using NzbDrone.Core.MetadataSource;

namespace NzbDrone.Core.Issues
{
    public interface IAddIssueService
    {
        Issue AddIssue(Issue issue, bool doRefresh = true);
        List<Issue> AddIssues(List<Issue> issues, bool doRefresh = true);
    }

    public class AddIssueService : IAddIssueService
    {
        private readonly IVolumeService _volumeService;
        private readonly IAddVolumeService _addVolumeService;
        private readonly IIssueService _issueService;
        private readonly IProvideIssueInfo _issueInfo;
        private readonly IImportListExclusionService _importListExclusionService;
        private readonly Logger _logger;

        public AddIssueService(IVolumeService volumeService,
                               IAddVolumeService addVolumeService,
                               IIssueService issueService,
                               IProvideIssueInfo issueInfo,
                               IImportListExclusionService importListExclusionService,
                               Logger logger)
        {
            _volumeService = volumeService;
            _addVolumeService = addVolumeService;
            _issueService = issueService;
            _issueInfo = issueInfo;
            _importListExclusionService = importListExclusionService;
            _logger = logger;
        }

        public Issue AddIssue(Issue issue, bool doRefresh = true)
        {
            _logger.Debug($"Adding issue {issue}");

            issue = AddSkyhookData(issue);

            // we allow adding extra editions, so check if the issue already exists
            var dbIssue = _issueService.FindById(issue.ForeignIssueId);
            if (dbIssue != null)
            {
                issue.UseDbFieldsFrom(dbIssue);
            }

            // Remove any import list exclusions preventing addition
            _importListExclusionService.Delete(issue.ForeignIssueId);
            _importListExclusionService.Delete(issue.VolumeMetadata.Value.ForeignVolumeId);

            // Note it's a manual addition so it's not deleted on next refresh
            issue.AddOptions.AddType = IssueAddType.Manual;
            issue.Editions.Value.Single(x => x.Monitored).ManualAdd = true;

            // Add the volume if necessary
            var dbVolume = _volumeService.FindById(issue.VolumeMetadata.Value.ForeignVolumeId);
            if (dbVolume == null)
            {
                var volume = issue.Volume.Value;

                volume.Metadata.Value.ForeignVolumeId = issue.VolumeMetadata.Value.ForeignVolumeId;

                dbVolume = _addVolumeService.AddVolume(volume, false);
            }

            issue.Volume = dbVolume;
            issue.VolumeMetadataId = dbVolume.VolumeMetadataId;
            _issueService.AddIssue(issue, doRefresh);

            return issue;
        }

        public List<Issue> AddIssues(List<Issue> issues, bool doRefresh = true)
        {
            var added = DateTime.UtcNow;
            var addedIssues = new List<Issue>();

            foreach (var a in issues)
            {
                a.Added = added;
                try
                {
                    addedIssues.Add(AddIssue(a, doRefresh));
                }
                catch (Exception ex)
                {
                    // Could be a bad id from an import list
                    _logger.Error(ex, "Failed to import id: {0} - {1}", a.ForeignIssueId, a.Title);
                }
            }

            return addedIssues;
        }

        private Issue AddSkyhookData(Issue newIssue)
        {
            var editionId = newIssue.Editions.Value.Single(x => x.Monitored).ForeignEditionId;

            Tuple<string, Issue, List<VolumeMetadata>> tuple = null;
            try
            {
                tuple = _issueInfo.GetIssueInfo(newIssue.ForeignIssueId);
            }
            catch (IssueNotFoundException)
            {
                _logger.Error("Issue with Foreign Id {0} was not found, it may have been removed from Goodreads.", newIssue.ForeignIssueId);

                throw new ValidationException(new List<ValidationFailure>
                                              {
                                                  new ValidationFailure("GoodreadsId", "A issue with this ID was not found", newIssue.ForeignIssueId)
                                              });
            }

            newIssue.UseMetadataFrom(tuple.Item2);
            newIssue.Added = DateTime.UtcNow;

            newIssue.Editions = tuple.Item2.Editions.Value;
            newIssue.Editions.Value.ForEach(x => x.Monitored = false);
            newIssue.Editions.Value.Single(x => x.ForeignEditionId == editionId).Monitored = true;

            var metadata = tuple.Item3.FirstOrDefault(x => x.ForeignVolumeId == tuple.Item1);
            newIssue.VolumeMetadata = metadata;

            return newIssue;
        }
    }
}
