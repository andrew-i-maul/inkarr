using System;
using System.Collections.Generic;
using System.IO;
using FluentValidation.Results;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Extras.Metadata
{
    public abstract class MetadataBase<TSettings> : IMetadata
        where TSettings : IProviderConfig, new()
    {
        public abstract string Name { get; }

        public Type ConfigContract => typeof(TSettings);

        public virtual ProviderMessage Message => null;

        public IEnumerable<ProviderDefinition> DefaultDefinitions => new List<ProviderDefinition>();

        public ProviderDefinition Definition { get; set; }

        public ValidationResult Test()
        {
            return new ValidationResult();
        }

        public virtual string GetFilenameAfterMove(Volume volume, IssueFile issueFile, MetadataFile metadataFile)
        {
            var existingFilename = Path.Combine(volume.Path, metadataFile.RelativePath);
            var extension = Path.GetExtension(existingFilename).TrimStart('.');
            var newFileName = Path.ChangeExtension(issueFile.Path, extension);

            return newFileName;
        }

        public virtual string GetFilenameAfterMove(Volume volume, string issuePath, MetadataFile metadataFile)
        {
            var existingFilename = Path.GetFileName(metadataFile.RelativePath);
            var newFileName = Path.Combine(volume.Path, issuePath, existingFilename);

            return newFileName;
        }

        public abstract MetadataFile FindMetadataFile(Volume volume, string path);

        public abstract MetadataFileResult VolumeMetadata(Volume volume);
        public abstract MetadataFileResult IssueMetadata(Volume volume, IssueFile issueFile);
        public abstract List<ImageFileResult> VolumeImages(Volume volume);
        public abstract List<ImageFileResult> IssueImages(Volume volume, IssueFile issueFile);

        public virtual object RequestAction(string action, IDictionary<string, string> query)
        {
            return null;
        }

        protected TSettings Settings => (TSettings)Definition.Settings;

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
