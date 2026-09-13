using System.Linq;
using FluentValidation.Validators;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.Validation.Paths
{
    public class VolumePathValidator : PropertyValidator
    {
        private readonly IVolumeService _volumeService;

        public VolumePathValidator(IVolumeService volumeService)
        {
            _volumeService = volumeService;
        }

        protected override string GetDefaultMessageTemplate() => "Path '{path}' is already configured for another volume";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null)
            {
                return true;
            }

            context.MessageFormatter.AppendArgument("path", context.PropertyValue.ToString());

            dynamic instance = context.ParentContext.InstanceToValidate;
            var instanceId = (int)instance.Id;

            return !_volumeService.AllVolumePaths().Any(s => s.Value.PathEquals(context.PropertyValue.ToString()) && s.Key != instanceId);
        }
    }
}
