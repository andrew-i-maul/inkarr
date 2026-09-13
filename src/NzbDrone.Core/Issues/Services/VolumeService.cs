using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Issues
{
    public interface IVolumeService
    {
        Volume GetVolume(int volumeId);
        Volume GetVolumeByMetadataId(int volumeMetadataId);
        List<Volume> GetVolumes(IEnumerable<int> volumeIds);
        Volume AddVolume(Volume newVolume, bool doRefresh);
        List<Volume> AddVolumes(List<Volume> newVolumes, bool doRefresh);
        Volume FindById(string foreignVolumeId);
        Volume FindByName(string title);
        Volume FindByNameInexact(string title);
        List<Volume> GetCandidates(string title);
        List<Volume> GetReportCandidates(string reportTitle);
        void DeleteVolume(int volumeId, bool deleteFiles, bool addImportListExclusion = false);
        List<Volume> GetAllVolumes();
        Dictionary<int, List<int>> GetAllVolumeTags();
        List<Volume> AllForTag(int tagId);
        Volume UpdateVolume(Volume volume);
        List<Volume> UpdateVolumes(List<Volume> volumes, bool useExistingRelativeFolder);
        Dictionary<int, string> AllVolumePaths();
        bool VolumePathExists(string folder);
        void RemoveAddOptions(Volume volume);
    }

    public class VolumeService : IVolumeService
    {
        private readonly IVolumeRepository _volumeRepository;
        private readonly IEventAggregator _eventAggregator;
        private readonly IBuildVolumePaths _volumePathBuilder;
        private readonly Logger _logger;
        private readonly ICached<List<Volume>> _cache;

        public VolumeService(IVolumeRepository volumeRepository,
                             IEventAggregator eventAggregator,
                             IBuildVolumePaths volumePathBuilder,
                             ICacheManager cacheManager,
                             Logger logger)
        {
            _volumeRepository = volumeRepository;
            _eventAggregator = eventAggregator;
            _volumePathBuilder = volumePathBuilder;
            _cache = cacheManager.GetRollingCache<List<Volume>>(GetType(), "volumecache", TimeSpan.FromSeconds(30));
            _logger = logger;
        }

        public Volume AddVolume(Volume newVolume, bool doRefresh)
        {
            _cache.Clear();
            _volumeRepository.Insert(newVolume);
            _eventAggregator.PublishEvent(new VolumeAddedEvent(GetVolume(newVolume.Id), doRefresh));

            return newVolume;
        }

        public List<Volume> AddVolumes(List<Volume> newVolumes, bool doRefresh)
        {
            _cache.Clear();
            _volumeRepository.InsertMany(newVolumes);
            _eventAggregator.PublishEvent(new VolumesImportedEvent(newVolumes.Select(s => s.Id).ToList(), doRefresh));

            return newVolumes;
        }

        public bool VolumePathExists(string folder)
        {
            return _volumeRepository.VolumePathExists(folder);
        }

        public void DeleteVolume(int volumeId, bool deleteFiles, bool addImportListExclusion = false)
        {
            _cache.Clear();
            var volume = _volumeRepository.Get(volumeId);
            _volumeRepository.Delete(volumeId);
            _eventAggregator.PublishEvent(new VolumeDeletedEvent(volume, deleteFiles, addImportListExclusion));
        }

        public Volume FindById(string foreignVolumeId)
        {
            return _volumeRepository.FindById(foreignVolumeId);
        }

        public Volume FindByName(string title)
        {
            return _volumeRepository.FindByName(title.CleanVolumeName());
        }

        public List<Tuple<Func<Volume, string, double>, string>> VolumeScoringFunctions(string title, string cleanTitle)
        {
            Func<Func<Volume, string, double>, string, Tuple<Func<Volume, string, double>, string>> tc = Tuple.Create;
            var scoringFunctions = new List<Tuple<Func<Volume, string, double>, string>>
            {
                tc((a, t) => a.Metadata.Value.Name.FuzzyMatch(t), title),
                tc((a, t) => a.Metadata.Value.NameLastFirst.FuzzyMatch(t), title)
            };

            return scoringFunctions;
        }

        public Volume FindByNameInexact(string title)
        {
            var volumes = GetAllVolumes();

            foreach (var func in VolumeScoringFunctions(title, title.CleanVolumeName()))
            {
                var results = FindByStringInexact(volumes, func.Item1, func.Item2);
                if (results.Count == 1)
                {
                    return results[0];
                }
            }

            return null;
        }

        public List<Volume> GetCandidates(string title)
        {
            var volumes = GetAllVolumes();
            var output = new List<Volume>();

            foreach (var func in VolumeScoringFunctions(title, title.CleanVolumeName()))
            {
                output.AddRange(FindByStringInexact(volumes, func.Item1, func.Item2));
            }

            return output.DistinctBy(x => x.Id).ToList();
        }

        public List<Tuple<Func<Volume, string, double>, string>> ReportVolumeScoringFunctions(string reportTitle, string cleanReportTitle)
        {
            Func<Func<Volume, string, double>, string, Tuple<Func<Volume, string, double>, string>> tc = Tuple.Create;
            var scoringFunctions = new List<Tuple<Func<Volume, string, double>, string>>
            {
                tc((a, t) => t.FuzzyMatch(a.Metadata.Value.Name, 0.6).Item3, reportTitle),
                tc((a, t) => t.FuzzyMatch(a.Metadata.Value.NameLastFirst, 0.6).Item3, reportTitle)
            };

            return scoringFunctions;
        }

        public List<Volume> GetReportCandidates(string reportTitle)
        {
            var volumes = GetAllVolumes();
            var output = new List<Volume>();

            foreach (var func in ReportVolumeScoringFunctions(reportTitle, reportTitle.CleanVolumeName()))
            {
                output.AddRange(FindByStringInexact(volumes, func.Item1, func.Item2));
            }

            return output.DistinctBy(x => x.Id).ToList();
        }

        private List<Volume> FindByStringInexact(List<Volume> volumes, Func<Volume, string, double> scoreFunction, string title)
        {
            const double fuzzThreshold = 0.8;
            const double fuzzGap = 0.2;

            var sortedVolumes = volumes.Select(s => new
            {
                MatchProb = scoreFunction(s, title),
                Volume = s
            })
                .ToList()
                .OrderByDescending(s => s.MatchProb)
                .ToList();

            return sortedVolumes.TakeWhile((x, i) => i == 0 || sortedVolumes[i - 1].MatchProb - x.MatchProb < fuzzGap)
                .TakeWhile((x, i) => x.MatchProb > fuzzThreshold || (i > 0 && sortedVolumes[i - 1].MatchProb > fuzzThreshold))
                .Select(x => x.Volume)
                .ToList();
        }

        public List<Volume> GetAllVolumes()
        {
            return _cache.Get("GetAllVolumes", () => _volumeRepository.All().ToList(), TimeSpan.FromSeconds(30));
        }

        public Dictionary<int, List<int>> GetAllVolumeTags()
        {
            return _volumeRepository.AllVolumeTags();
        }

        public Dictionary<int, string> AllVolumePaths()
        {
            return _volumeRepository.AllVolumePaths();
        }

        public List<Volume> AllForTag(int tagId)
        {
            return GetAllVolumes().Where(s => s.Tags.Contains(tagId))
                                 .ToList();
        }

        public Volume GetVolume(int volumeId)
        {
            return _volumeRepository.Get(volumeId);
        }

        public Volume GetVolumeByMetadataId(int volumeMetadataId)
        {
            return _volumeRepository.GetVolumeByMetadataId(volumeMetadataId);
        }

        public List<Volume> GetVolumes(IEnumerable<int> volumeIds)
        {
            return _volumeRepository.Get(volumeIds).ToList();
        }

        public void RemoveAddOptions(Volume volume)
        {
            _volumeRepository.SetFields(volume, s => s.AddOptions);
        }

        public Volume UpdateVolume(Volume volume)
        {
            _cache.Clear();

            var storedVolume = GetVolume(volume.Id);

            // Never update AddOptions when updating an volume, keep it the same as the existing stored volume.
            volume.AddOptions = storedVolume.AddOptions;

            var updatedVolume = _volumeRepository.Update(volume);
            _eventAggregator.PublishEvent(new VolumeEditedEvent(updatedVolume, storedVolume));

            return updatedVolume;
        }

        public List<Volume> UpdateVolumes(List<Volume> volume, bool useExistingRelativeFolder)
        {
            _cache.Clear();
            _logger.Debug("Updating {0} volume", volume.Count);

            foreach (var s in volume)
            {
                _logger.Trace("Updating: {0}", s.Name);

                if (!s.RootFolderPath.IsNullOrWhiteSpace())
                {
                    s.Path = _volumePathBuilder.BuildPath(s, useExistingRelativeFolder);

                    _logger.Trace("Changing path for {0} to {1}", s.Name, s.Path);
                }
                else
                {
                    _logger.Trace("Not changing path for: {0}", s.Name);
                }
            }

            _volumeRepository.UpdateMany(volume);
            _logger.Debug("{0} volumes updated", volume.Count);

            return volume;
        }
    }
}
