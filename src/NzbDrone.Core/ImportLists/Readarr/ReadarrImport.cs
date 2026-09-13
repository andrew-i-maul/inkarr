using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.Readarr
{
    public class ReadarrImport : ImportListBase<ReadarrSettings>
    {
        private readonly IReadarrV1Proxy _readarrV1Proxy;
        public override string Name => "Readarr";

        public override ImportListType ListType => ImportListType.Program;
        public override TimeSpan MinRefreshInterval => TimeSpan.FromMinutes(15);

        public ReadarrImport(IReadarrV1Proxy readarrV1Proxy,
                            IImportListStatusService importListStatusService,
                            IConfigService configService,
                            IParsingService parsingService,
                            Logger logger)
            : base(importListStatusService, configService, parsingService, logger)
        {
            _readarrV1Proxy = readarrV1Proxy;
        }

        public override IList<ImportListItemInfo> Fetch()
        {
            var volumesAndIssues = new List<ImportListItemInfo>();

            try
            {
                var remoteIssues = _readarrV1Proxy.GetIssues(Settings);
                var remoteVolumes = _readarrV1Proxy.GetVolumes(Settings);

                var volumeDict = remoteVolumes.ToDictionary(x => x.Id);

                foreach (var remoteIssue in remoteIssues)
                {
                    var remoteVolume = volumeDict[remoteIssue.VolumeId];

                    if (Settings.ProfileIds.Any() && !Settings.ProfileIds.Contains(remoteVolume.QualityProfileId))
                    {
                        continue;
                    }

                    if (Settings.TagIds.Any() && !Settings.TagIds.Any(x => remoteVolume.Tags.Any(y => y == x)))
                    {
                        continue;
                    }

                    if (Settings.RootFolderPaths.Any() && !Settings.RootFolderPaths.Any(rootFolderPath => remoteVolume.RootFolderPath.ContainsIgnoreCase(rootFolderPath)))
                    {
                        continue;
                    }

                    if (!remoteIssue.Monitored || !remoteVolume.Monitored)
                    {
                        continue;
                    }

                    volumesAndIssues.Add(new ImportListItemInfo
                    {
                        IssueGoodreadsId = remoteIssue.ForeignIssueId,
                        Issue = remoteIssue.Title,
                        EditionGoodreadsId = remoteIssue.ForeignEditionId,
                        Volume = remoteVolume.VolumeName,
                        VolumeGoodreadsId = remoteVolume.ForeignVolumeId
                    });
                }

                _importListStatusService.RecordSuccess(Definition.Id);
            }
            catch
            {
                _logger.Warn("List Import Sync Task Failed for List [{0}]", Definition.Name);
                _importListStatusService.RecordFailure(Definition.Id);
            }

            return CleanupListItems(volumesAndIssues);
        }

        public override object RequestAction(string action, IDictionary<string, string> query)
        {
            // Return early if there is not an API key
            if (Settings.ApiKey.IsNullOrWhiteSpace())
            {
                return new
                {
                    devices = new List<object>()
                };
            }

            Settings.Validate().Filter("ApiKey").ThrowOnError();

            if (action == "getProfiles")
            {
                var devices = _readarrV1Proxy.GetProfiles(Settings);

                return new
                {
                    options = devices.OrderBy(d => d.Name, StringComparer.InvariantCultureIgnoreCase)
                        .Select(d => new
                        {
                            Value = d.Id,
                            Name = d.Name
                        })
                };
            }

            if (action == "getTags")
            {
                var devices = _readarrV1Proxy.GetTags(Settings);

                return new
                {
                    options = devices.OrderBy(d => d.Label, StringComparer.InvariantCultureIgnoreCase)
                        .Select(d => new
                        {
                            Value = d.Id,
                            Name = d.Label
                        })
                };
            }

            if (action == "getRootFolders")
            {
                var remoteRootFolders = _readarrV1Proxy.GetRootFolders(Settings);

                return new
                {
                    options = remoteRootFolders.OrderBy(d => d.Path, StringComparer.InvariantCultureIgnoreCase)
                        .Select(d => new
                        {
                            value = d.Path,
                            name = d.Path
                        })
                };
            }

            return new { };
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(_readarrV1Proxy.Test(Settings));
        }
    }
}
