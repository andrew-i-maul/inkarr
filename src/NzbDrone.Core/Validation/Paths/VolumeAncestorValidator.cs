using System.Linq;
using FluentValidation.Validators;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.Validation.Paths
{
    public class VolumeAncestorValidator : PropertyValidator
    {
        private readonly IVolumeService _volumeService;

        public VolumeAncestorValidator(IVolumeService volumeService)
        {
            _volumeService = volumeService;
        }

        protected override string GetDefaultMessageTemplate() => "Path '{path}' is an ancestor of an existing volume";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null)
            {
                return true;
            }

            context.MessageFormatter.AppendArgument("path", context.PropertyValue.ToString());

            return !_volumeService.AllVolumePaths().Any(s => context.PropertyValue.ToString().IsParentPath(s.Value));
        }
    }
}
