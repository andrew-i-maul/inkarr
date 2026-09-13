using System;
using System.IO;
using FluentValidation.Validators;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Organizer;

namespace Inkarr.Api.V1.Volume
{
    public class VolumeFolderAsRootFolderValidator : PropertyValidator
    {
        private readonly IBuildFileNames _fileNameBuilder;

        public VolumeFolderAsRootFolderValidator(IBuildFileNames fileNameBuilder)
        {
            _fileNameBuilder = fileNameBuilder;
        }

        protected override string GetDefaultMessageTemplate() => "Root folder path '{rootFolderPath}' contains volume folder '{volumeFolder}'";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null)
            {
                return true;
            }

            if (context.ParentContext.InstanceToValidate is not VolumeResource volumeResource)
            {
                return true;
            }

            var rootFolderPath = context.PropertyValue.ToString();

            if (rootFolderPath.IsNullOrWhiteSpace())
            {
                return true;
            }

            var rootFolder = new DirectoryInfo(rootFolderPath!).Name;
            var volume = volumeResource.ToModel();
            var volumeFolder = _fileNameBuilder.GetVolumeFolder(volume);

            context.MessageFormatter.AppendArgument("rootFolderPath", rootFolderPath);
            context.MessageFormatter.AppendArgument("volumeFolder", volumeFolder);

            if (volumeFolder == rootFolder)
            {
                return false;
            }

            var distance = volumeFolder.LevenshteinDistance(rootFolder);

            return distance >= Math.Max(1, volumeFolder.Length * 0.2);
        }
    }
}
