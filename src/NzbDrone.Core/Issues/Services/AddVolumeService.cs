using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Issues
{
    public interface IAddVolumeService
    {
        Volume AddVolume(Volume newVolume, bool doRefresh = true);
        List<Volume> AddVolumes(List<Volume> newVolumes, bool doRefresh = true);
    }

    public class AddVolumeService : IAddVolumeService
    {
        private readonly IVolumeService _volumeService;
        private readonly IVolumeMetadataService _volumeMetadataService;
        private readonly IProvideVolumeInfo _volumeInfo;
        private readonly IBuildFileNames _fileNameBuilder;
        private readonly IAddVolumeValidator _addVolumeValidator;
        private readonly Logger _logger;

        public AddVolumeService(IVolumeService volumeService,
                                IVolumeMetadataService volumeMetadataService,
                                IProvideVolumeInfo volumeInfo,
                                IBuildFileNames fileNameBuilder,
                                IAddVolumeValidator addVolumeValidator,
                                Logger logger)
        {
            _volumeService = volumeService;
            _volumeMetadataService = volumeMetadataService;
            _volumeInfo = volumeInfo;
            _fileNameBuilder = fileNameBuilder;
            _addVolumeValidator = addVolumeValidator;
            _logger = logger;
        }

        public Volume AddVolume(Volume newVolume, bool doRefresh = true)
        {
            Ensure.That(newVolume, () => newVolume).IsNotNull();

            newVolume = AddSkyhookData(newVolume);
            newVolume = SetPropertiesAndValidate(newVolume);

            _logger.Info("Adding Volume {0} Path: [{1}]", newVolume, newVolume.Path);

            // add metadata
            _volumeMetadataService.Upsert(newVolume.Metadata.Value);
            newVolume.VolumeMetadataId = newVolume.Metadata.Value.Id;

            // add the volume itself
            return _volumeService.AddVolume(newVolume, doRefresh);
        }

        public List<Volume> AddVolumes(List<Volume> newVolumes, bool doRefresh = true)
        {
            var added = DateTime.UtcNow;
            var volumesToAdd = new List<Volume>();

            foreach (var s in newVolumes)
            {
                try
                {
                    var volume = AddSkyhookData(s);
                    volume = SetPropertiesAndValidate(volume);
                    volume.Added = added;
                    volumesToAdd.Add(volume);
                }
                catch (Exception ex)
                {
                    // Catch Import Errors for now until we get things fixed up
                    _logger.Error(ex, "Failed to import id: {0} - {1}", s.Metadata.Value.ForeignVolumeId, s.Metadata.Value.Name);
                }
            }

            // add metadata
            _volumeMetadataService.UpsertMany(volumesToAdd.Select(x => x.Metadata.Value).ToList());
            volumesToAdd.ForEach(x => x.VolumeMetadataId = x.Metadata.Value.Id);

            return _volumeService.AddVolumes(volumesToAdd, doRefresh);
        }

        private Volume AddSkyhookData(Volume newVolume)
        {
            Volume volume;

            try
            {
                volume = _volumeInfo.GetVolumeInfo(newVolume.Metadata.Value.ForeignVolumeId, false);
            }
            catch (VolumeNotFoundException)
            {
                _logger.Error("InkarrId {0} was not found, it may have been removed from Goodreads.", newVolume.Metadata.Value.ForeignVolumeId);

                throw new ValidationException(new List<ValidationFailure>
                {
                    new ("ForeignVolumeId", "An volume with this ID was not found", newVolume.Metadata.Value.ForeignVolumeId)
                });
            }

            volume.ApplyChanges(newVolume);

            return volume;
        }

        private Volume SetPropertiesAndValidate(Volume newVolume)
        {
            var path = newVolume.Path;
            if (string.IsNullOrWhiteSpace(path))
            {
                var folderName = _fileNameBuilder.GetVolumeFolder(newVolume);
                path = Path.Combine(newVolume.RootFolderPath, folderName);
            }

            // Disambiguate volume path if it exists already
            if (_volumeService.VolumePathExists(path))
            {
                if (newVolume.Metadata.Value.Disambiguation.IsNotNullOrWhiteSpace())
                {
                    path += $" ({newVolume.Metadata.Value.Disambiguation})";
                }

                if (_volumeService.VolumePathExists(path))
                {
                    var basepath = path;
                    var i = 0;
                    do
                    {
                        i++;
                        path = basepath + $" ({i})";
                    }
                    while (_volumeService.VolumePathExists(path));
                }
            }

            newVolume.Path = path;
            newVolume.CleanName = newVolume.Metadata.Value.Name.CleanVolumeName();
            newVolume.Added = DateTime.UtcNow;

            if (newVolume.AddOptions != null && newVolume.AddOptions.Monitor == MonitorTypes.None)
            {
                newVolume.Monitored = false;
            }

            var validationResult = _addVolumeValidator.Validate(newVolume);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            return newVolume;
        }
    }
}
